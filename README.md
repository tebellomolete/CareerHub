c# CareerHub API

Welcome to the CareerHub API. This is the backend code for a job board application. It is built using a tool called ASP.NET Core.

---

## Part 1: Design & Decisions

Below are the decisions we made when building this application and why we made them.

### 1. Recording the Job Post Date (PostedAt)
When a new job is posted, the system itself should record exactly when it was posted (`PostedAt`). We do not let the user decide this date. If we did, a user could set a fake date (like a date in the past or the future). To prevent this, we created two different models:
* One model for **creating a job**, which does not let the user set a post date.
* One model for **displaying a job**, which includes the post date that the system calculated.

### 2. Checking Salary Ranges
We need to make sure that the maximum salary for a job is not smaller than the minimum salary (since a salary range like $50,000 to $40,000 does not make sense!). 
We set up rules so that this check happens automatically before our main code even runs. If the salaries are invalid, the system immediately rejects the request with a clear message. This keeps our main code clean and focused on doing its job.

### 3. Returning Updated Job Data (PUT)
When a user updates a job listing, we return a "200 OK" response containing the fully updated job details. We do this instead of sending back a blank response.
This is helpful because our system formats the salary range into a nice, readable text. By sending the updated job back immediately, the frontend app can show the new details right away without having to make a second request to ask the server how the updated job looks.

### 4. Deleting a Job That is Already Gone (DELETE)
If someone tries to delete a job using an ID that does not exist in our system, we return a "404 Not Found" error instead of pretending it succeeded.
This is important when multiple people are using the system at the same time. For example, if two administrators try to delete the exact same job, the first admin will successfully delete it. The second admin needs to know that the job was already gone. Sending a "404" lets them know the job did not exist when they tried to delete it.

---

## Part 2: Error Handling & Logging Updates

We have updated the project to handle errors and write log messages in a cleaner, more professional way.

### 1. Keeping Controllers Thin (Controller Thinning)
Instead of writing code in our controller to manually return error responses (like `return NotFound()`), we now just throw custom errors (like `throw new JobNotFoundException()`).

**Why this is better:**
* **Simpler Code (The Happy Path)**: The controller code only has to focus on what happens when things go right. It does not get cluttered with code for when things go wrong.
* **One Place for Errors**: We have a single, central error handler (called middleware) that watches for these custom errors. When an error is thrown, this handler catches it and decides how to format the message and what status code to send back to the user.
* **Easier to Change**: If we ever want to change how we show error messages to our users, we only have to change the code in one place (the central error handler) instead of updating every controller in our application.

### 2. Structured Logging (Serilog vs Console.WriteLine)
Instead of using standard print commands (like `Console.WriteLine`) to print simple text lines to the screen, we use a library called Serilog to create **Structured Logs** (formatted as organized data, like JSON).

**Why this is better:**
* **Searchable Logs**: Plain text prints are easy for a human to read on their own computer, but in a real production environment, millions of lines of logs are written every day. It is very hard to search through raw text.
* **Computer-Friendly**: Structured logs are saved like a database. A computer program can easily search, filter, and analyze them. For example, you can quickly search for:
  - All errors that happened in the last 15 minutes.
  - Every action taken by a specific user.
  - How long it took, on average, for a specific page to load.
* **Rich Details**: Serilog automatically includes extra information with every log, such as the exact time, the web address being requested, and the type of device the user was using, without us having to write extra code for it.

---

## Part 3: Authentication & Authorization

### 1. Stateless Authentication (Session-based vs JWT-based)
* **Session-Based Authentication (Stateful)**: In a traditional session-based system, the server authenticates the user and creates a session record stored in memory or a database (e.g., Redis). It sends a session ID back to the client, usually inside a cookie. On subsequent requests, the client sends this cookie, and the server must look up the session ID in its data store to verify the user.
* **JWT-Based Authentication (Stateless)**: In JWT-based authentication, the server generates a JSON Web Token containing the user's identity and claims (such as their username and roles), signs it digitally with a secret key, and sends it to the client. The client stores it and sends it in the `Authorization: Bearer <token>` header. The server verifies the token's signature using its secret key without querying any database or storing any session state on the server.
* **Why Statelessness Matters for Horizontal Scaling**: In a horizontally scaled system, multiple instances of the API run behind a load balancer. If we use stateful session-based authentication, a user's request might hit Server A (where their session is stored) but the next request might go to Server B (which knows nothing about the session), requiring sticky sessions or a shared session store (Redis). With stateless JWTs, any server instance can independently verify the token using the secret key. This simplifies scaling, reduces database load, and eliminates single points of failure.

