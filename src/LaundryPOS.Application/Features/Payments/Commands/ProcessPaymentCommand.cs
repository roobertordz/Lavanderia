using FluentValidation;
using LaundryPOS.Application.Common;
using LaundryPOS.Application.Common.Interfaces;
using LaundryPOS.Application.Common.Models;
using LaundryPOS.Domain.Entities;
using LaundryPOS.Domain.Enums;
using LaundryPOS.Domain.Exceptions;
using LaundryPOS.Domain.Interfaces.Repositories;
using LaundryPOS.Domain.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LaundryPOS.Application.Features.Payments.Commands;

// ─── Process Payment & Start Machine ───
public record ProcessPaymentCommand : ICommand<TransactionDto>
{
    public Guid MachineId { get; init; }
    public Guid BranchId { get; init; }
    public PaymentMethod PaymentMethod { get; init; }
    public string PaymentGateway { get; init; } = string.Empty;
    public Guid? PromotionId { get; init; }
}

public class ProcessPaymentValidator : AbstractValidator<ProcessPaymentCommand>
{
    public ProcessPaymentValidator()
    {
        RuleFor(x => x.MachineId).NotEmpty();
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.PaymentGateway).NotEmpty().MaximumLength(50);
    }
}

public class ProcessPaymentHandler : IRequestHandler<ProcessPaymentCommand, Result<TransactionDto>>
{
    private readonly IUnitOfWork _uow;
    private readonly IPaymentGatewayFactory _paymentFactory;
    private readonly IIoTDriverFactory _iotFactory;
    private readonly IRealTimeNotificationService _realTime;
    private readonly ILogger<ProcessPaymentHandler> _logger;

    public ProcessPaymentHandler(
        IUnitOfWork uow,
        IPaymentGatewayFactory paymentFactory,
        IIoTDriverFactory iotFactory,
        IRealTimeNotificationService realTime,
        ILogger<ProcessPaymentHandler> logger)
    {
        _uow = uow;
        _paymentFactory = paymentFactory;
        _iotFactory = iotFactory;
        _realTime = realTime;
        _logger = logger;
    }

