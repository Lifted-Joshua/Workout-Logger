using WorkoutLogger.Enums;
using WorkoutLogger.Models.Auth;

namespace WorkoutLogger.Models;
public class Workout
{
    public int Id { get; set; }
    public required int UserId { get; set; }
    public required WorkoutDay CurrentDay { get; set; }
    public required DateTimeOffset DateTime { get; set; }
    public string Notes { get; set; } = null!;
    public User User { get; set; } = null!;
    public ICollection<WorkoutExercise> WorkoutExercises { get; set; } = null!;
}