### 2. 401 Unauthorized vs 403 Forbidden
* **401 Unauthorized**: This status code means the user is not authenticated. The server doesn't know who they are, or the authentication credentials provided (e.g., token) are invalid or missing.
  - **Where it is produced**: This is produced by the **Authentication Middleware** (`UseAuthentication()`). If the token is invalid, expired, or missing when a secure endpoint is requested, the authentication handler challenges the request and halts the pipeline, returning a `401 Unauthorized` before reaching the authorization checks or the controller.
* **403 Forbidden**: This status code means the user is authenticated (we know who they are), but they do not have the required permissions or roles to access the resource.
  - **Where it is produced**: This is produced by the **Authorization Middleware** (`UseAuthorization()`). Once the authentication middleware successfully identifies the user, the authorization middleware checks the user's claims against the endpoint's requirements (such as `[Authorize(Roles = "Employer")]`). If the user has a valid token but lacks the `"Employer"` role, the middleware stops the request and returns a `403 Forbidden`.

### 3. Token Storage and Security Risks
* **The Risk of `localStorage`**: Storing a JWT in `localStorage` or `sessionStorage` makes it vulnerable to Cross-Site Scripting (XSS) attacks. If an attacker manages to inject a malicious script (e.g., via a compromised third-party library, user-submitted HTML, or CDN), they can access the token using `window.localStorage.getItem(...)` and steal it.
* **Safer Alternatives**:
  - **HttpOnly Cookies**: Store the JWT in an `HttpOnly` and `Secure` cookie. Browsers automatically attach cookies to requests but prevent JavaScript from reading them (mitigating XSS theft). Use the `SameSite=Strict` or `SameSite=Lax` attribute to protect against Cross-Site Request Forgery (CSRF).
  - **In-Memory Storage**: Store the JWT in application memory (e.g., in a plain JavaScript variable). When the tab is closed or refreshed, the token is cleared. To keep the user logged in, use a silent refresh mechanism using an HttpOnly refresh token.

---

## Part 4: Database Persistence with EF Core

### 1. The Change Tracker
EF Core's change tracker watches entities for modifications. When we load a `JobListing` using `FindAsync`, EF Core takes a snapshot of its state. When we modify its properties, the change tracker detects the differences. `SaveChangesAsync()` is called only once at the end of the operation because it allows EF Core to batch all pending changes (inserts, updates, deletes) into a single, optimized database transaction, rather than executing a separate query for every single property change. This significantly improves performance and ensures data consistency.

### 2. Migrations as Version Control
The generated migration file must be committed to source control because it represents a specific, versioned state of the database schema that matches the application code at that point in time. It allows all developers to have a consistent database schema. 
If a teammate pulls code that references a migration they have not applied locally, their application code (which might depend on a new column or table) will fail when it tries to query their outdated local database. They must run `dotnet ef database update` to sync their schema with the new code.

### 3. Connection String Security
The connection string belongs in `appsettings.Development.json` and not `appsettings.json` because `appsettings.json` is typically committed to source control and distributed. Putting a production connection string (which contains passwords) in version control is a major security risk. `appsettings.Development.json` is often meant only for local, low-risk database credentials.
A safer alternative for a production deployment is to inject the connection string securely using Environment Variables or a dedicated secrets manager (like AWS Secrets Manager, Azure Key Vault, or HashiCorp Vault).

---

## Part 5: Advanced EF Core & Query Optimization

### 1. Relationship Design Decisions
**Delete Behaviour (Company → JobListing):**
We chose to configure the relationship between a `Company` and a `JobListing` using `DeleteBehavior.Restrict`. This means that if a company is deleted, its associated job listings will not be automatically deleted, and the database will prevent the deletion of the company if it still has active job listings. This is crucial for maintaining historical integrity; job listings represent historical data that users (applicants) might have already applied to. Deleting a company and cascading that deletion to job listings would orphaned applications and cause data loss. 

**Application Entity as an Explicit Join Table:**
A many-to-many relationship can sometimes be represented by a hidden join table (an implicit join table that EF Core manages for you). However, the `Application` relationship cannot be a hidden join table because it represents a *domain concept* in its own right. It carries its own unique payload data, such as `SubmittedAt` (when the application was submitted) and `Status` (the current state of the application in the hiring workflow). An implicit join table can only hold the two foreign keys, meaning it has no place to store this additional, crucial information. Therefore, an explicit `Application` entity is absolutely necessary.

### 2. N+1 Query Problem
**Observation:**
Before fixing the loading strategy, when calling `GET /api/jobs` and requesting a list of job listings that belong to different companies, the console logged multiple SQL queries: one initial query to fetch all the job listings, and then *N* additional queries (one for each job listing) to fetch the associated company data or application counts. 
After fixing the query using a `.Select()` projection, exactly **one** SQL statement was generated. This single query efficiently joined the necessary tables and aggregated the data directly in the database.

