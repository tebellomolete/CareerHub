namespace CareerHub.Api.Exceptions;

public class DuplicateJobListingException : Exception
{
    public DuplicateJobListingException(string company, string title) 
        : base($"A job listing for '{title}' at '{company}' already exists.")
    {
    }
}