# CareerHub API - Database Optimization & PostgreSQL Features

This document details the architectural decisions and optimizations made during the Assignment 2.4 update.

## PostgreSQL Constraints
We added several database-level CHECK constraints to ensure data integrity independently of the application layer:
1. `ck_joblistings_salary_range`: Ensures `SalaryMax >= SalaryMin`. Prevents logically impossible salary ranges.
2. `ck_joblistings_expiresaftercreated`: Ensures `ClosingDate > PostedAt`. Prevents listings from expiring before they are posted.
3. `ck_applications_submitted_not_future`: Ensures `SubmittedAt <= CURRENT_TIMESTAMP`. Prevents applications from being recorded with a future submission date.

These constraints provide a robust final line of defense against bugs in the service layer or erroneous manual database updates.

## Full-Text Search and GIN Index Strategy
To support robust full-text search across job titles and descriptions:
- We introduced a `SearchVector` property of type `NpgsqlTsVector` on the `JobListing` entity, configured with the `english` dictionary.
- We added a **GIN (Generalized Inverted Index)** on this `SearchVector` column.
- GIN indices are heavily optimized for mapping values to rows that contain them, making them highly efficient for full-text search queries (like `@@ to_tsquery`) compared to standard B-Tree indices.

## Performance Analysis (`EXPLAIN ANALYZE`)
We seeded 200 job listings to analyze query performance before and after applying our indexing strategy.

### Query 1: Active Listings (`"IsActive" = true AND "ClosingDate" > CURRENT_TIMESTAMP`)
**Before Indices (Sequential Scan):**
```text
 Seq Scan on job_listings  (cost=0.00..13.00 rows=112 width=330) (actual time=0.013..0.078 rows=111.00 loops=1)
   Filter: ("IsActive" AND ("ClosingDate" > CURRENT_TIMESTAMP))
 Execution Time: 0.125 ms
```

**After Indices (Bitmap Index Scan):**
```text
 Bitmap Heap Scan on job_listings  (cost=9.30..20.98 rows=112 width=330) (actual time=0.512..0.593 rows=111.00 loops=1)
   ->  Bitmap Index Scan on ix_job_listings_isactive_closingdate  (cost=0.00..9.27 rows=112 width=0) (actual time=0.493..0.493 rows=111.00 loops=1)
         Index Cond: (("IsActive" = true) AND ("ClosingDate" > CURRENT_TIMESTAMP))
 Execution Time: 0.652 ms
```
*(Note: Because the dataset was small, PostgreSQL initially favored a sequential scan. We disabled `enable_seqscan` to force the index scan and demonstrate the index usage.)*

### Query 2: Full-Text Search (`"SearchVector" @@ to_tsquery('english', 'sprint')`)
**Before GIN Index (Sequential Scan):**
```text
 Seq Scan on job_listings  (cost=0.00..13.50 rows=36 width=330) (actual time=0.029..0.100 rows=37.00 loops=1)
   Filter: ("IsActive" AND ("SearchVector" @@ '''sprint'''::tsquery) AND ("ClosingDate" > CURRENT_TIMESTAMP))
 Execution Time: 0.149 ms
```

**After GIN Index (Bitmap Index Scan):**
```text
 Bitmap Heap Scan on job_listings  (cost=8.87..19.99 rows=36 width=330) (actual time=0.055..0.107 rows=37.00 loops=1)
   ->  Bitmap Index Scan on ix_job_listings_search_vector  (cost=0.00..8.86 rows=64 width=0) (actual time=0.043..0.044 rows=64.00 loops=1)
         Index Cond: ("SearchVector" @@ '''sprint'''::tsquery)
 Execution Time: 0.157 ms
```

## Raw SQL for Advanced Analytics
To calculate complex aggregations and rankings, we implemented `GetApplicationStatsAsync` using raw SQL with `_context.Database.SqlQuery<T>`.

This approach was chosen because EF Core LINQ translation does not easily support Window Functions (like `RANK() OVER`) or advanced aggregate filtering (`COUNT(...) FILTER (WHERE ...)`). Using raw SQL allows us to leverage PostgreSQL's full analytical capabilities directly while still mapping the results efficiently to our `JobListingStatsResponse` DTO.

