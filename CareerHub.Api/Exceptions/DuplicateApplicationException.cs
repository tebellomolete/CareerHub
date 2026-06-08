namespace CareerHub.Api.Exceptions;

public class DuplicateApplicationException : Exception
{
    public DuplicateApplicationException(Guid applicantId, Guid listingId)
        : base($"Applicant '{applicantId}' has already applied for listing '{listingId}'.")
    {
    }
}
