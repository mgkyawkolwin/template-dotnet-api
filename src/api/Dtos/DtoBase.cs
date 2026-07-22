namespace Template.Api.Dtos;

public record DtoBase
{
    public Guid? RowVersion { get; init; }
}