using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace WorkoutLogger.Models.DTOs;
public class LoginUserDto
{
    [Required, MinLength(1), MaxLength(12)]
    public string? Username { get; set; }

    [Required, MinLength(1), MaxLength(16)]
    public string? Password { get; set; }
}
