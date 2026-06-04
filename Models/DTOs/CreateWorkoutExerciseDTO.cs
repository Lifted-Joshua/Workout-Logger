using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace WorkoutLogger.Models.DTOs;
public class CreateWorkoutExerciseDto
{
    public int ExerciseId { get; set; }
    [Range(1, 10)]
    public int Sets { get; set; }
    [Range(1, 100)]
    public int Reps { get; set; }
    [Range(1, 1000)]
    public double WeightKg { get; set; }
}
