namespace Template.Api.Dtos.Auth;

public sealed record RegisterDto(
    string UserName,
    string DisplayName,
    string Password,
    string? Email,
    string? Phone
);
