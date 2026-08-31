using FluentValidation;
using LaundryPOS.Application.Common;
using LaundryPOS.Application.Common.Interfaces;
using LaundryPOS.Application.Common.Models;
using LaundryPOS.Domain.Entities;
using LaundryPOS.Domain.Enums;
using LaundryPOS.Domain.Interfaces.Repositories;
using MediatR;

namespace LaundryPOS.Application.Features.LaundryOrders.Commands;

internal static class LaundryOrderMapper
{
    public static LaundryOrderDto ToDto(LaundryOrder o, string branchName, string? processedByUserName) => new()
    {
        Id = o.Id,
        OrderNumber = o.OrderNumber,
        ServiceType = o.ServiceType,
        Status = o.Status,
        CustomerName = o.CustomerName,
        CustomerPhone = o.CustomerPhone,
        WeightKg = o.WeightKg,
        PricePerKg = o.PricePerKg,
        ComforterCount = o.ComforterCount,
        ComforterSize = o.ComforterSize,
        PricePerComforter = o.PricePerComforter,
        TotalPrice = o.TotalPrice,
        PaymentMethod = o.PaymentMethod,
        PaymentStatus = o.PaymentStatus,
        ReceivedAt = o.ReceivedAt,
        EstimatedReadyAt = o.EstimatedReadyAt,
        DeliveredAt = o.DeliveredAt,
        Notes = o.Notes,
        BranchId = o.BranchId,
        BranchName = branchName,
        ProcessedByUserName = processedByUserName
    };
}

// ─── Create Laundry Order ───
public record CreateLaundryOrderCommand : ICommand<LaundryOrderDto>
{
    public LaundryOrderServiceType ServiceType { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public string? CustomerPhone { get; init; }

    // ByWeight
    public decimal? WeightKg { get; init; }
    public decimal? PricePerKg { get; init; }

    // Comforter
    public int? ComforterCount { get; init; }
    public string? ComforterSize { get; init; }
    public decimal? PricePerComforter { get; init; }

    public DateTime? EstimatedReadyAt { get; init; }
    public string? Notes { get; init; }
    public Guid BranchId { get; init; }
    public Guid? UserId { get; init; }
}

public class CreateLaundryOrderValidator : AbstractValidator<CreateLaundryOrderCommand>
{
    public CreateLaundryOrderValidator()
    {
        RuleFor(x => x.CustomerName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CustomerPhone).MaximumLength(30);
        RuleFor(x => x.BranchId).NotEmpty();

        When(x => x.ServiceType == LaundryOrderServiceType.ByWeight, () =>
        {
            RuleFor(x => x.WeightKg).NotNull().GreaterThan(0);
            RuleFor(x => x.PricePerKg).NotNull().GreaterThan(0);
        });

        When(x => x.ServiceType == LaundryOrderServiceType.Comforter, () =>
        {
            RuleFor(x => x.ComforterCount).NotNull().GreaterThan(0);
            RuleFor(x => x.PricePerComforter).NotNull().GreaterThan(0);
        });
    }
}

public class CreateLaundryOrderHandler : IRequestHandler<CreateLaundryOrderCommand, Result<LaundryOrderDto>>
{
    private readonly IUnitOfWork _uow;

