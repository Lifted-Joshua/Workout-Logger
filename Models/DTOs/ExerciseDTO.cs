using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WorkoutLogger.Models.DTOs;
public class ExerciseDto
{
    public required int Id { get; set; }
    public required string Name { get; set; }
    public required string MuscleGroup { get; set; }
}
