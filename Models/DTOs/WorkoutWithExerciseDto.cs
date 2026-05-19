using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkoutLogger.Enums;

namespace WorkoutLogger.Models.DTOs;
public class WorkoutWithExerciseDto
{
    public WorkoutDay Day { get; set; }
    public DateTimeOffset DateTime { get; set; }
    public List<ExerciseDTO> Exercises { get; set; }

    public WorkoutWithExerciseDto(WorkoutDay day, DateTimeOffset dateTime, List<ExerciseDTO> exercises)
    {
        this.Day = day;
        this.DateTime = dateTime;
        this.Exercises = exercises;
    }

}
