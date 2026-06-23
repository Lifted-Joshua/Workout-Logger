using System.Text;
using Microsoft.IdentityModel.Tokens;
using WorkoutLogger.Models.JWT;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Options;


namespace WorkoutLogger.Jwt;
public class JwtTokenHelper
{
    protected JwtTokenHelper()
    {
    }
    public static string GenerateJwtToken(int id, string username, IOptions<JwtOptions> jwtOptions)
    {
        // Confirm if user name is empty
        if(string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Usernmae cannot be null");

        // This code sets everything needed for the signature to be generated later
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Value.Key)); // Creating the key
        //Defines how to sign the key - when you sign this token, use this key and this algorithm (HMAC-SHA256)
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        // Define a timespan of 5 minutes/ duration of minutes defined in jwt token
        TimeSpan offset = new TimeSpan(0, jwtOptions.Value.DurationInMinutes, 0);

        var claims = new[]
        {
            new Claim("UserId", id.ToString()),
            new Claim("username", username)
        };

        var token = new JwtSecurityToken
        (
            issuer: jwtOptions.Value.Issuer,
            audience: jwtOptions.Value.Audience,
            claims: claims,
            expires: DateTime.UtcNow.Add(offset),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

