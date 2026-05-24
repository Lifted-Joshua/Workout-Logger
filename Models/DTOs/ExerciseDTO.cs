using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WorkoutLogger.Models.DTOs;
public class ExerciseDto
{
    public required string Name { get; set; }
    public required string MuscleGroup { get; set; }
    public required int Sets { get; set; }
    public required int Reps { get; set; }
    public required double WeightKg { get; set; }

}
