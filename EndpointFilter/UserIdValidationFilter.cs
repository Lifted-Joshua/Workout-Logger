using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WorkoutLogger.EndpointFilter;

public class UserIdValidationFilter : IEndpointFilter
{
    public async ValueTask<object> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        // Try to read "userId" from route values which will include the jwt token
        // If the userId is null or cannot be converted / parsed into an int we return a badRequest object
        if (!context.HttpContext.Request.RouteValues.TryGetValue("userId", out var idObj) ||
            idObj is null ||
            !int.TryParse(idObj.ToString(), out var userId))
        {
            return Results.BadRequest(new { error = "Invalid or missing userId in route." });
        }

        // Additional checks (example)
        if (userId <= 0)
        {
            return Results.BadRequest(new { error = "userId must be a positive integer." });
        }

        // Add value to HttpContext so endpoints can use it
        context.HttpContext.Items["UserId"] = userId;

        return await next(context);
    }
}
