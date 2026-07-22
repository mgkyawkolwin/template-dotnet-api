using System.Diagnostics;
using System.Net;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Template.Api.Exceptions;
using Template.Api.I18N;

namespace Template.Api.Controllers;

[ApiController]
[Route("api")]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IConfiguration _configuration;
    private readonly IStringLocalizer<LocalizedStrings> _localizer;

    public HomeController(ILogger<HomeController> logger, IConfiguration configuration, IStringLocalizer<LocalizedStrings> localizer)
    {
        _logger = logger;
        _configuration = configuration;
        _localizer = localizer;
    }

    [HttpGet]
    public IActionResult Index()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var version = FileVersionInfo.GetVersionInfo(assembly.Location).FileVersion ?? "Unknown";
            var environment = _configuration["ASPNETCORE_ENVIRONMENT"] ?? "Unknown";
            var message = _localizer["Text.ApiIsRunning", version, environment].Value;
            _logger.LogInformation(message);
            return Ok(new { Success = true, Message = message });
        }
        catch (CustomException ex)
        {
            _logger.LogError(ex, "Custom Exception: {message}", ex.Message);
            return StatusCode((int)HttpStatusCode.BadRequest, new { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred.");
            return StatusCode((int)HttpStatusCode.InternalServerError, new { Success = false, Message = "Unexpected error occurred." });
        }
    }
}
