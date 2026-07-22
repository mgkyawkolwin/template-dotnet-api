using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Template.Api.Controllers;

public class BaseController : Controller
{
    private readonly ILogger<BaseController> _logger;

    public BaseController(ILogger<BaseController> logger)
    {
        _logger = logger;
    }

    protected Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        // Log the action being executed
        _logger.LogInformation("Executing action: {Action} on {Controller}", 
            context.ActionDescriptor.DisplayName,
            context.Controller.GetType().Name);

        // Check for model binding errors
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => new { 
                    ErrorMessage = e.ErrorMessage, 
                    Exception = e.Exception?.Message 
                })
                .ToList();

            _logger.LogWarning("Model binding errors: {Errors}", 
                System.Text.Json.JsonSerializer.Serialize(errors));
        }

        base.OnActionExecuting(context);
    }

    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Log the action being executed
        _logger.LogInformation("Executing action: {Action} on {Controller}", 
            context.ActionDescriptor.DisplayName,
            context.Controller.GetType().Name);

        // Check for model binding errors
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => new { 
                    ErrorMessage = e.ErrorMessage, 
                    Exception = e.Exception?.Message 
                })
                .ToList();

            _logger.LogWarning("Model binding errors: {Errors}", 
                System.Text.Json.JsonSerializer.Serialize(errors));
        }

        await base.OnActionExecutionAsync(context, next);
    }

    public override void OnActionExecuted(ActionExecutedContext context)
    {
        // Log after action execution
        if (context.Exception != null)
        {
            _logger.LogError(context.Exception, "Action failed");
        }
        else
        {
            _logger.LogInformation("Action completed successfully");
        }

        base.OnActionExecuted(context);
    }
}