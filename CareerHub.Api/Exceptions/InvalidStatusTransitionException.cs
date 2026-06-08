using CareerHub.Api.Models;

namespace CareerHub.Api.Exceptions;

public class InvalidStatusTransitionException : Exception
{
    public InvalidStatusTransitionException(ApplicationStatus currentStatus, ApplicationStatus newStatus)
        : base($"Cannot transition application status from '{currentStatus}' to '{newStatus}'.")
    {
    }
}
