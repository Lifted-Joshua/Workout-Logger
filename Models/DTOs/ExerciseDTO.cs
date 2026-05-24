using System;
using System.ComponentModel.DataAnnotations;

namespace WorkoutLogger.Models.DTOs;
public class ExerciseDto
{
    [Required, MinLength(1)]
    public required string Name { get; set; }
    [Required, MinLength(1)]
    public required string MuscleGroup { get; set; }
}
