using LaundryPOS.Application.Features.Maintenance.Commands;
using LaundryPOS.Application.Common.Models;
using LaundryPOS.Domain.Enums;
using LaundryPOS.Domain.Interfaces.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LaundryPOS.API.Controllers;

[Authorize(Policy = "TechnicianAccess")]
public class MaintenanceController : BaseApiController
{
    /// <summary>
    /// Get maintenance records for a branch
    /// </summary>
    [HttpGet("branch/{branchId:guid}")]
    public async Task<IActionResult> GetByBranch(Guid branchId, [FromQuery] DateTime from, [FromQuery] DateTime to, [FromServices] IUnitOfWork uow)
    {
        var records = await uow.Maintenance.GetByBranchAsync(branchId, from, to);
        var machines = await uow.Machines.GetByBranchAsync(branchId);

        var dtos = records.Select(r => new MaintenanceRecordDto
        {
            Id = r.Id,
            Title = r.Title,
            Description = r.Description,
            Type = r.Type,
            Status = r.Status,
            ScheduledDate = r.ScheduledDate,
            CompletedDate = r.CompletedDate,
            Cost = r.Cost,
            PartsReplaced = r.PartsReplaced,
            MachineId = r.MachineId,
            MachineName = machines.FirstOrDefault(m => m.Id == r.MachineId)?.Name ?? "Unknown"
        }).ToList();

        return Ok(new ApiResponse<List<MaintenanceRecordDto>> { Success = true, Data = dtos });
    }

    /// <summary>
    /// Get scheduled maintenance
    /// </summary>
    [HttpGet("branch/{branchId:guid}/scheduled")]
    public async Task<IActionResult> GetScheduled(Guid branchId, [FromServices] IUnitOfWork uow)
    {
        var records = await uow.Maintenance.GetScheduledAsync(branchId);
        var machines = await uow.Machines.GetByBranchAsync(branchId);

        var dtos = records.Select(r => new MaintenanceRecordDto
        {
            Id = r.Id,
            Title = r.Title,
            Description = r.Description,
            Type = r.Type,
            Status = r.Status,
            ScheduledDate = r.ScheduledDate,
            MachineId = r.MachineId,
            MachineName = machines.FirstOrDefault(m => m.Id == r.MachineId)?.Name ?? "Unknown"
        }).ToList();

        return Ok(new ApiResponse<List<MaintenanceRecordDto>> { Success = true, Data = dtos });
    }

    /// <summary>
    /// Create a maintenance record
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateMaintenanceCommand command)
    {
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Complete a maintenance record
    /// </summary>
    [HttpPatch("{maintenanceId:guid}/complete")]
    public async Task<IActionResult> Complete(Guid maintenanceId, [FromBody] CompleteMaintenanceCommand command)
    {
        if (maintenanceId != command.MaintenanceId)
            return BadRequest(new ApiResponse { Success = false, Error = "Maintenance ID mismatch" });

        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Get maintenance history for a machine
    /// </summary>
    [HttpGet("machine/{machineId:guid}")]
    public async Task<IActionResult> GetByMachine(Guid machineId, [FromServices] IUnitOfWork uow)
    {
        var records = await uow.Maintenance.GetByMachineAsync(machineId);
        var machine = await uow.Machines.GetByIdAsync(machineId);

        var dtos = records.Select(r => new MaintenanceRecordDto
        {
            Id = r.Id,
            Title = r.Title,
            Description = r.Description,
            Type = r.Type,
            Status = r.Status,
            ScheduledDate = r.ScheduledDate,
            CompletedDate = r.CompletedDate,
            Cost = r.Cost,
            PartsReplaced = r.PartsReplaced,
            MachineId = r.MachineId,
            MachineName = machine?.Name ?? "Unknown"
        }).ToList();

        return Ok(new ApiResponse<List<MaintenanceRecordDto>> { Success = true, Data = dtos });
    }
}
