using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WorkoutLogger.Models;
public class Exercise
{
    public int Id { get; set; }
    public required string Name { get; set; } = string.Empty;
    public required string MuscleGroup { get; set; } = string.Empty;
}
