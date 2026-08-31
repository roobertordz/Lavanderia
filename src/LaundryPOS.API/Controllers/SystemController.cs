using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LaundryPOS.API.Controllers;

/// <summary>
/// Exposes basic system/version metadata. Used by the update system and the
/// admin UI to know which version is currently running.
/// </summary>
[AllowAnonymous]
public class SystemController : BaseApiController
{
    private static readonly string VersionFilePath = Path.Combine(AppContext.BaseDirectory, "VERSION");

    /// <summary>
    /// Returns the currently deployed application version.
    /// </summary>
    [HttpGet("version")]
    public IActionResult GetVersion()
    {
        var version = System.IO.File.Exists(VersionFilePath)
            ? System.IO.File.ReadAllText(VersionFilePath).Trim()
            : "unknown";

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Data = new
            {
                version,
                environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "unknown"
            }
        });
    }
}
