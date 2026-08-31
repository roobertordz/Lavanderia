using LaundryPOS.Application.Features.Machines.Commands;
using LaundryPOS.Application.Features.Machines.Queries;
using LaundryPOS.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LaundryPOS.API.Controllers;

[Authorize]
public class MachinesController : BaseApiController
{
    /// <summary>
    /// Get all machines for a branch
    /// </summary>
    [HttpGet("branch/{branchId:guid}")]
    public async Task<IActionResult> GetByBranch(Guid branchId)
    {
        var result = await Mediator.Send(new GetMachinesByBranchQuery(branchId));
        return HandleResult(result);
    }

    /// <summary>
    /// Get available machines for a branch (kiosk endpoint)
    /// </summary>
    [HttpGet("branch/{branchId:guid}/available")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAvailable(Guid branchId)
    {
        var result = await Mediator.Send(new GetAvailableMachinesQuery(branchId));
        return HandleResult(result);
    }

    /// <summary>
    /// Get a specific machine
    /// </summary>
    [HttpGet("{machineId:guid}")]
    public async Task<IActionResult> GetById(Guid machineId)
    {
        var result = await Mediator.Send(new GetMachineByIdQuery(machineId));
        return HandleResult(result);
    }

    /// <summary>
    /// Create a new machine
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Create([FromBody] CreateMachineCommand command)
    {
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Update an existing machine
    /// </summary>
    [HttpPut("{machineId:guid}")]
    [Authorize(Policy = "SupervisorOrAbove")]
    public async Task<IActionResult> Update(Guid machineId, [FromBody] UpdateMachineCommand command)
    {
        if (machineId != command.Id)
            return BadRequest(new ApiResponse { Success = false, Error = "Machine ID mismatch" });

        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Change machine status
    /// </summary>
    [HttpPatch("{machineId:guid}/status")]
    [Authorize(Policy = "EmployeeOrAbove")]
    public async Task<IActionResult> ChangeStatus(Guid machineId, [FromBody] MachineStatus newStatus)
    {
        var result = await Mediator.Send(new ChangeMachineStatusCommand { MachineId = machineId, NewStatus = newStatus });
        return HandleResult(result);
    }

    /// <summary>
    /// Soft-delete a machine
    /// </summary>
    [HttpDelete("{machineId:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(Guid machineId)
    {
        var result = await Mediator.Send(new DeleteMachineCommand(machineId));
        return HandleResult(result);
    }
}
