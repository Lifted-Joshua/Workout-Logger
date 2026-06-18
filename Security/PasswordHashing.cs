using Microsoft.AspNetCore.Identity;
using WorkoutLogger.Models.Auth;
namespace WorkoutLogger.Security;
public static class PasswordHashing
{
    private static readonly PasswordHasher<string> _passwordHasher = new();
    public static string HashPassword(User user, string password)
        => HashingPassword(user, password);

    public static bool VerifyPassword(string hashedPassword, string providedPassword)
        => VerifyingPassword(hashedPassword, providedPassword);

    private static string HashingPassword(User user, string password)
    {
        var hasher = new PasswordHasher<User>();

        var hash = hasher.HashPassword(user, password);

        return hash;
    }

    private static bool VerifyingPassword(string hashedPassword, string providedPassword)
    {
        Console.WriteLine("VerifyingPassword method has been called");

        var result = _passwordHasher.VerifyHashedPassword(null,
                    hashedPassword, providedPassword);

        return result == PasswordVerificationResult.Success;
    }
}
