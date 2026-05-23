using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WorkoutLogger.Models.DTOs;

public class UpdateWorkoutExerciseDto
{
    public required int Sets { get; set; }
    public required int Reps { get; set; }
    public required double WeightKg { get; set; }
}
