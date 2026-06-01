using System;
using System.ComponentModel.DataAnnotations;

namespace WorkoutLogger.Models.DTOs;
/// <summary>
/// Input exercise dto - create specifies input
/// </summary>
public class CreateExerciseDto
{
    [Required, MinLength(1)]
    public string? Name { get; set; }
    [Required, MinLength(1)]
    public string? MuscleGroup { get; set; }
}
