using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WorkoutLogger.Models;
public class Exercise
{
    public int Id { get; set; }
    public required string Name { get; set; } = null!;
    public required string MuscleGroup { get; set; } = null!;
    public ICollection<WorkoutExercise> WorkoutExercises { get; set; } = null!;
}
