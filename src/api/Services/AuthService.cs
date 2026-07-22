using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Template.Api.Data;
using Template.Api.Dtos.Auth;
using Template.Api.Dtos.Users;
using Template.Api.Entities;
using Template.Api.Exceptions;
using Template.Api.Models;
using Template.Api.Utilities;
using System.Text.Json;
using Microsoft.Extensions.Localization;
using Template.Api.I18N;

namespace Template.Api.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _dbContext;
    private readonly IPasswordHasher<UserEntity> _passwordHasher;
    private readonly ILogger<AuthService> _logger;
    private readonly JwtSettings _jwtSettings;
    private readonly GoogleAuthSettings _googleAuthSettings;
    private readonly IStringLocalizer<LocalizedStrings> _localizer;

    public AuthService(AppDbContext dbContext, IPasswordHasher<UserEntity> passwordHasher, ILogger<AuthService> logger, JwtSettings jwtSettings, GoogleAuthSettings googleAuthSettings, IStringLocalizer<LocalizedStrings> localizer)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _logger = logger;
        _jwtSettings = jwtSettings;
        _googleAuthSettings = googleAuthSettings;
        _localizer = localizer;
    }

    private string CreateJwtToken(UserEntity user)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Name, user.UserName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiresInMinutes);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
    {
        _logger.LogDebug("CALLED: RegisterAsync(dto={Dto})", JsonSerializer.Serialize(dto));
        ValidationHelper.ValidateRequiredString(_localizer, "Name", dto.UserName);
        ValidationHelper.ValidateEmail(_localizer, "Email", dto.Email);
        ValidationHelper.ValidateRequiredString(_localizer, "Password", dto.Password);

        var normalizedName = dto.UserName.Trim();
        var normalizedEmail = string.IsNullOrWhiteSpace(dto.Email) ? string.Empty : dto.Email.Trim().ToLowerInvariant();

        if (await _dbContext.Users.AnyAsync(u => u.UserName == normalizedName || (!string.IsNullOrEmpty(normalizedEmail) && u.Email == normalizedEmail)))
        {
            _logger.LogWarning("Registration conflict for user {Name}", dto.UserName);
            throw new CustomException(_localizer[$"Template.AlreadyExists", "User"]);
        }

        var user = new UserEntity
        {
            UserName = normalizedName,
            DisplayName = string.IsNullOrWhiteSpace(dto.DisplayName) ? normalizedName : dto.DisplayName,
            Email = normalizedEmail,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, dto.Password);
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();
        _logger.LogTrace("User entity persisted with id {UserId}", user.Id);

        var token = CreateJwtToken(user);
        var response = new AuthResponseDto(
            token,
            new UserDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                Address = user.Address,
                City = user.City,
                ProfilePictureUrl = user.ProfilePictureUrl,
                Token = token
            }
        );
        _logger.LogTrace("AuthResponseDto: {@Response}", response);
        return response;
    }

    public async Task<AuthResponseDto> SignInAsync(LoginDto dto)
    {
        _logger.LogDebug("CALLED: SignInAsync(dto={Dto})", JsonSerializer.Serialize(dto));
        ValidationHelper.ValidateRequiredString(_localizer, "Username", dto.Username);
        ValidationHelper.ValidateRequiredString(_localizer, "Password", dto.Password);

        var normalizedUsername = dto.Username.Trim();
        var normalizedEmail = normalizedUsername.Contains('@') ? normalizedUsername.ToLowerInvariant() : null;
        var normalizedName = normalizedUsername.ToLowerInvariant();
        var user = await _dbContext.Users.SingleOrDefaultAsync(u => (normalizedEmail != null && u.Email == normalizedEmail) || u.UserName.ToLower() == normalizedName);
        if (user == null)
        {
            _logger.LogWarning("Sign-in failed for unknown username {Username}", dto.Username);
            throw new CustomException(_localizer[$"Template.Incorrect", "Username or password"]);
        }

        var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
        if (verificationResult == PasswordVerificationResult.Failed)
        {
            _logger.LogWarning("Sign-in failed for username {Username}: invalid password", dto.Username);
            throw new CustomException(_localizer[$"Template.Incorrect", "Username or password"]);
        }

        _logger.LogTrace("User {Username} authenticated successfully", dto.Username);

        var token = CreateJwtToken(user);
        var response = new AuthResponseDto(
            token,
            new UserDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                Address = user.Address,
                City = user.City,
                ProfilePictureUrl = user.ProfilePictureUrl,
                Token = token
            }
        );
        _logger.LogTrace("AuthResponseDto: {@Response}", response);
        return response;
    }

    public async Task<AuthResponseDto> SignInWithGoogleAsync(GoogleLoginDto dto)
    {
        _logger.LogDebug("CALLED: SignInWithGoogleAsync(dto={Dto})", JsonSerializer.Serialize(dto));

        var tokenInfo = await ValidateGoogleIdTokenAsync(dto.IdToken);
        if (tokenInfo == null)
        {
            _logger.LogWarning("Google token validation failed.");
            throw new CustomException("Invalid Google sign-in token.");
        }

        if (!string.Equals(tokenInfo.Aud, _googleAuthSettings.ClientId, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Google token audience mismatch: {Aud}", tokenInfo.Aud);
            throw new CustomException("Invalid Google sign-in token.");
        }

        if (tokenInfo.Iss != "https://accounts.google.com" && tokenInfo.Iss != "accounts.google.com")
        {
            _logger.LogWarning("Google token issuer mismatch: {Issuer}", tokenInfo.Iss);
            throw new CustomException("Invalid Google sign-in token.");
        }

        if (!bool.TryParse(tokenInfo.EmailVerified, out var emailVerified) || !emailVerified)
        {
            _logger.LogWarning("Google email not verified for token: {Email}", tokenInfo.Email);
            throw new CustomException("Google email must be verified.");
        }

        var normalizedEmail = tokenInfo.Email.Trim().ToLowerInvariant();
        var user = await _dbContext.Users.SingleOrDefaultAsync(u => u.Email == normalizedEmail);

        if (user == null)
        {
            user = new UserEntity
            {
                UserName = tokenInfo.Name ?? tokenInfo.Email,
                DisplayName = tokenInfo.Name ?? tokenInfo.Email,
                Email = normalizedEmail,
                ProfilePictureUrl = tokenInfo.Picture,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
            };
            user.PasswordHash = _passwordHasher.HashPassword(user, Guid.NewGuid().ToString("N"));

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Created new user from Google sign-in: {Email}", user.Email);
        }
        else
        {
            var updated = false;
            if (!string.IsNullOrEmpty(tokenInfo.Name) && user.UserName != tokenInfo.Name)
            {
                user.UserName = tokenInfo.Name;
                updated = true;
            }
            if (!string.IsNullOrEmpty(tokenInfo.Picture) && user.ProfilePictureUrl != tokenInfo.Picture)
            {
                user.ProfilePictureUrl = tokenInfo.Picture;
                updated = true;
            }

            if (updated)
            {
                _dbContext.Users.Update(user);
                await _dbContext.SaveChangesAsync();
            }
        }

        var token = CreateJwtToken(user);
        var response = new AuthResponseDto(
            token,
            new UserDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                Address = user.Address,
                City = user.City,
                ProfilePictureUrl = user.ProfilePictureUrl,
                Token = token
            }
        );
        _logger.LogTrace("Google auth response created for {Email}", tokenInfo.Email);
        return response;
    }

    private async Task<GoogleIdTokenInfo?> ValidateGoogleIdTokenAsync(string idToken)
    {
        using var httpClient = new HttpClient();
        var uri = $"https://oauth2.googleapis.com/tokeninfo?id_token={Uri.EscapeDataString(idToken)}";
        var tokenInfo = await httpClient.GetFromJsonAsync<GoogleIdTokenInfo>(uri);
        return tokenInfo;
    }

    private sealed record GoogleIdTokenInfo(
        string Aud,
        string Iss,
        string Email,
        string EmailVerified,
        string? Name,
        string? Picture
    );
}
