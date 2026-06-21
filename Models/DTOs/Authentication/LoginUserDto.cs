using System.ComponentModel.DataAnnotations;

namespace WorkoutLogger.Models.DTOs.Authentication;
/// <summary>
/// NonInhertiable class used a model for logging in users.
/// </summary>
public sealed class LoginUserDto
{
    [Required, MinLength(1), MaxLength(12)]
    public string? Username { get; set; }

    [Required, MinLength(1), MaxLength(16)]
    public string? Password { get; set; }
}
