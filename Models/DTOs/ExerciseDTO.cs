using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WorkoutLogger.Models.DTOs;
public class ExerciseDTO
{
    public string Name { get; set; } = string.Empty;
    public string MuscleGroup { get; set; } = string.Empty;

    public ExerciseDTO(Exercise exercise)
    {
        this.Name = exercise.Name;
        this.MuscleGroup = exercise.MuscleGroup;
    }
}
