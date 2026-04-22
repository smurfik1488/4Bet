using _4Bet.Infrastructure.Domain;

namespace _4Bet.Application.DTOs;

public class UserRegistrationDto
{
    public required string Email { get; set; }
    public required string Password { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public DateTime Birthday { get; set; }
    public UserRole Role { get; set; }
}