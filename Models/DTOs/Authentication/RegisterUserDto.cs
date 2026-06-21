using System.ComponentModel.DataAnnotations;


namespace WorkoutLogger.Models.DTOs.Authentication;
public sealed class RegisterUserDto
{
    [Required, MinLength(1), MaxLength(12)]
    public string? Username { get; set; }

    [Required, MinLength(1), MaxLength(16)]
    public string? Password { get; set; }
}