**Why it's dangerous in production:**
While an N+1 issue might work correctly and quickly in a local development environment (because the database is usually on the same machine and has very little data), it becomes a massive performance bottleneck in production. In production, each database query incurs network latency. If an endpoint returns 1,000 job listings, an N+1 issue would result in 1,001 separate network round-trips to the database. This rapidly exhausts database connection pools, spikes server CPU usage, and drastically slows down the API response times for users.

### 3. Read vs Write Queries
**Query Behavior (Change Tracking):**
When a `GET` endpoint fetches data using standard Entity Framework Core methods (like `ToListAsync()`), EF Core places those entities into its Change Tracker. This requires extra memory and CPU cycles as EF Core takes a snapshot of the data to watch for modifications. When we use `.AsNoTracking()`, EF Core bypasses the Change Tracker entirely. The query executes faster and consumes less memory because EF Core simply returns the data and forgets about it.

**Silent Data Loss Bug Scenario:**
Using the wrong setting on a write operation can cause silent bugs. For example, consider an update scenario (`PUT /api/jobs/{id}`) where we fetch the existing job listing using `.AsNoTracking()`. If we then modify the properties of that detached entity and call `SaveChangesAsync()`, EF Core will *not* save the changes because the Change Tracker isn't watching the entity. The update will appear to succeed (no exception is thrown), but the new data will silently fail to write to the database, resulting in data loss.

---

## Part 6: Architecture, DI & Repository Pattern

### 1. Repository Design Decisions
I chose to implement separate repositories (`IJobListingRepository`, `ICompanyRepository`, `IApplicantRepository`, and `IApplicationRepository`) rather than combining them into a single massive generic repository. 
- **Boundary Decision**: The `ApplicationService` needs to validate that a `JobListing` exists and is open before creating an application. It relies on the `IJobListingRepository.IsOpenForApplicationsAsync` method for this validation query. 
- **Company Validation**: I created a specific `ICompanyRepository` containing just an `ExistsAsync` method. This handles the company-related validation query needed by `JobListingService` cleanly. It is sufficient because the application only needs to know if the company exists before creating a job; it doesn't currently edit company data.

### 2. What the Controller Lost
During this refactor, the controllers shed all business and persistence responsibilities:
- **EF Core Dependencies**: Removed all `DbContext`, `AnyAsync()`, `FindAsync()`, and `Include()` logic. This belongs in the **Repository Layer**, abstracting database implementations away from the controllers.
- **Business Rule Validations**: Removed manual if-statements checking for duplicate jobs or duplicate applications. This logic moved to the **Service Layer**, allowing it to be reused elsewhere without an HTTP context.
- **Error Formatting**: Removed manual HTTP error responses (like `return BadRequest()`). This is now handled entirely by custom typed exceptions intercepted by the **GlobalExceptionHandler Middleware**.
- **Model Construction**: Removed the instantiation of domain entities (e.g. `new JobListing { ... }`). The **Service Layer** now owns building and manipulating entities before sending them to the repository.

### 3. Status Transition Design
Valid status transitions are encoded using a `static readonly Dictionary<ApplicationStatus, HashSet<ApplicationStatus>> ValidTransitions` inside the `ApplicationService`.
- **Why this mechanism**: A dictionary provides an explicit graph of allowed movements in memory with O(1) lookup time.
- **Future Changes**: Adding a new valid transition (like `Offered` -> `Accepted`) only requires adding `"Accepted"` to the `HashSet` inside the single `Offered` dictionary key. We do not need to modify long, nested switch statements or if/else chains elsewhere.

### 4. Lifetime Misconfiguration
When I deliberately registered `IJobListingService` as a `Singleton` while it depended on the `Scoped` `IJobListingRepository`, the .NET DI Container halted startup with this exact error message:
`Cannot consume scoped service 'CareerHub.Api.Repositories.IJobListingRepository' from singleton 'CareerHub.Api.Services.IJobListingService'.`
- **Why it's blocked**: A `Scoped` service is tied to a specific HTTP request, meaning it lives and dies with that request to guarantee a safe database transaction. A `Singleton` service is created once and lives forever. If a Singleton were allowed to hold a Scoped dependency (a Captive Dependency), the repository (and its DbContext connection) would be kept alive forever instead of being disposed of after the HTTP request. 
- **Runtime consequence**: At runtime, this captive DbContext would become a massive single point of failure. It would accumulate change tracker memory leading to memory leaks, and concurrent requests would attempt to use the exact same database connection simultaneously, causing threading crashes and database lock-ups.

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
