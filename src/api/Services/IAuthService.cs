using Template.Api.Dtos.Auth;

namespace Template.Api.Services;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterDto dto);
    Task<AuthResponseDto> SignInAsync(LoginDto dto);
    Task<AuthResponseDto> SignInWithGoogleAsync(GoogleLoginDto dto);
}
