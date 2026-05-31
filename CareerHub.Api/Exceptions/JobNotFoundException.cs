namespace CareerHub.Api.Exceptions;

public class JobNotFoundException : Exception
{
    public JobNotFoundException(Guid id) 
        : base($"The job listing with ID {id} was not found.")
    {
    }
}