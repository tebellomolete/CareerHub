namespace CareerHub.Api.Exceptions;

public class UnauthorizedListingUpdateException : Exception
{
    public UnauthorizedListingUpdateException(Guid listingId)
        : base($"You are not authorized to update job listing '{listingId}'.")
    {
    }
}
