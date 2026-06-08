using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace WorkoutLogger.EndpointFilter;


// This generic endpoint filter validates incoming models using Data Annotations.
// TModel: The type of the model to validate (e.g., Employee, Product, etc.)
public class ValidationEndPointFilter<TModel> : IEndpointFilter where TModel : class
{
    // This method is called for each request to an endpoint that uses this filter
    // context: Provides access to arguments, HttpContext, etc.
    // next: Delegate to call the next filter or the endpoint handler itself.
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        // Extract the first argument from the endpoint's parameters that matches type TModel
        // This assumes the model to validate is passed as a parameter to the endpoint
        // This is typically the deserialized request body model (e.g., Employee, Product, etc).
        var model = context.Arguments.OfType<TModel>().FirstOrDefault();

        // If no model of the expected type is found in the arguments,
        // immediately return a 400 Bad Request indicating the payload is missing or invalid.
        if (model == null)
            return Results.BadRequest($"Missing payload of type {typeof(TModel).Name}");

        // Prepare a list to store any validation errors found during model validation.
        var validationResults = new List<ValidationResult>();

        // Create a validation context describing the model to validate
        // This provides metadata and contextual information needed for validation
        var validationContext = new ValidationContext(model);

        // Use the Validator to check all properties on the model against their Data Annotations.
        // The 'true' parameter means validate all properties (not just top-level).
        if (!Validator.TryValidateObject(model, validationContext, validationResults, true))
        {
            // If validation fails, extract all error messages from validationResults
            var errors = validationResults.Select(r => r.ErrorMessage).ToArray();

            // Return HTTP 400 Bad Request with a detailed response object
            // containing a message and list of validation error messages
            return Results.BadRequest(new { Message = "Validation failed", Errors = errors });
        }

        // If validation succeeds, invoke the next delegate in the filter pipeline
        // This calls the actual endpoint handler or next filter
        return await next(context);
    }
}
