using System;
using System.ComponentModel.DataAnnotations;

namespace WorkoutLogger.Models.DTOs;

public class UpdateWorkoutExerciseDto
{
    [Required, MinLength(1)]
    public required int Sets { get; set; }
    [Required, MinLength(1)]
    public required int Reps { get; set; }
    [Required, MinLength(1)]
    public required double WeightKg { get; set; }
}
