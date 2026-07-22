using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Template.Api.Caching;
using Template.Api.Dtos.Users;
using Template.Api.Exceptions;
using Template.Api.Services;

namespace Template.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class UsersController : BaseController
{
    private readonly IOutputCacheStore _cacheStore;
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserService userService, ILogger<UsersController> logger) : base(logger)
    {
        _userService = userService;
        _logger = logger;
    }

    [HttpGet]
    [OutputCache(PolicyName = CachePolicyKeys.StrictPerUserCache)]
    public async Task<IActionResult> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            _logger.LogDebug("CALLED: GetUsers(page={Page}, pageSize={PageSize})", page, pageSize);
            var result = await _userService.GetUsersAsync(page, pageSize);
            return Ok(new { Success = true, Data = result });
        }
        catch (CustomException ex)
        {
            _logger.LogError("Custom exception occurred: {Message}", ex.Message);
            return BadRequest(new { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred.");
            return StatusCode((int)HttpStatusCode.InternalServerError, new { Success = false, Message = "An unexpected error occurred." });
        }
    }

    [HttpGet("{id:guid}")]
    [OutputCache(PolicyName = CachePolicyKeys.StrictPerUserCache)]
    public async Task<IActionResult> GetUserById(Guid id)
    {
        try
        {
            _logger.LogDebug("CALLED: GetUserById(id={Id})", id);
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound(new { Success = false, Message = "User not found." });
            }

            return Ok(new { Success = true, Data = user });
        }
        catch (CustomException ex)
        {
            _logger.LogError("Custom exception occurred: {Message}", ex.Message);
            return BadRequest(new { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred.");
            return StatusCode((int)HttpStatusCode.InternalServerError, new { Success = false, Message = "An unexpected error occurred." });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequestDto request)
    {
        try
        {
            _logger.LogDebug("CALLED: CreateUser(request={Request})", request);
            var user = await _userService.CreateUserAsync(request, GetCurrentUserId() ?? throw new CustomException("Invalid session user."));
            await _cacheStore.EvictByTagAsync(CachePolicyKeys.StrictPerUserCache, new CancellationToken());
            return Ok(new { Success = true, Data = user });
        }
        catch (CustomException ex)
        {
            _logger.LogError("Custom exception occurred: {Message}", ex.Message);
            return BadRequest(new { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred.");
            return StatusCode((int)HttpStatusCode.InternalServerError, new { Success = false, Message = "An unexpected error occurred." });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserRequestDto request)
    {
        try
        {
            _logger.LogDebug("CALLED: UpdateUser(id={Id}, request={Request})", id, request);
            var user = await _userService.UpdateUserAsync(id, request, GetCurrentUserId() ?? throw new CustomException("Invalid session user."));
            await _cacheStore.EvictByTagAsync(CachePolicyKeys.StrictPerUserCache, new CancellationToken());
            return Ok(new { Success = true, Data = user });
        }
        catch (CustomException ex)
        {
            _logger.LogError("Custom exception occurred: {Message}", ex.Message);
            return BadRequest(new { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred.");
            return StatusCode((int)HttpStatusCode.InternalServerError, new { Success = false, Message = "An unexpected error occurred." });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        try
        {
            _logger.LogDebug("CALLED: DeleteUser(id={Id})", id);
            var deleted = await _userService.DeleteUserAsync(id);
            return Ok(new { Success = true, Message = "User deleted successfully." });
        }
        catch (CustomException ex)
        {
            _logger.LogError("Custom exception occurred: {Message}", ex.Message);
            return BadRequest(new { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred.");
            return StatusCode((int)HttpStatusCode.InternalServerError, new { Success = false, Message = "An unexpected error occurred." });
        }
    }

    [HttpPatch("{id:guid}/password")]
    public async Task<IActionResult> ChangePassword(Guid id, [FromBody] ChangePasswordRequestDto request)
    {
        try
        {
            _logger.LogDebug("CALLED: ChangePassword(id={Id})", id);
            var changed = await _userService.ChangePasswordAsync(id, request);
            return Ok(new { Success = true, Message = "Password updated successfully." });
        }
        catch (CustomException ex)
        {
            _logger.LogError("Custom exception occurred: {Message}", ex.Message);
            return BadRequest(new { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred.");
            return StatusCode((int)HttpStatusCode.InternalServerError, new { Success = false, Message = "An unexpected error occurred." });
        }
    }

    [HttpPatch("{id:guid}/profilePicture")]
    [Consumes("multipart/form-data")]
    [Route("api/users/{id:guid}/profilePicture")]
    public async Task<IActionResult> UploadProfilePicture(Guid id, IFormFile file)
    {
        try
        {
            _logger.LogDebug("CALLED: UploadProfilePicture(id={Id})", id);
            var user = await _userService.UploadProfilePictureAsync(id, file, GetCurrentUserId() ?? throw new CustomException("Invalid session user."));
            return Ok(new { Success = true, Data = user });
        }
        catch (CustomException ex)
        {
            _logger.LogError("Custom exception occurred: {Message}", ex.Message);
            return BadRequest(new { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred.");
            return StatusCode((int)HttpStatusCode.InternalServerError, new { Success = false, Message = "An unexpected error occurred." });
        }
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        try
        {
            _logger.LogDebug("CALLED: GetMe()");
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new { Success = false, Message = "Invalid token claims." });
            }

            var user = await _userService.GetUserByIdAsync(userId.Value);
            if (user == null)
            {
                return NotFound(new { Success = false, Message = "User not found." });
            }

            return Ok(new
            {
                Success = true,
                Data = new UserDto
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    DisplayName = user.DisplayName,
                    Email = user.Email,
                    Address = user.Address,
                    City = user.City,
                    Rating = user.Rating,
                    RatingCount = user.RatingCount,
                    ProfilePictureUrl = user.ProfilePictureUrl,
                    Token = null
                }
            });
        }
        catch (CustomException ex)
        {
            _logger.LogError("Custom exception occurred: {Message}", ex.Message);
            return BadRequest(new { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred.");
            return StatusCode((int)HttpStatusCode.InternalServerError, new { Success = false, Message = "An unexpected error occurred." });
        }
    }
}
