namespace WorkoutLogger.Models.Auth;
public sealed class User
{
    public int Id { get; set; }
    public string UserName { get; set; } = null!;
    public string HashedPassword { get; set; } = null!;
    public ICollection<Workout> Workouts { get; set; } = null!;
}
