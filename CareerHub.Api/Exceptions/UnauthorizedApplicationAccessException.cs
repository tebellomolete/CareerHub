namespace CareerHub.Api.Exceptions;

public class UnauthorizedApplicationAccessException : Exception
{
    public UnauthorizedApplicationAccessException(Guid applicationId)
        : base($"You are not authorized to modify application '{applicationId}'.")
    {
    }
}
