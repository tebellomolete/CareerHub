namespace CareerHub.Api.Models;

public class Applicant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    public ICollection<Application> Applications { get; set; } = new List<Application>();
}
