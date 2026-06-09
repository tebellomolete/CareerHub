using CareerHub.Api.Models;

namespace CareerHub.Api.DTOs;

public record UpdateApplicationStatusRequest(
    ApplicationStatus Status
);
