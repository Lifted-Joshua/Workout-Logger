namespace WorkoutLogger.Models;
public class WorkoutExercise
{
    // Primary key definition
    public int Id { get; set; }
    // Foreign Key Definition
    public required int WorkoutId { get; set; }
    // Foreign Key Definition
    public required int ExerciseId { get; set; }
    public required int Sets { get; set; }
    public required int Reps { get; set; }
    public required double WeightKg { get; set; }
    // Navigation property to the related Workout
    public Workout Workout { get; set; } = null!;
    // Navigation property to the related exercise
    public Exercise Exercise { get; set; } = null!;
}