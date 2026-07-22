namespace Template.Api.Dtos;

public record UpdateRequestBase<T>
{
    public Guid RowVersion { get; init; }
}