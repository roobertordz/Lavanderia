using FluentValidation;
using LaundryPOS.Application.Common;
using LaundryPOS.Application.Common.Interfaces;
using LaundryPOS.Application.Common.Models;
using LaundryPOS.Domain.Entities;
using LaundryPOS.Domain.Enums;
using LaundryPOS.Domain.Interfaces.Repositories;
using MediatR;

namespace LaundryPOS.Application.Features.Maintenance.Commands;

public record CreateMaintenanceCommand : ICommand<MaintenanceRecordDto>
{
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public MaintenanceType Type { get; init; }
    public DateTime ScheduledDate { get; init; }
    public Guid MachineId { get; init; }
    public Guid? TechnicianId { get; init; }
    public Guid BranchId { get; init; }
}

public class CreateMaintenanceValidator : AbstractValidator<CreateMaintenanceCommand>
{
    public CreateMaintenanceValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.MachineId).NotEmpty();
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.ScheduledDate).GreaterThan(DateTime.UtcNow.AddHours(-1));
    }
}

public class CreateMaintenanceHandler : IRequestHandler<CreateMaintenanceCommand, Result<MaintenanceRecordDto>>
{
    private readonly IUnitOfWork _uow;

    public CreateMaintenanceHandler(IUnitOfWork uow) { _uow = uow; }

    public async Task<Result<MaintenanceRecordDto>> Handle(CreateMaintenanceCommand request, CancellationToken ct)
    {
        var machine = await _uow.Machines.GetByIdAsync(request.MachineId, ct);
        if (machine == null)
            return Result.Failure<MaintenanceRecordDto>("Machine not found.", "NOT_FOUND");

        var record = new MaintenanceRecord
        {
            Title = request.Title,
            Description = request.Description,
            Type = request.Type,
            ScheduledDate = request.ScheduledDate,
            MachineId = request.MachineId,
            TechnicianId = request.TechnicianId,
            BranchId = request.BranchId,
            HoursWorkedAtService = machine.TotalHoursWorked,
            CyclesAtService = machine.TotalCycles
        };

        await _uow.Maintenance.AddAsync(record, ct);
        await _uow.SaveChangesAsync(ct);

        User? tech = null;
        if (request.TechnicianId.HasValue)
            tech = await _uow.Users.GetByIdAsync(request.TechnicianId.Value, ct);

        return Result.Success(new MaintenanceRecordDto
        {
            Id = record.Id,
            Title = record.Title,
            Description = record.Description,
            Type = record.Type,
            Status = record.Status,
            ScheduledDate = record.ScheduledDate,
            Cost = record.Cost,
            MachineId = record.MachineId,
            MachineName = machine.Name,
            TechnicianId = record.TechnicianId,
            TechnicianName = tech?.FullName
        });
    }
}

public record CompleteMaintenanceCommand : ICommand
{
    public Guid MaintenanceId { get; init; }
    public decimal? Cost { get; init; }
    public string? PartsReplaced { get; init; }
    public string? TechnicianNotes { get; init; }
}

public class CompleteMaintenanceHandler : IRequestHandler<CompleteMaintenanceCommand, Result>
{
    private readonly IUnitOfWork _uow;

    public CompleteMaintenanceHandler(IUnitOfWork uow) { _uow = uow; }

    public async Task<Result> Handle(CompleteMaintenanceCommand request, CancellationToken ct)
    {
        var record = await _uow.Maintenance.GetByIdAsync(request.MaintenanceId, ct);
        if (record == null)
            return Result.Failure("Maintenance record not found.", "NOT_FOUND");

        record.Status = MaintenanceStatus.Completed;
        record.CompletedDate = DateTime.UtcNow;
        record.Cost = request.Cost;
        record.PartsReplaced = request.PartsReplaced;
        record.TechnicianNotes = request.TechnicianNotes;

        // Restore machine to available
        var machine = await _uow.Machines.GetByIdAsync(record.MachineId, ct);
        if (machine != null && machine.Status == MachineStatus.Maintenance)
        {
            machine.Status = MachineStatus.Available;
            machine.LastMaintenanceDate = DateTime.UtcNow;
            await _uow.Machines.UpdateAsync(machine, ct);
        }

        await _uow.Maintenance.UpdateAsync(record, ct);
        await _uow.SaveChangesAsync(ct);

        return Result.Success();
    }
}
