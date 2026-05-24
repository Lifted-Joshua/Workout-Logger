using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using WorkoutLogger.Enums;

namespace WorkoutLogger.Models.DTOs;

/// <summary>
/// We use this to pass into create or update workouts, so we do not pass in an id field we leave that unto the database
/// </summary>
public class WorkoutDto
{
    [Required, MinLength(1)]
    public required WorkoutDay CurrentDay { get; set; }
    [Required, MinLength(1)]
    public required DateTimeOffset DateTime { get; set; }
    public string? Notes { get; set; }
}
