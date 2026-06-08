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