    public CreateLaundryOrderHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Result<LaundryOrderDto>> Handle(CreateLaundryOrderCommand request, CancellationToken ct)
    {
        var totalPrice = request.ServiceType == LaundryOrderServiceType.ByWeight
            ? (request.WeightKg ?? 0) * (request.PricePerKg ?? 0)
            : (request.ComforterCount ?? 0) * (request.PricePerComforter ?? 0);

        var order = new LaundryOrder
        {
            OrderNumber = await _uow.LaundryOrders.GenerateOrderNumberAsync(request.BranchId, ct),
            ServiceType = request.ServiceType,
            Status = LaundryOrderStatus.Received,
            CustomerName = request.CustomerName,
            CustomerPhone = request.CustomerPhone,
            WeightKg = request.ServiceType == LaundryOrderServiceType.ByWeight ? request.WeightKg : null,
            PricePerKg = request.ServiceType == LaundryOrderServiceType.ByWeight ? request.PricePerKg : null,
            ComforterCount = request.ServiceType == LaundryOrderServiceType.Comforter ? request.ComforterCount : null,
            ComforterSize = request.ServiceType == LaundryOrderServiceType.Comforter ? request.ComforterSize : null,
            PricePerComforter = request.ServiceType == LaundryOrderServiceType.Comforter ? request.PricePerComforter : null,
            TotalPrice = totalPrice,
            ReceivedAt = DateTime.UtcNow,
            EstimatedReadyAt = request.EstimatedReadyAt,
            Notes = request.Notes,
            BranchId = request.BranchId,
            ProcessedByUserId = request.UserId
        };

        await _uow.LaundryOrders.AddAsync(order, ct);
        await _uow.SaveChangesAsync(ct);

        var branch = await _uow.Branches.GetByIdAsync(request.BranchId, ct);
        var user = request.UserId.HasValue ? await _uow.Users.GetByIdAsync(request.UserId.Value, ct) : null;

        return Result.Success(LaundryOrderMapper.ToDto(order, branch?.Name ?? string.Empty,
            user != null ? $"{user.FirstName} {user.LastName}" : null));
    }
}

// ─── Update Status (avanzar el flujo: Received -> InProgress -> Ready -> Delivered, o Cancelled) ───
public record UpdateLaundryOrderStatusCommand : ICommand<LaundryOrderDto>
{
    public Guid Id { get; init; }
    public LaundryOrderStatus NewStatus { get; init; }
}

public class UpdateLaundryOrderStatusValidator : AbstractValidator<UpdateLaundryOrderStatusCommand>
{
    public UpdateLaundryOrderStatusValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class UpdateLaundryOrderStatusHandler : IRequestHandler<UpdateLaundryOrderStatusCommand, Result<LaundryOrderDto>>
{
    private readonly IUnitOfWork _uow;

    public UpdateLaundryOrderStatusHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Result<LaundryOrderDto>> Handle(UpdateLaundryOrderStatusCommand request, CancellationToken ct)
    {
        var order = await _uow.LaundryOrders.GetByIdAsync(request.Id, ct);
        if (order == null)
            return Result.Failure<LaundryOrderDto>("Encargo no encontrado.", "NOT_FOUND");

        order.Status = request.NewStatus;
        if (request.NewStatus == LaundryOrderStatus.Delivered)
            order.DeliveredAt = DateTime.UtcNow;

        await _uow.LaundryOrders.UpdateAsync(order, ct);
        await _uow.SaveChangesAsync(ct);

        var branch = await _uow.Branches.GetByIdAsync(order.BranchId, ct);
        var user = order.ProcessedByUserId.HasValue ? await _uow.Users.GetByIdAsync(order.ProcessedByUserId.Value, ct) : null;

        return Result.Success(LaundryOrderMapper.ToDto(order, branch?.Name ?? string.Empty,
            user != null ? $"{user.FirstName} {user.LastName}" : null));
    }
}

// ─── Register payment ───
public record RegisterLaundryOrderPaymentCommand : ICommand<LaundryOrderDto>
{
    public Guid Id { get; init; }
    public PaymentMethod PaymentMethod { get; init; }
}

public class RegisterLaundryOrderPaymentValidator : AbstractValidator<RegisterLaundryOrderPaymentCommand>
{
    public RegisterLaundryOrderPaymentValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class RegisterLaundryOrderPaymentHandler : IRequestHandler<RegisterLaundryOrderPaymentCommand, Result<LaundryOrderDto>>
{
    private readonly IUnitOfWork _uow;

    public RegisterLaundryOrderPaymentHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Result<LaundryOrderDto>> Handle(RegisterLaundryOrderPaymentCommand request, CancellationToken ct)
    {
        var order = await _uow.LaundryOrders.GetByIdAsync(request.Id, ct);
        if (order == null)
            return Result.Failure<LaundryOrderDto>("Encargo no encontrado.", "NOT_FOUND");

        order.PaymentMethod = request.PaymentMethod;
        order.PaymentStatus = PaymentStatus.Completed;

        await _uow.LaundryOrders.UpdateAsync(order, ct);
        await _uow.SaveChangesAsync(ct);

        var branch = await _uow.Branches.GetByIdAsync(order.BranchId, ct);
        var user = order.ProcessedByUserId.HasValue ? await _uow.Users.GetByIdAsync(order.ProcessedByUserId.Value, ct) : null;

        return Result.Success(LaundryOrderMapper.ToDto(order, branch?.Name ?? string.Empty,
            user != null ? $"{user.FirstName} {user.LastName}" : null));
    }
}

// ─── Cancel / Delete (soft) ───
public record DeleteLaundryOrderCommand(Guid Id) : ICommand;

public class DeleteLaundryOrderHandler : IRequestHandler<DeleteLaundryOrderCommand, Result>
{
    private readonly IUnitOfWork _uow;

    public DeleteLaundryOrderHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Result> Handle(DeleteLaundryOrderCommand request, CancellationToken ct)
    {
        var order = await _uow.LaundryOrders.GetByIdAsync(request.Id, ct);
        if (order == null)
            return Result.Failure("Encargo no encontrado.", "NOT_FOUND");

        order.IsDeleted = true;
        order.IsActive = false;
        order.DeletedAt = DateTime.UtcNow;

        await _uow.LaundryOrders.UpdateAsync(order, ct);
        await _uow.SaveChangesAsync(ct);

        return Result.Success();
    }
}
