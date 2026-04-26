using System.ComponentModel.DataAnnotations;

namespace _4Bet.Application.DTOs;

public class UpdateAvatarDto
{
    [Required]
    [MaxLength(3_000_000)]
    public string AvatarDataUrl { get; set; } = string.Empty;
}
