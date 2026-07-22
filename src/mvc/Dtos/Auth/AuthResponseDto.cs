using Template.Api.Dtos.Users;

namespace Template.Api.Dtos.Auth;

public sealed record AuthResponseDto(
    string Token,
    UserDto User
);
