namespace CareerHub.Api.Exceptions;

public class CompanyNotFoundException : Exception
{
    public CompanyNotFoundException(Guid companyId)
        : base($"Company '{companyId}' was not found.")
    {
    }
}
