using WorkoutLogger.Enums;

namespace WorkoutLogger.Models;
public class Workout
{
    public int Id { get; set; }
    public WorkoutDay CurrentDay { get; set; }
    public DateTimeOffset DateTime { get; set; }
    public string? Notes { get; set; }
}
