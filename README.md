# CareerHub API

Welcome to the CareerHub API. This repository contains the backend services for a modern job board application, built using ASP.NET Core.

## Assignment 1.2: Design Decisions & Architecture

As part of evolving the read-only CareerHub API into a robust system capable of creating, updating, and deleting job listings, several key architectural decisions were made to enforce data contracts, protect server-owned state, and standardize error handling.

### 1. The `PostedAt` Field and DTO Separation

The `PostedAt` field represents metadata that the system generates at the exact moment a record is processed. It belongs in the `JobResponse` so the React frontend can accurately display how long a listing has been active. However, it is explicitly excluded from the `CreateJobRequest` to maintain the absolute integrity of our data. If clients were allowed to submit this field, a malicious or malfunctioning client could backdate or future-date job listings, making the job board's timeline completely untrustworthy.

### 2. Salary Cross-Field Validation

To enforce that `SalaryMax` is strictly greater than `SalaryMin`, I implemented the `IValidatableObject` interface directly on the `CreateJobRequest` (and inherited it in `UpdateJobRequest`). I chose this approach because it hooks seamlessly into the native .NET model binding pipeline. This ensures the cross-field validation triggers automatically alongside the standard Data Annotations (like `[Required]`), immediately returning a 400 Bad Request if it fails. This keeps the controller completely clean and focused solely on handling the HTTP request rather than manual validation logic.

### 3. PUT Status Code Choice

For the PUT endpoint, I chose to return a `200 OK` accompanied by the updated `JobResponse` body, rather than a `204 No Content`. Because our API is responsible for mapping the DTO and computing the human-readable `SalaryDisplay` string, returning the updated response allows the client to immediately receive and render these calculated fields. If I had returned a `204`, the frontend would have to fire a secondary GET request just to figure out how the server formatted the newly updated salary data.

### 4. DELETE Behaviour for a Missing ID

If a client sends a DELETE request for a UUID that does not exist in the store, the API returns a `404 Not Found`. On a platform like a job board, there is a real possibility of concurrent actions—for example, two administrators might attempt to delete an expired listing at the exact same time. If the API returned a `204 No Content` for a missing ID, the second admin would falsely assume their specific command was the one that successfully executed the deletion. A 404 explicitly and correctly informs them that the resource was already gone before their request arrived.
