using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WorkoutLogger.Models.DTOs;
public class WorkoutExercisesDto
{
    public required int Id { get; set; }
    public required int WorkoutId { get; set; }
    public required int ExerciseId { get; set; }
    public required int Sets { get; set; }
    public required int Reps { get; set; }
    public required double WeightKg { get; set; }
}
