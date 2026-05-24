using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkoutLogger.Enums;

namespace WorkoutLogger.Models.DTOs;
public class WorkoutWithExerciseDto
{
    public required WorkoutDay Day { get; init; }
    public required DateTimeOffset DateTime { get; init; }
    public string? Notes { get; init; }
    public required List<ExerciseDto> Exercises { get; init; }



}
