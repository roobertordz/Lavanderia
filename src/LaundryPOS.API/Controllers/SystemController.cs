using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LaundryPOS.API.Controllers;

/// <summary>
/// Exposes system/version metadata and the manual self-update flow.
/// The update actions are Administrator-only and rely on the Docker socket
/// being mounted into this container (see docker-compose.yml). This container
/// spawns a throwaway sibling container to actually run the update, so the
/// update survives this very container being rebuilt/recreated mid-flight.
/// </summary>
public class SystemController : BaseApiController
{
    private const string RepoPath = "/workspace";
    private const string UpdaterContainerName = "laundrypos-updater-run";
    private static readonly string VersionFilePath = Path.Combine(AppContext.BaseDirectory, "VERSION");
    private static readonly string LogFilePath = $"{RepoPath}/scripts/update.log";

    /// <summary>
    /// Returns the currently deployed application version. Public/anonymous.
    /// </summary>
    [HttpGet("version")]
    [AllowAnonymous]
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

    /// <summary>
    /// Checks the git remote for a newer tagged version than the one currently deployed.
    /// </summary>
    [HttpGet("check-update")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> CheckUpdate(CancellationToken ct)
    {
        try
        {
            await RunAsync("git", new[] { "-C", RepoPath, "fetch", "--tags", "--quiet" }, ct);

            var current = (await RunAsync("git", new[] { "-C", RepoPath, "describe", "--tags", "--abbrev=0" }, ct)).Trim();
            var tagsOutput = await RunAsync("git", new[] { "-C", RepoPath, "tag", "--sort=-v:refname" }, ct);
            var latest = tagsOutput
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .FirstOrDefault() ?? current;

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Data = new
                {
                    currentVersion = current,
                    latestVersion = latest,
                    updateAvailable = !string.Equals(current, latest, StringComparison.Ordinal)
                }
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse { Success = false, Error = $"No se pudo verificar actualizaciones: {ex.Message}" });
        }
    }

    /// <summary>
    /// Triggers the update to the given tag (or the latest tag if omitted).
    /// Spawns an isolated sibling container (via the mounted docker.sock) that
    /// runs scripts/update.sh, so the update process survives this container
    /// being torn down and recreated as part of its own update.
    /// </summary>
    [HttpPost("apply-update")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ApplyUpdate([FromQuery] string? tag, CancellationToken ct)
    {
        try
        {
            var alreadyRunning = (await RunAsync("docker",
                new[] { "ps", "--filter", $"name={UpdaterContainerName}", "--format", "{{.Names}}" }, ct)).Trim();

            if (!string.IsNullOrEmpty(alreadyRunning))
                return Conflict(new ApiResponse { Success = false, Error = "Ya hay una actualización en curso." });

            var innerCommand =
                "apk add --no-cache bash git curl >/dev/null 2>&1 && " +
                $"bash scripts/update.sh{(string.IsNullOrWhiteSpace(tag) ? "" : $" {tag}")} > scripts/update.log 2>&1";

            // IMPORTANT: the sibling container is created by the HOST docker
            // daemon (Docker-outside-of-Docker), so the bind mount must use the
            // real host path, not "/workspace" (which is only how *this*
            // container sees the repo). HOST_REPO_PATH is injected via
            // docker-compose.yml from the host's $PWD at startup.
            var hostRepoPath = Environment.GetEnvironmentVariable("HOST_REPO_PATH")
                ?? throw new InvalidOperationException("HOST_REPO_PATH no está configurada.");

            // The sibling container needs to reach this API's health endpoint
            // to verify the update. It gets its own network namespace, so
            // "localhost" inside it does NOT reach the host or this container.
            // Instead, join it to the same docker-compose network this
            // container is on, and use the "laundrypos-api" DNS alias (which
            // compose keeps pointing at whichever container is current, even
            // after the rebuild that happens mid-update).
            var networkName = (await RunAsync("docker",
                new[] { "inspect", "laundrypos-api", "--format", "{{range $k, $v := .NetworkSettings.Networks}}{{$k}}{{end}}" },
                ct)).Trim();

            await RunAsync("docker", new[]
            {
                "run", "-d", "--rm",
                "--name", UpdaterContainerName,
                "--network", networkName,
                "-e", "HEALTH_URL=http://laundrypos-api/api/system/version",
                "-v", "/var/run/docker.sock:/var/run/docker.sock",
                "-v", $"{hostRepoPath}:/workspace",
                "-w", "/workspace",
                "docker:27-cli",
                "sh", "-c", innerCommand
            }, ct);

            return Ok(new ApiResponse { Success = true });
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse { Success = false, Error = $"No se pudo iniciar la actualización: {ex.Message}" });
        }
    }

    /// <summary>
    /// Reports whether an update is currently running and the tail of its log.
    /// </summary>
    [HttpGet("update-status")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetUpdateStatus(CancellationToken ct)
    {
        var running = (await RunAsync("docker",
            new[] { "ps", "--filter", $"name={UpdaterContainerName}", "--format", "{{.Names}}" }, ct)).Trim();

        var log = System.IO.File.Exists(LogFilePath)
            ? await System.IO.File.ReadAllTextAsync(LogFilePath, ct)
            : "";
        var tail = string.Join('\n', log.Split('\n').TakeLast(80));

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Data = new { running = !string.IsNullOrEmpty(running), log = tail }
        });
    }

    private static async Task<string> RunAsync(string fileName, IEnumerable<string> args, CancellationToken ct)
    {
        var psi = new ProcessStartInfo(fileName)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        foreach (var arg in args) psi.ArgumentList.Add(arg);

        using var process = Process.Start(psi) ?? throw new InvalidOperationException($"No se pudo iniciar '{fileName}'.");
        var stdout = await process.StandardOutput.ReadToEndAsync(ct);
        var stderr = await process.StandardError.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);

        if (process.ExitCode != 0)
            throw new InvalidOperationException($"'{fileName} {string.Join(' ', args)}' falló ({process.ExitCode}): {stderr}");

        return stdout;
    }
}
