namespace Template.Api.Dtos;

public sealed record PaginatedResultDto<T>(
    IEnumerable<T> Items,
    int Page,
    int PageSize,
    int Total,
    int TotalPages
);
