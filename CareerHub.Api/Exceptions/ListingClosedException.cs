namespace CareerHub.Api.Exceptions;

public class ListingClosedException : Exception
{
    public ListingClosedException(Guid listingId)
        : base($"Job listing '{listingId}' is closed for new applications or updates.")
    {
    }
}
