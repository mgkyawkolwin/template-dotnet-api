using System;
using System.ComponentModel.DataAnnotations;

namespace Template.Api.Entities;

public abstract class EntityBase<TKey>
    where TKey : struct
{
    [Key]
    public TKey Id { get; set; } = default!;
    [Required]
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    [Required]
    public TKey CreatedById { get; set; } = default;
    [Required]
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    [Required]
    public TKey UpdatedById { get; set; } = default;
}
