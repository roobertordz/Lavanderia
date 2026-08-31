using FluentValidation;
using LaundryPOS.Application.Common;
using LaundryPOS.Application.Common.Interfaces;
using LaundryPOS.Application.Common.Models;
using LaundryPOS.Domain.Entities;
using LaundryPOS.Domain.Enums;
using LaundryPOS.Domain.Interfaces.Repositories;
using MediatR;

namespace LaundryPOS.Application.Features.Machines.Commands;

// ─── Create Machine ───
public record CreateMachineCommand : ICommand<MachineDto>
{
    public int Number { get; init; }
    public string Name { get; init; } = string.Empty;
    public MachineType Type { get; init; }
    public string Capacity { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public int DurationMinutes { get; init; }
    public string Location { get; init; } = string.Empty;
    public string? IpAddress { get; init; }
    public string? Model { get; init; }
    public string? Brand { get; init; }
    public string? SerialNumber { get; init; }
    public Guid BranchId { get; init; }
    public Guid? IoTControllerId { get; init; }
}

public class CreateMachineValidator : AbstractValidator<CreateMachineCommand>
{
    public CreateMachineValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Number).GreaterThan(0);
        RuleFor(x => x.Capacity).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Price).GreaterThan(0);
        RuleFor(x => x.DurationMinutes).GreaterThan(0).LessThanOrEqualTo(240);
        RuleFor(x => x.BranchId).NotEmpty();
    }
}

public class CreateMachineHandler : IRequestHandler<CreateMachineCommand, Result<MachineDto>>
{
    private readonly IUnitOfWork _uow;

    public CreateMachineHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Result<MachineDto>> Handle(CreateMachineCommand request, CancellationToken ct)
    {
        var exists = await _uow.Machines.GetByNumberAndBranchAsync(request.Number, request.BranchId, ct);
        if (exists != null)
            return Result.Failure<MachineDto>("A machine with this number already exists in this branch.", "DUPLICATE_NUMBER");

        var machine = new Machine
        {
            Number = request.Number,
            Name = request.Name,
            Type = request.Type,
            Capacity = request.Capacity,
            Price = request.Price,
            DurationMinutes = request.DurationMinutes,
            Location = request.Location,
            IpAddress = request.IpAddress,
            Model = request.Model,
            Brand = request.Brand,
            SerialNumber = request.SerialNumber,
            BranchId = request.BranchId,
            IoTControllerId = request.IoTControllerId,
            Status = MachineStatus.Available
        };

        await _uow.Machines.AddAsync(machine, ct);
        await _uow.SaveChangesAsync(ct);

        var branch = await _uow.Branches.GetByIdAsync(request.BranchId, ct);

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
            BranchId = machine.BranchId,
            BranchName = branch?.Name ?? string.Empty,
            IoTControllerId = machine.IoTControllerId,
            CommunicationStatus = machine.CommunicationStatus,
            TotalCycles = machine.TotalCycles,
            TotalHoursWorked = machine.TotalHoursWorked
        });
    }
}

// ─── Update Machine ───
public record UpdateMachineCommand : ICommand<MachineDto>
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Capacity { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public int DurationMinutes { get; init; }
    public string Location { get; init; } = string.Empty;
    public string? IpAddress { get; init; }
    public string? Model { get; init; }
    public string? Brand { get; init; }
    public Guid? IoTControllerId { get; init; }
}

public class UpdateMachineHandler : IRequestHandler<UpdateMachineCommand, Result<MachineDto>>
{
    private readonly IUnitOfWork _uow;

    public UpdateMachineHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Result<MachineDto>> Handle(UpdateMachineCommand request, CancellationToken ct)
    {
        var machine = await _uow.Machines.GetByIdAsync(request.Id, ct);
        if (machine == null)
            return Result.Failure<MachineDto>("Machine not found.", "NOT_FOUND");

        machine.Name = request.Name;
        machine.Capacity = request.Capacity;
        machine.Price = request.Price;
        machine.DurationMinutes = request.DurationMinutes;
        machine.Location = request.Location;
        machine.IpAddress = request.IpAddress;
        machine.Model = request.Model;
        machine.Brand = request.Brand;
        machine.IoTControllerId = request.IoTControllerId;
        machine.UpdatedAt = DateTime.UtcNow;

        await _uow.Machines.UpdateAsync(machine, ct);
        await _uow.SaveChangesAsync(ct);

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
            BranchId = machine.BranchId,
            BranchName = branch?.Name ?? string.Empty,
            IoTControllerId = machine.IoTControllerId,
            CommunicationStatus = machine.CommunicationStatus,
            TotalCycles = machine.TotalCycles,
            TotalHoursWorked = machine.TotalHoursWorked
        });
    }
}

// ─── Change Machine Status ───
public record ChangeMachineStatusCommand : ICommand
{
    public Guid MachineId { get; init; }
    public MachineStatus NewStatus { get; init; }
}

public class ChangeMachineStatusHandler : IRequestHandler<ChangeMachineStatusCommand, Result>
{
    private readonly IUnitOfWork _uow;

    public ChangeMachineStatusHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Result> Handle(ChangeMachineStatusCommand request, CancellationToken ct)
    {
        var machine = await _uow.Machines.GetByIdAsync(request.MachineId, ct);
        if (machine == null)
            return Result.Failure("Machine not found.", "NOT_FOUND");

        machine.Status = request.NewStatus;
        machine.UpdatedAt = DateTime.UtcNow;

        await _uow.Machines.UpdateAsync(machine, ct);
        await _uow.SaveChangesAsync(ct);

        return Result.Success();
    }
}

// ─── Delete Machine (Soft) ───
public record DeleteMachineCommand(Guid MachineId) : ICommand;

public class DeleteMachineHandler : IRequestHandler<DeleteMachineCommand, Result>
{
    private readonly IUnitOfWork _uow;

    public DeleteMachineHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Result> Handle(DeleteMachineCommand request, CancellationToken ct)
    {
        var machine = await _uow.Machines.GetByIdAsync(request.MachineId, ct);
        if (machine == null)
            return Result.Failure("Machine not found.", "NOT_FOUND");

        machine.IsDeleted = true;
        machine.IsActive = false;
        machine.DeletedAt = DateTime.UtcNow;
        machine.Status = MachineStatus.OutOfService;

        await _uow.Machines.UpdateAsync(machine, ct);
        await _uow.SaveChangesAsync(ct);

        return Result.Success();
    }
}
