using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkoutLogger.Enums;

namespace WorkoutLogger.Models.DTOs;
/// <summary>
/// Dto for returning a workout with all of its exercises.
/// </summary>
public class WorkoutWithExerciseDto
{
    public required WorkoutDay Day { get; init; }
    public required DateTimeOffset DateTime { get; init; }
    public string? Notes { get; init; }
    public required List<WorkoutExerciseDetails> Exercises { get; init; }
}
