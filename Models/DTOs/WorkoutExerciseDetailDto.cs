using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace WorkoutLogger.Models.DTOs;
/// <summary>
/// Output/ Response Dto
/// </summary>
public class WorkoutExerciseDetailDto
{
    public required string Name { get; set; }
    public required string MuscleGroup { get; set; }
    public required int Sets { get; set; }
    public required int Reps { get; set; }
    public required double WeightKg { get; set; }

}
