using System;
using System.ComponentModel.DataAnnotations;

namespace WorkoutLogger.Models.DTOs;

public class UpdateWorkoutExerciseDto
{
    [Range(1, 10)]
    public int Sets { get; set; }
    [Range(1, 100)]
    public int Reps { get; set; }
    [Range(1, 1000)]
    public double WeightKg { get; set; }
}
