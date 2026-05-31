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
