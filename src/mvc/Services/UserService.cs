using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Template.Api.Data;
using Template.Api.Dtos;
using Template.Api.Dtos.Users;
using Template.Api.Entities;
using Template.Api.Exceptions;
using Template.Api.Utilities;

namespace Template.Api.Services;

public interface IUserService
{
    Task<PaginatedResultDto<UserDto>> GetUsersAsync(int page, int pageSize);
    Task<UserDto?> GetUserByIdAsync(Guid id);
    Task<UserDto> CreateUserAsync(CreateUserRequestDto request, Guid currentUserId);
    Task<UserDto?> UpdateUserAsync(Guid id, UpdateUserRequestDto request, Guid currentUserId);
    Task<bool> DeleteUserAsync(Guid id);
    Task<bool> ChangePasswordAsync(Guid id, ChangePasswordRequestDto request);
    Task<UserDto?> UploadProfilePictureAsync(Guid id, IFormFile file, Guid currentUserId);
}

public class UserService : IUserService
{
    private readonly AppDbContext _dbContext;
    private readonly IPasswordHasher<UserEntity> _passwordHasher;
    private readonly IStorageService _storageService;
    private readonly ILogger<UserService> _logger;

    public UserService(AppDbContext dbContext, IPasswordHasher<UserEntity> passwordHasher, IStorageService storageService, ILogger<UserService> logger)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _storageService = storageService;
        _logger = logger;
    }

    public async Task<PaginatedResultDto<UserDto>> GetUsersAsync(int page, int pageSize)
    {
        _logger.LogDebug("CALLED: GetUsersAsync(page={Page}, pageSize={PageSize})", page, pageSize);
        page = PaginationHelper.NormalizePage(page);
        pageSize = PaginationHelper.NormalizePageSize(pageSize);

        var query = _dbContext.Users.AsNoTracking().OrderByDescending(u => u.CreatedAtUtc);
        var total = await query.CountAsync();

        var users = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(user => new UserDto
            {
                Id = user.Id,
                UserName = user.UserName,
                DisplayName = user.DisplayName,
                Email = user.Email,
                Address = user.Address,
                City = user.City,
                ProfilePictureUrl = user.ProfilePictureUrl
            })
            .ToListAsync();

        return new PaginatedResultDto<UserDto>(users, page, pageSize, total, PaginationHelper.CalculateTotalPages(total, pageSize));
    }

    public async Task<UserDto?> GetUserByIdAsync(Guid id)
    {
        _logger.LogDebug("CALLED: GetUserByIdAsync(id={Id})", id);
        var user = await _dbContext.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == id);
        return user == null ? null : MapToDto(user);
    }

    public async Task<UserDto> CreateUserAsync(CreateUserRequestDto request, Guid currentUserId)
    {
        _logger.LogDebug("CALLED: CreateUserAsync(request={Request})", request);
        ValidationHelper.ValidateRequiredString(request.UserName, "UserName");
        ValidationHelper.ValidateEmail(request.Email);
        ValidationHelper.ValidateRequiredString(request.Password, "Password");

        var normalizedUserName = request.UserName.Trim();
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        if (await _dbContext.Users.AnyAsync(u => u.UserName == normalizedUserName || u.Email == normalizedEmail))
        {
            throw new CustomException("A user with this username or email already exists.");
        }

        var user = new UserEntity
        {
            UserName = normalizedUserName,
            DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? normalizedUserName : request.DisplayName.Trim(),
            Email = normalizedEmail,
            Address = request.Address,
            City = request.City,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedById = currentUserId,
            UpdatedAtUtc = DateTime.UtcNow,
            UpdatedById = currentUserId
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password!);
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        _logger.LogTrace("Created user {UserName} with id {UserId}", user.UserName, user.Id);
        return MapToDto(user);
    }

    public async Task<UserDto?> UpdateUserAsync(Guid id, UpdateUserRequestDto request, Guid currentUserId)
    {
        _logger.LogDebug("CALLED: UpdateUserAsync(id={Id}, request={Request})", id, request);
        ValidationHelper.ValidateEmail(request.Email);
        ValidationHelper.ValidateRequiredString(request.DisplayName, "DisplayName");

        var user = await _dbContext.Users.SingleOrDefaultAsync(u => u.Id == id);
        if (user == null)
        {
            throw new CustomException("User not found.");
        }

        if (!string.IsNullOrWhiteSpace(request.DisplayName))
        {
            user.DisplayName = request.DisplayName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();
            var emailExists = await _dbContext.Users.AnyAsync(u => u.Id != id && u.Email == normalizedEmail);
            if (emailExists)
            {
                throw new CustomException("A user with this email already exists.");
            }

            user.Email = normalizedEmail;
        }

        user.Address = request.Address;
        user.City = request.City;
        user.UpdatedAtUtc = DateTime.UtcNow;
        user.UpdatedById = currentUserId;
        await _dbContext.SaveChangesAsync();

        return MapToDto(user);
    }

    public async Task<bool> DeleteUserAsync(Guid id)
    {
        _logger.LogDebug("CALLED: DeleteUserAsync(id={Id})", id);
        var user = await _dbContext.Users.SingleOrDefaultAsync(u => u.Id == id) ?? throw new CustomException("User not found.");
        _dbContext.Users.Remove(user);
        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ChangePasswordAsync(Guid id, ChangePasswordRequestDto request)
    {
        _logger.LogDebug("CALLED: ChangePasswordAsync(id={Id})", id);
        ValidationHelper.ValidateRequiredString(request.CurrentPassword, "CurrentPassword");
        ValidationHelper.ValidateRequiredString(request.NewPassword, "NewPassword");

        var user = await _dbContext.Users.SingleOrDefaultAsync(u => u.Id == id);
        if (user == null)
        {
            throw new CustomException("User not found.");
        }

        var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword);
        if (verificationResult == PasswordVerificationResult.Failed)
        {
            throw new CustomException("Current password is incorrect.");
        }

        user.PasswordHash = _passwordHasher.HashPassword(user, request.NewPassword);
        user.UpdatedAtUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<UserDto?> UploadProfilePictureAsync(Guid id, IFormFile file, Guid currentUserId)
    {
        _logger.LogDebug("CALLED: UploadProfilePictureAsync(id={Id}, file={FileName})", id, file?.FileName);
        if (file == null || file.Length == 0)
        {
            throw new CustomException("A file is required.");
        }

        var user = await _dbContext.Users.SingleOrDefaultAsync(u => u.Id == id) ?? throw new CustomException("User not found.");
        
        var objectName = await _storageService.UploadFileAsync(file);
        user.ProfilePictureUrl = objectName;
        user.UpdatedAtUtc = DateTime.UtcNow;
        user.UpdatedById = currentUserId;
        await _dbContext.SaveChangesAsync();

        return MapToDto(user);
    }

    private static UserDto MapToDto(UserEntity user)
    {
        return new UserDto
        {
            Id = user.Id,
            UserName = user.UserName,
            DisplayName = user.DisplayName,
            Email = user.Email,
            Address = user.Address,
            City = user.City,
            ProfilePictureUrl = user.ProfilePictureUrl
        };
    }
}
