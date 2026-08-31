using LaundryPOS.Application.Features.Branches.Commands;
using LaundryPOS.Domain.Interfaces.Repositories;
using LaundryPOS.Application.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LaundryPOS.API.Controllers;

[Authorize]
public class BranchesController : BaseApiController
{
    /// <summary>
    /// Get all branches
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromServices] IUnitOfWork uow)
    {
        var branches = await uow.Branches.GetAllAsync();
        var dtos = branches.Where(b => b.IsActive).Select(b => new BranchDto
        {
            Id = b.Id,
            Name = b.Name,
            Code = b.Code,
            Address = b.Address,
            City = b.City,
            State = b.State,
            Phone = b.Phone,
            Currency = b.Currency,
            TaxRate = b.TaxRate,
            IsActive = b.IsActive
        }).ToList();

        return Ok(new ApiResponse<List<BranchDto>> { Success = true, Data = dtos });
    }

    /// <summary>
    /// Get a specific branch with its machines
    /// </summary>
    [HttpGet("{branchId:guid}")]
    public async Task<IActionResult> GetById(Guid branchId, [FromServices] IUnitOfWork uow)
    {
        var branch = await uow.Branches.GetWithMachinesAsync(branchId);
        if (branch == null)
            return NotFound(new ApiResponse { Success = false, Error = "Branch not found" });

        return Ok(new ApiResponse<BranchDto>
        {
            Success = true,
            Data = new BranchDto
            {
                Id = branch.Id,
                Name = branch.Name,
                Code = branch.Code,
                Address = branch.Address,
                City = branch.City,
                State = branch.State,
                Phone = branch.Phone,
                Currency = branch.Currency,
                TaxRate = branch.TaxRate,
                TotalMachines = branch.Machines.Count,
                AvailableMachines = branch.Machines.Count(m => m.Status == Domain.Enums.MachineStatus.Available),
                IsActive = branch.IsActive
            }
        });
    }

    /// <summary>
    /// Create a new branch
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Create([FromBody] CreateBranchCommand command)
    {
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Update an existing branch
    /// </summary>
    [HttpPut("{branchId:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Update(Guid branchId, [FromBody] UpdateBranchCommand command)
    {
        if (branchId != command.Id)
            return BadRequest(new ApiResponse { Success = false, Error = "Branch ID mismatch" });

        var result = await Mediator.Send(command);
        return HandleResult(result);
    }
}
