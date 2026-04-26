using System.ComponentModel.DataAnnotations;

namespace _4Bet.Application.DTOs;

public class UpdateProfileDto
{
    [Required]
    [RegularExpression("^[a-zA-Zа-яА-ЯіІїЇєЄґҐ' ]+$", ErrorMessage = "First name must contain only letters")]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^[a-zA-Zа-яА-ЯіІїЇєЄґҐ' ]+$", ErrorMessage = "Last name must contain only letters")]
    public string LastName { get; set; } = string.Empty;
}
