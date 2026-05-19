using System;
using Microsoft.EntityFrameworkCore;
using WorkoutLogger.Models;

namespace WorkoutLogger.Data;
public class WorkoutsDb : DbContext
{
    public WorkoutsDb(DbContextOptions<WorkoutsDb> options) :base(options)
    { }

    public DbSet<Workout> Workouts { get; set; }
    public DbSet<Exercise> Exercises { get; set; }
    public DbSet<WorkoutExercise> WorkoutExercises { get; set; }
}