## Connection Pooling
We updated `appsettings.json` and `appsettings.Development.json` with Npgsql connection pooling parameters (`MinPoolSize=5;MaxPoolSize=100;`). This ensures the API efficiently reuses database connections under high load, avoiding the overhead of constantly establishing new TCP connections.

## Assignment 3.1: Advanced API Patterns

### Pagination
**Why did you use offset pagination instead of cursor pagination? What is the trade-off?**
I used offset pagination because it allows users to jump directly to specific pages, which is common in typical search and listing interfaces. It is easier to implement using standard LINQ (`Skip` and `Take`) and provides total page count information easily. The trade-off is performance and data consistency at scale: offset queries become slower on very large datasets because the database must scan and discard rows before reaching the offset. Additionally, if rows are inserted or deleted while the user is paginating, they might see duplicate or missing items. Cursor pagination avoids these issues by using a unique identifier to fetch the next set of rows but prevents jumping to arbitrary pages.

### PATCH vs PUT
**Explain why partial updates are safer than PUT for a frontend team building a long form.**
Partial updates are safer because they only transmit the fields the user actively changed. If a frontend uses PUT, it must send the entire resource. If another user or system modifies a different field between the time the frontend fetches the form and submits the PUT request, the PUT request will inadvertently overwrite the other user's changes with stale data. PATCH limits the footprint of the update.
**Explain the race condition that still exists in your PATCH implementation.**
Even with PATCH, a race condition exists if two users simultaneously update the *same* field. The last request to reach the database will blindly overwrite the earlier one without any warning. Additionally, our implementation fetches the record, updates it in memory, and then saves it; if another process alters the database record during this small window, those changes might be lost or conflict with our save operation.

### API Versioning
**If you need to make a breaking change to the JobResponse next month, explain the lifecycle of introducing v2 and sunsetting v1.**
First, I would introduce `v2` by creating a new version of the controller (or using versioning attributes) and adding the breaking changes to `JobResponseV2`, while leaving `v1` intact and functioning. I would mark `v1` as deprecated using `[ApiVersion("1.0", Deprecated = true)]` so clients receive deprecation headers. Next, I would notify API consumers of the deprecation timeline. After the sunset period ends, I would completely remove the `v1` code, endpoints, and models, forcing all remaining clients to use `v2`.

### ETags
**What fields did you include in the job listing ETag fingerprint and why? Why is a strong ETag appropriate here?**
I included the job listing's `Id`, `PostedAt.Ticks`, and the hash code of the `SalaryDisplay`. This ensures that any meaningful modification to the job posting (such as an updated salary or a new listing with the same ID) generates a distinct ETag. A strong ETag is appropriate here because we want to guarantee byte-for-byte equivalence of the resource representation for conditional GET requests. It ensures clients rely on a cached version only if the resource is absolutely identical, preventing employers from viewing stale listing or salary data.

### Rate Limiting
**Explain how your application rate limits an authenticated user vs an anonymous user.**
In our rate limiting configuration, we use the client's IP address (`RemoteIpAddress`) as the partition key. This means both authenticated and anonymous users are treated identically based on their origin IP. Each IP is granted a global limit of 200 requests per minute, a sliding window of 30 search queries per minute, and stricter fixed limits for applying (5 per hour) and posting listings (10 per hour). If we wanted to differentiate, we would use `HttpContext.User.Identity.Name` or a claim as the partition key for authenticated users, leaving the IP fallback only for anonymous users.

### Connection Pooling
**If your API container gets 500 requests per second, but your database only has 100 connections available, what happens to request 101? How does EF Core handle this?**
When all 100 database connections are actively leased from the connection pool, request 101 will be queued by the connection pool manager. The application thread will wait asynchronously for a connection to be released back into the pool. If a connection is released within the configured timeout period, request 101 proceeds. If the timeout is reached before a connection becomes available, EF Core throws an `InvalidOperationException` (Timeout expired).
