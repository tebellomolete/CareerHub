namespace CareerHub.Api.DTOs;

public record PagedResponse<T>(
    IEnumerable<T> Data,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages,
    bool HasNextPage,
    bool HasPreviousPage
);