    public async Task<Result<TransactionDto>> Handle(ProcessPaymentCommand request, CancellationToken ct)
    {
        // 1. Validate machine is available
        var machine = await _uow.Machines.GetWithControllerAsync(request.MachineId, ct);
        if (machine == null)
            return Result.Failure<TransactionDto>("Machine not found.", "NOT_FOUND");

        if (machine.Status != MachineStatus.Available)
            return Result.Failure<TransactionDto>("Machine is not available.", "MACHINE_NOT_AVAILABLE");

        if (machine.IoTController == null)
            return Result.Failure<TransactionDto>("Machine has no IoT controller assigned.", "NO_CONTROLLER");

        // 2. Get branch for tax calculation
        var branch = await _uow.Branches.GetByIdAsync(request.BranchId, ct);
        if (branch == null)
            return Result.Failure<TransactionDto>("Branch not found.", "BRANCH_NOT_FOUND");

        // 3. Calculate amounts
        decimal baseAmount = machine.Price;
        decimal discountAmount = 0;

        if (request.PromotionId.HasValue)
        {
            var promotion = await _uow.Promotions.GetByIdAsync(request.PromotionId.Value, ct);
            if (promotion != null && promotion.IsActive && promotion.StartDate <= DateTime.UtcNow && promotion.EndDate >= DateTime.UtcNow)
            {
                discountAmount = promotion.DiscountFixedAmount ?? (baseAmount * promotion.DiscountPercentage / 100);
            }
        }

        decimal taxableAmount = baseAmount - discountAmount;
        decimal taxAmount = taxableAmount * branch.TaxRate / 100;
        decimal totalAmount = taxableAmount + taxAmount;

        // 4. Generate transaction number
        string txNumber = await _uow.Transactions.GenerateTransactionNumberAsync(request.BranchId, ct);

        // 5. Create transaction record
        var transaction = new Transaction
        {
            TransactionNumber = txNumber,
            TransactionDate = DateTime.UtcNow,
            Amount = baseAmount,
            TaxAmount = taxAmount,
            TotalAmount = totalAmount,
            DiscountAmount = discountAmount > 0 ? discountAmount : null,
            PaymentMethod = request.PaymentMethod,
            PaymentStatus = PaymentStatus.Processing,
            Status = TransactionStatus.PaymentPending,
            PaymentGateway = request.PaymentGateway,
            DurationMinutes = machine.DurationMinutes,
            MachineId = machine.Id,
            BranchId = request.BranchId,
            PromotionId = request.PromotionId
        };

        await _uow.Transactions.AddAsync(transaction, ct);
        await _uow.SaveChangesAsync(ct);

        try
        {
            // 6. Process payment
            var gateway = _paymentFactory.GetGateway(request.PaymentGateway);
            var paymentResult = await gateway.ProcessPaymentAsync(new PaymentRequest
            {
                TransactionNumber = txNumber,
                Amount = totalAmount,
                Currency = branch.Currency,
                Method = request.PaymentMethod,
                Description = $"Laundry Service - Machine {machine.Number} - {machine.Name}"
            }, ct);

            if (!paymentResult.Success)
            {
                transaction.PaymentStatus = PaymentStatus.Failed;
                transaction.Status = TransactionStatus.Failed;
                transaction.ErrorMessage = paymentResult.ErrorMessage;
                await _uow.SaveChangesAsync(ct);

                return Result.Failure<TransactionDto>($"Payment failed: {paymentResult.ErrorMessage}", "PAYMENT_FAILED");
            }

            // 7. Update transaction with payment info
            transaction.PaymentStatus = PaymentStatus.Authorized;
            transaction.Status = TransactionStatus.PaymentAuthorized;
            transaction.AuthorizationNumber = paymentResult.AuthorizationNumber;
            transaction.GatewayTransactionId = paymentResult.GatewayTransactionId;
            await _uow.SaveChangesAsync(ct);

            // 8. Send start command to IoT controller
            transaction.Status = TransactionStatus.MachineStarting;
            await _uow.SaveChangesAsync(ct);

            var driver = _iotFactory.GetDriver(machine.IoTController.ControllerType.ToString());
            var startResult = await driver.StartMachineAsync(
                machine.IoTController.ConnectionString ?? machine.IpAddress ?? "",
                machine.DurationMinutes, ct);

            if (!startResult.Success)
            {
                _logger.LogError("Failed to start machine {MachineId}: {Error}", machine.Id, startResult.ErrorMessage);

                // Initiate refund
                await gateway.RefundPaymentAsync(paymentResult.GatewayTransactionId!, totalAmount, ct);
                transaction.PaymentStatus = PaymentStatus.Refunded;
                transaction.Status = TransactionStatus.Refunded;
                transaction.ErrorMessage = $"Machine start failed: {startResult.ErrorMessage}";
                await _uow.SaveChangesAsync(ct);

                return Result.Failure<TransactionDto>("Failed to start machine. Payment has been refunded.", "MACHINE_START_FAILED");
            }

            // 9. Update machine and transaction status
            machine.Status = MachineStatus.InCycle;
            machine.TotalCycles++;
            transaction.Status = TransactionStatus.InProgress;
            transaction.PaymentStatus = PaymentStatus.Completed;
            transaction.StartTime = DateTime.UtcNow;
            transaction.EndTime = DateTime.UtcNow.AddMinutes(machine.DurationMinutes);

            await _uow.Machines.UpdateAsync(machine, ct);
            await _uow.SaveChangesAsync(ct);

            // 10. Notify real-time
            await _realTime.NotifyMachineStatusChangedAsync(request.BranchId, machine.Id, MachineStatus.InCycle, ct);
            await _realTime.NotifyDashboardUpdateAsync(request.BranchId, ct);

            return Result.Success(new TransactionDto
            {
                Id = transaction.Id,
                TransactionNumber = transaction.TransactionNumber,
                TransactionDate = transaction.TransactionDate,
                Amount = transaction.Amount,
                TaxAmount = transaction.TaxAmount,
                TotalAmount = transaction.TotalAmount,
                DiscountAmount = transaction.DiscountAmount,
                PaymentMethod = transaction.PaymentMethod,
                PaymentStatus = transaction.PaymentStatus,
                Status = transaction.Status,
                PaymentGateway = transaction.PaymentGateway,
                AuthorizationNumber = transaction.AuthorizationNumber,
                DurationMinutes = transaction.DurationMinutes,
                StartTime = transaction.StartTime,
                EndTime = transaction.EndTime,
                MachineId = machine.Id,
                MachineName = machine.Name,
                MachineNumber = machine.Number,
                BranchId = request.BranchId,
                BranchName = branch.Name
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment for transaction {TransactionNumber}", txNumber);
            transaction.Status = TransactionStatus.Failed;
            transaction.PaymentStatus = PaymentStatus.Failed;
            transaction.ErrorMessage = "An unexpected error occurred during payment processing.";
            await _uow.SaveChangesAsync(ct);

            return Result.Failure<TransactionDto>("An unexpected error occurred.", "INTERNAL_ERROR");
        }
    }
}
