namespace Template.Api.Dtos.Users;

public sealed record CreateUserRequestDto
{
    public string UserName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Password { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
}
