using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Template.Api.Dtos.Auth;
using Template.Api.Services;
using Template.Api.Exceptions;
using Template.Api.Dtos;

namespace Template.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<ActionResult> Register(RegisterDto dto)
    {
        try
        {
            _logger.LogDebug("CALLED: Register(dto={Dto})", JsonSerializer.Serialize(dto));
            var response = await _authService.RegisterAsync(dto);
            return Ok(new { Success = true, Data = response });
        }
        catch (CustomException ex)
        {
            _logger.LogError(ex, "Custom Exception: {message}", ex.Message);
            return StatusCode((int)HttpStatusCode.BadRequest, new  { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected Error Occured.");
            return StatusCode((int)HttpStatusCode.InternalServerError, new { Success = false, Message = "An error occurred while registering the user." });
        }
    }

    [HttpPost("signin")]
    public async Task<ActionResult<AuthResponseDto>> SignIn(LoginDto dto)
    {
        try
        {
            _logger.LogDebug("CALLED: SignIn(dto={Dto})", JsonSerializer.Serialize(dto));
            var response = await _authService.SignInAsync(dto);
            _logger.LogTrace("AuthResponseDto: {Response}", JsonSerializer.Serialize(response));
            return Ok(new {Success = true, Data = response});
        }
        catch (CustomException ex)
        {
            _logger.LogError(ex, "Custom Exception: {message}", ex.Message);
            return StatusCode((int)HttpStatusCode.BadRequest, new { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected Error Occured.");
            return StatusCode((int)HttpStatusCode.InternalServerError, new { Success = false, Message = "An error occurred while signing in." });
        }
    }

    [HttpPost("google")]
    public async Task<ActionResult<AuthResponseDto>> GoogleSignIn(GoogleLoginDto dto)
    {
        try
        {
            _logger.LogDebug("CALLED: GoogleSignIn(dto={Dto})", JsonSerializer.Serialize(dto));
            var response = await _authService.SignInWithGoogleAsync(dto);
            _logger.LogTrace("Google sign-in successful for user {Email}", response.User.Email);
            _logger.LogTrace("AuthResponseDto: {Response}", JsonSerializer.Serialize(response));
            return Ok(new { Success = true, Data = response });
        }
        catch (CustomException ex)
        {
            _logger.LogError(ex, "Custom Exception: {message}", ex.Message);
            return StatusCode((int)HttpStatusCode.BadRequest, new { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected Error Occured.");
            return StatusCode((int)HttpStatusCode.InternalServerError, new { Success = false, Message = "An error occurred while signing in with Google." });
        }
    }
}
