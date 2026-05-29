using WorkoutLogger.Enums;

namespace WorkoutLogger.Models;
public class Workout
{
    public int Id { get; set; }
    public required WorkoutDay CurrentDay { get; set; }
    public required DateTimeOffset DateTime { get; set; }
    public string Notes { get; set; } = null!;
    public ICollection<WorkoutExercise> WorkoutExercises { get; set; } = null!;
}
