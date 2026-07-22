using Microsoft.AspNetCore.Mvc.Filters;

namespace Template.Api.Filters;

public class GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger) : IExceptionFilter
{
    private readonly ILogger<GlobalExceptionFilter> _logger = logger;

    public void OnException(ExceptionContext context)
    {
        // Log the exception
        _logger.LogError(context.Exception, 
            "Exception Logged In Global Exception Filter: {Message}", 
            context.Exception.Message);
    }
}