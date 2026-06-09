using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using WorkoutLogger.Models.Auth;

namespace WorkoutLogger.Security;
public static class PasswordHashing
{

    public static string HashPassword(User user, string password) => HashingPassword(user, password);

    private static string HashingPassword(User user, string password)
    {
        var hasher = new PasswordHasher<User>();

        var hash = hasher.HashPassword(user, password);

        return hash;
    }
}
