using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Template.Api.Entities;

[Table("Users")]
public class UserEntity : EntityBase<Guid>
{
    [Required]
    [MaxLength(20)]
    public required string UserName { get; set; }

    [Required]
    [MaxLength(100)]
    public required string DisplayName { get; set; }

    [Required]
    [EmailAddress]
    [MaxLength(50)]
    public required string Email { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(500)]
    public string? ProfilePictureUrl { get; set; }

    [Required]
    [MaxLength(256)]
    public string PasswordHash { get; set; } = null!;

}
