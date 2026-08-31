using LaundryPOS.Application.Common;
using LaundryPOS.Application.Common.Interfaces;
using LaundryPOS.Application.Common.Models;
using LaundryPOS.Domain.Interfaces.Repositories;
using MediatR;

namespace LaundryPOS.Application.Features.Machines.Queries;

// ─── Get Machines by Branch ───
public record GetMachinesByBranchQuery(Guid BranchId) : IQuery<IReadOnlyList<MachineDto>>;

public class GetMachinesByBranchHandler : IRequestHandler<GetMachinesByBranchQuery, Result<IReadOnlyList<MachineDto>>>
{
    private readonly IUnitOfWork _uow;

    public GetMachinesByBranchHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Result<IReadOnlyList<MachineDto>>> Handle(GetMachinesByBranchQuery request, CancellationToken ct)
    {
        var machines = await _uow.Machines.GetByBranchAsync(request.BranchId, ct);
        var branch = await _uow.Branches.GetByIdAsync(request.BranchId, ct);

        var dtos = machines.Select(m => new MachineDto
        {
            Id = m.Id,
            Number = m.Number,
            Name = m.Name,
            Type = m.Type,
            Capacity = m.Capacity,
            Price = m.Price,
            DurationMinutes = m.DurationMinutes,
            Status = m.Status,
            Location = m.Location,
            IpAddress = m.IpAddress,
            Model = m.Model,
            Brand = m.Brand,
            SerialNumber = m.SerialNumber,
            CommunicationStatus = m.CommunicationStatus,
            LastHeartbeat = m.LastHeartbeat,
            TotalCycles = m.TotalCycles,
            TotalHoursWorked = m.TotalHoursWorked,
            BranchId = m.BranchId,
            BranchName = branch?.Name ?? string.Empty,
            IoTControllerId = m.IoTControllerId
        }).ToList().AsReadOnly();

        return Result.Success<IReadOnlyList<MachineDto>>(dtos);
    }
}

// ─── Get Machine by ID ───
public record GetMachineByIdQuery(Guid MachineId) : IQuery<MachineDto>;

public class GetMachineByIdHandler : IRequestHandler<GetMachineByIdQuery, Result<MachineDto>>
{
    private readonly IUnitOfWork _uow;

    public GetMachineByIdHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Result<MachineDto>> Handle(GetMachineByIdQuery request, CancellationToken ct)
    {
        var machine = await _uow.Machines.GetByIdAsync(request.MachineId, ct);
        if (machine == null)
            return Result.Failure<MachineDto>("Machine not found.", "NOT_FOUND");

        var branch = await _uow.Branches.GetByIdAsync(machine.BranchId, ct);

        return Result.Success(new MachineDto
        {
            Id = machine.Id,
            Number = machine.Number,
            Name = machine.Name,
            Type = machine.Type,
            Capacity = machine.Capacity,
            Price = machine.Price,
            DurationMinutes = machine.DurationMinutes,
            Status = machine.Status,
            Location = machine.Location,
            IpAddress = machine.IpAddress,
            Model = machine.Model,
            Brand = machine.Brand,
            SerialNumber = machine.SerialNumber,
            CommunicationStatus = machine.CommunicationStatus,
            LastHeartbeat = machine.LastHeartbeat,
            TotalCycles = machine.TotalCycles,
            TotalHoursWorked = machine.TotalHoursWorked,
            BranchId = machine.BranchId,
            BranchName = branch?.Name ?? string.Empty,
            IoTControllerId = machine.IoTControllerId
        });
    }
}

// ─── Get Available Machines ───
public record GetAvailableMachinesQuery(Guid BranchId) : IQuery<IReadOnlyList<MachineDto>>;

public class GetAvailableMachinesHandler : IRequestHandler<GetAvailableMachinesQuery, Result<IReadOnlyList<MachineDto>>>
{
    private readonly IUnitOfWork _uow;

    public GetAvailableMachinesHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Result<IReadOnlyList<MachineDto>>> Handle(GetAvailableMachinesQuery request, CancellationToken ct)
    {
        var machines = await _uow.Machines.GetAvailableByBranchAsync(request.BranchId, ct);
        var branch = await _uow.Branches.GetByIdAsync(request.BranchId, ct);

        var dtos = machines.Select(m => new MachineDto
        {
            Id = m.Id,
            Number = m.Number,
            Name = m.Name,
            Type = m.Type,
            Capacity = m.Capacity,
            Price = m.Price,
            DurationMinutes = m.DurationMinutes,
            Status = m.Status,
            Location = m.Location,
            BranchId = m.BranchId,
            BranchName = branch?.Name ?? string.Empty,
            CommunicationStatus = m.CommunicationStatus
        }).ToList().AsReadOnly();

        return Result.Success<IReadOnlyList<MachineDto>>(dtos);
    }
}
