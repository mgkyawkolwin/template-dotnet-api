namespace Template.Api.Dtos.Users;

public sealed record UpdateUserRequestDto : UpdateRequestBase<Guid>
{
    public string? DisplayName { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
}
