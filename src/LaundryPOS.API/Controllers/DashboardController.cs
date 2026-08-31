using LaundryPOS.Application.Features.Dashboard.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LaundryPOS.API.Controllers;

[Authorize]
public class DashboardController : BaseApiController
{
    /// <summary>
    /// Get real-time dashboard data for a branch
    /// </summary>
    [HttpGet("{branchId:guid}")]
    public async Task<IActionResult> GetDashboard(Guid branchId)
    {
        var result = await Mediator.Send(new GetDashboardQuery(branchId));
        return HandleResult(result);
    }
}
