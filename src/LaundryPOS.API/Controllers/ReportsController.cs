using LaundryPOS.Application.Features.Reports.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LaundryPOS.API.Controllers;

[Authorize(Policy = "SupervisorOrAbove")]
public class ReportsController : BaseApiController
{
    /// <summary>
    /// Get daily revenue report for a branch
    /// </summary>
    [HttpGet("revenue/daily/{branchId:guid}")]
    public async Task<IActionResult> GetDailyRevenue(Guid branchId, [FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        var result = await Mediator.Send(new GetDailyRevenueReportQuery(branchId, from, to));
        return HandleResult(result);
    }

    /// <summary>
    /// Get machine usage report for a branch
    /// </summary>
    [HttpGet("machines/usage/{branchId:guid}")]
    public async Task<IActionResult> GetMachineUsage(Guid branchId, [FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        var result = await Mediator.Send(new GetMachineUsageReportQuery(branchId, from, to));
        return HandleResult(result);
    }

    /// <summary>
    /// Get revenue report across all branches
    /// </summary>
    [HttpGet("revenue/branches")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetBranchRevenue([FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        var result = await Mediator.Send(new GetBranchRevenueReportQuery(from, to));
        return HandleResult(result);
    }

    /// <summary>
    /// Export report to PDF
    /// </summary>
    [HttpGet("export/pdf/{branchId:guid}")]
    public async Task<IActionResult> ExportPdf(
        Guid branchId,
        [FromQuery] string reportType,
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromServices] Domain.Interfaces.Services.IReportExportService exportService)
    {
        object data = reportType.ToLower() switch
        {
            "revenue" => (await Mediator.Send(new GetDailyRevenueReportQuery(branchId, from, to))).Value!,
            "usage" => (await Mediator.Send(new GetMachineUsageReportQuery(branchId, from, to))).Value!,
            _ => throw new ArgumentException($"Unknown report type: {reportType}")
        };

        var pdf = await exportService.ExportToPdfAsync(reportType, data);
        return File(pdf, "application/pdf", $"{reportType}-report-{from:yyyyMMdd}-{to:yyyyMMdd}.pdf");
    }

    /// <summary>
    /// Export report to Excel
    /// </summary>
    [HttpGet("export/excel/{branchId:guid}")]
    public async Task<IActionResult> ExportExcel(
        Guid branchId,
        [FromQuery] string reportType,
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromServices] Domain.Interfaces.Services.IReportExportService exportService)
    {
        object data = reportType.ToLower() switch
        {
            "revenue" => (await Mediator.Send(new GetDailyRevenueReportQuery(branchId, from, to))).Value!,
            "usage" => (await Mediator.Send(new GetMachineUsageReportQuery(branchId, from, to))).Value!,
            _ => throw new ArgumentException($"Unknown report type: {reportType}")
        };

        var excel = await exportService.ExportToExcelAsync(reportType, data);
        return File(excel, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{reportType}-report-{from:yyyyMMdd}-{to:yyyyMMdd}.xlsx");
    }

    /// <summary>
    /// Export report to CSV
    /// </summary>
    [HttpGet("export/csv/{branchId:guid}")]
    public async Task<IActionResult> ExportCsv(
        Guid branchId,
        [FromQuery] string reportType,
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromServices] Domain.Interfaces.Services.IReportExportService exportService)
    {
        object data = reportType.ToLower() switch
        {
            "revenue" => (await Mediator.Send(new GetDailyRevenueReportQuery(branchId, from, to))).Value!,
            "usage" => (await Mediator.Send(new GetMachineUsageReportQuery(branchId, from, to))).Value!,
            _ => throw new ArgumentException($"Unknown report type: {reportType}")
        };

        var csv = await exportService.ExportToCsvAsync(data);
        return File(csv, "text/csv", $"{reportType}-report-{from:yyyyMMdd}-{to:yyyyMMdd}.csv");
    }
}
