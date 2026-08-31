using FluentValidation;
using LaundryPOS.Application.Common;
using LaundryPOS.Application.Common.Interfaces;
using LaundryPOS.Application.Common.Models;
using LaundryPOS.Domain.Entities;
using LaundryPOS.Domain.Enums;
using LaundryPOS.Domain.Interfaces.Repositories;
using MediatR;

namespace LaundryPOS.Application.Features.Branches.Commands;

public record CreateBranchCommand : ICommand<BranchDto>
{
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string ZipCode { get; init; } = string.Empty;
    public string Country { get; init; } = "México";
    public string Phone { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? TimeZone { get; init; }
    public string? OpeningTime { get; init; }
    public string? ClosingTime { get; init; }
    public decimal TaxRate { get; init; } = 16;
    public string Currency { get; init; } = "MXN";
    public int GracePeriodMinutes { get; init; } = 5;
}

public class CreateBranchValidator : AbstractValidator<CreateBranchCommand>
{
    public CreateBranchValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Address).NotEmpty().MaximumLength(500);
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.State).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.TaxRate).InclusiveBetween(0, 100);
    }
}

public class CreateBranchHandler : IRequestHandler<CreateBranchCommand, Result<BranchDto>>
{
    private readonly IUnitOfWork _uow;

    public CreateBranchHandler(IUnitOfWork uow) { _uow = uow; }

    public async Task<Result<BranchDto>> Handle(CreateBranchCommand request, CancellationToken ct)
    {
        var existing = await _uow.Branches.GetByCodeAsync(request.Code, ct);
        if (existing != null)
            return Result.Failure<BranchDto>("Branch code already exists.", "DUPLICATE_CODE");

        var branch = new Branch
        {
            Name = request.Name,
            Code = request.Code,
            Address = request.Address,
            City = request.City,
            State = request.State,
            ZipCode = request.ZipCode,
            Country = request.Country,
            Phone = request.Phone,
            Email = request.Email,
            TimeZone = request.TimeZone,
            OpeningTime = request.OpeningTime,
            ClosingTime = request.ClosingTime,
            TaxRate = request.TaxRate,
            Currency = request.Currency,
            GracePeriodMinutes = request.GracePeriodMinutes
        };

        await _uow.Branches.AddAsync(branch, ct);
        await _uow.SaveChangesAsync(ct);

        return Result.Success(new BranchDto
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
            TotalMachines = 0,
            AvailableMachines = 0,
            IsActive = branch.IsActive
        });
    }
}

public record UpdateBranchCommand : ICommand<BranchDto>
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? OpeningTime { get; init; }
    public string? ClosingTime { get; init; }
    public decimal TaxRate { get; init; }
    public int GracePeriodMinutes { get; init; }
}

public class UpdateBranchHandler : IRequestHandler<UpdateBranchCommand, Result<BranchDto>>
{
    private readonly IUnitOfWork _uow;

    public UpdateBranchHandler(IUnitOfWork uow) { _uow = uow; }

    public async Task<Result<BranchDto>> Handle(UpdateBranchCommand request, CancellationToken ct)
    {
        var branch = await _uow.Branches.GetByIdAsync(request.Id, ct);
        if (branch == null)
            return Result.Failure<BranchDto>("Branch not found.", "NOT_FOUND");

        branch.Name = request.Name;
        branch.Address = request.Address;
        branch.City = request.City;
        branch.State = request.State;
        branch.Phone = request.Phone;
        branch.Email = request.Email;
        branch.OpeningTime = request.OpeningTime;
        branch.ClosingTime = request.ClosingTime;
        branch.TaxRate = request.TaxRate;
        branch.GracePeriodMinutes = request.GracePeriodMinutes;
        branch.UpdatedAt = DateTime.UtcNow;

        await _uow.Branches.UpdateAsync(branch, ct);
        await _uow.SaveChangesAsync(ct);

        var machines = await _uow.Machines.GetByBranchAsync(branch.Id, ct);

        return Result.Success(new BranchDto
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
            TotalMachines = machines.Count,
            AvailableMachines = machines.Count(m => m.Status == MachineStatus.Available),
            IsActive = branch.IsActive
        });
    }
}
