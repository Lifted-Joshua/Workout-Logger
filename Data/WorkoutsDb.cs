using System;
using Microsoft.EntityFrameworkCore;
using WorkoutLogger.Enums;
using WorkoutLogger.Models;

namespace WorkoutLogger.Data;
public class WorkoutsDb : DbContext
{
    public WorkoutsDb(DbContextOptions<WorkoutsDb> options) :base(options)
    { }

    public DbSet<Workout> Workouts { get; set; }
    public DbSet<Exercise> Exercises { get; set; }
    public DbSet<WorkoutExercise> WorkoutExercises { get; set; }

    /// <summary>
    /// Configuring our entity classes using ModelBuilder provided by FluentAPI
    /// </summary>
    /// <param name="modelBuilder"></param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Workout>(entity =>
        {
            // Defines table name for Workout Entity
            entity.ToTable("Workouts");

            //Defines Workout.Id as primary key
            entity.HasKey(x => x.Id);

            // Setting this properties as required
            entity.Property(x => x.CurrentDay)
                .IsRequired();

            // Setting this property as required
            entity.Property(x => x.DateTime)
                .IsRequired();

            // Setting this property as required with a max length
            entity.Property(x => x.Notes)
                .IsRequired()
                .HasMaxLength(500);

            // Defining One to Many relationship Workout has with WorkoutExercises
            entity.HasMany(x => x.WorkoutExercises)
                .WithOne(x => x.Workout)
                .HasForeignKey(x => x.WorkoutId)
                .IsRequired();

            entity.HasData(
                new Workout { Id = 1, CurrentDay = WorkoutDay.Monday, DateTime = DateTimeOffset.UtcNow, Notes = "Push Day"},
                new Workout { Id = 2, CurrentDay = WorkoutDay.Wednesday, DateTime = DateTimeOffset.UtcNow, Notes = "Pull Day"},
                new Workout { Id = 3, CurrentDay = WorkoutDay.Friday, DateTime = DateTimeOffset.UtcNow, Notes = "Leg Day"}
            );
        });

        modelBuilder.Entity<Exercise>(entity =>
        {
            // Defining table name
            entity.ToTable("Exercises");

            // Defining primary key
            entity.HasKey(x => x.Id);

            // Defining requirements for name property
            entity.Property(x => x.Name)
                .HasMaxLength(50)
                .IsRequired();

            // Defining requirements for muscle group property
            entity.Property(x => x.MuscleGroup)
                .HasMaxLength(25)
                .IsRequired();

            entity.HasMany(x => x.WorkoutExercises)
                .WithOne(x => x.Exercise)
                .HasForeignKey(x => x.ExerciseId)
                .IsRequired();

            entity.HasData
            (
                new Exercise { Id = 1, Name = "Bench Press", MuscleGroup = "Chest"},
                new Exercise { Id = 2, Name = "Overhead Shoulder Press", MuscleGroup = "Shoulders" },
                new Exercise { Id = 3, Name = "Pull Ups", MuscleGroup = "Back" },
                new Exercise { Id = 4, Name = "Barbell Rows", MuscleGroup = "Back" },
                new Exercise { Id = 5, Name = "Barbell Squat", MuscleGroup = "Quadriceps" },
                new Exercise { Id = 6, Name = "Romanian Deadlift", MuscleGroup = "Hamstrings" }
            );
        });

        modelBuilder.Entity<WorkoutExercise>(entity =>
        {

            entity.HasData
            (
                new WorkoutExercise { Id = 1, WorkoutId = 1, ExerciseId = 1, Sets = 4, Reps = 10, WeightKg = 90},
                new WorkoutExercise { Id = 2, WorkoutId = 1, ExerciseId = 2, Sets = 4, Reps = 12, WeightKg = 25},
                new WorkoutExercise { Id = 3, WorkoutId = 2, ExerciseId = 3, Sets = 3, Reps = 5, WeightKg = 115},
                new WorkoutExercise { Id = 4, WorkoutId = 2, ExerciseId = 4, Sets = 5, Reps = 5, WeightKg = 85},
                new WorkoutExercise { Id = 5, WorkoutId = 3, ExerciseId = 5, Sets = 6, Reps = 8, WeightKg = 120},
                new WorkoutExercise { Id = 6, WorkoutId = 3, ExerciseId = 6, Sets = 5, Reps = 6, WeightKg = 140}
            );

            entity.ToTable("WorkoutExercises", table =>
            {
                table.HasCheckConstraint("CK_Sets_Range", "\"Sets\" >= 0 AND \"Sets\" <= 10");
                table.HasCheckConstraint("CK_Reps_Range", "\"Reps\" >= 0 AND \"Reps\" <= 100");
                table.HasCheckConstraint("CK_WeightsKg_Range", "\"WeightKg\" >= 0 AND \"WeightKg\" <= 1000");
            });

            //Defining primary key
            entity.HasKey(x => x.Id);

            // Defining One to Many relationship and foreign Keys
            entity.HasOne(x => x.Workout) // Navigation Property
                .WithMany(x => x.WorkoutExercises) // Navigation property collection
                .HasForeignKey(x => x.WorkoutId); // Foreign key

            // Defining One to Many relationship and foreign Keys
            entity.HasOne(x => x.Exercise) // Navigation property
                .WithMany(x => x.WorkoutExercises) // Navigation property collection
                .HasForeignKey(x => x.ExerciseId); // Foreign key

            // Defining composite key. No Duplicate combinations allowed
            entity.HasIndex(x => new { x.WorkoutId, x.ExerciseId}).IsUnique();


            entity.Property(x => x.Sets)
                .IsRequired();

            entity.Property(x => x.Reps)
                .IsRequired();

            entity.Property(x => x.WeightKg)
                .IsRequired();

        });
    }
}
