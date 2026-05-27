namespace CareerHub.Api.Models;

public record JobListing
(
    Guid Id, 
    string Title, 
    string Description, 
    string Company, 
    string Location, 
    string Type
);