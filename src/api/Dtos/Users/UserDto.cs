namespace Template.Api.Dtos.Users;

public sealed record UserDto : DtoBase
{
    public Guid Id { get; set; }
    public string? DisplayName { get; set; }
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public double? Rating { get; set; }
    public int? RatingCount { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public string? Token { get; set; }
}