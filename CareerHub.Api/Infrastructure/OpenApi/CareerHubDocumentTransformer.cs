using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace API.Infrastructure.OpenApi;

public class CareerHubDocumentTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Info.Title       = "Conference Booking API";
        document.Info.Version     = "v1";
        document.Info.Description =
            "Manages job listings. " +
            "Public endpoints require no authentication. " +
            "Write operations require a Bearer token.";

        document.Info.Contact = new OpenApiContact
        {
            Name  = "Platform Team",
            Email = "platform@example.com"
        };

        return Task.CompletedTask;
    }
}
