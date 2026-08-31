using LaundryPOS.Application.Common;
using LaundryPOS.Application.Common.Interfaces;
using LaundryPOS.Application.Common.Models;
using LaundryPOS.Domain.Interfaces.Repositories;
using MediatR;

namespace LaundryPOS.Application.Features.Transactions.Queries;

public record GetTransactionsByBranchQuery(Guid BranchId, DateTime From, DateTime To) : IQuery<IReadOnlyList<TransactionDto>>;

public class GetTransactionsByBranchHandler : IRequestHandler<GetTransactionsByBranchQuery, Result<IReadOnlyList<TransactionDto>>>
{
    private readonly IUnitOfWork _uow;

    public GetTransactionsByBranchHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Result<IReadOnlyList<TransactionDto>>> Handle(GetTransactionsByBranchQuery request, CancellationToken ct)
    {
        var transactions = await _uow.Transactions.GetByBranchAsync(request.BranchId, request.From, request.To, ct);
        var machines = await _uow.Machines.GetByBranchAsync(request.BranchId, ct);
        var branch = await _uow.Branches.GetByIdAsync(request.BranchId, ct);

        var dtos = transactions.Select(t =>
        {
            var machine = machines.FirstOrDefault(m => m.Id == t.MachineId);
            return new TransactionDto
            {
                Id = t.Id,
                TransactionNumber = t.TransactionNumber,
                TransactionDate = t.TransactionDate,
                Amount = t.Amount,
                TaxAmount = t.TaxAmount,
                TotalAmount = t.TotalAmount,
                DiscountAmount = t.DiscountAmount,
                PaymentMethod = t.PaymentMethod,
                PaymentStatus = t.PaymentStatus,
                Status = t.Status,
                PaymentGateway = t.PaymentGateway,
                AuthorizationNumber = t.AuthorizationNumber,
                DurationMinutes = t.DurationMinutes,
                StartTime = t.StartTime,
                EndTime = t.EndTime,
                MachineId = t.MachineId,
                MachineName = machine?.Name ?? "Unknown",
                MachineNumber = machine?.Number ?? 0,
                BranchId = t.BranchId,
                BranchName = branch?.Name ?? string.Empty
            };
        }).ToList().AsReadOnly();

        return Result.Success<IReadOnlyList<TransactionDto>>(dtos);
    }
}

public record GetTransactionByIdQuery(Guid TransactionId) : IQuery<TransactionDto>;

public class GetTransactionByIdHandler : IRequestHandler<GetTransactionByIdQuery, Result<TransactionDto>>
{
    private readonly IUnitOfWork _uow;

    public GetTransactionByIdHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Result<TransactionDto>> Handle(GetTransactionByIdQuery request, CancellationToken ct)
    {
        var tx = await _uow.Transactions.GetByIdAsync(request.TransactionId, ct);
        if (tx == null)
            return Result.Failure<TransactionDto>("Transaction not found.", "NOT_FOUND");

        var machine = await _uow.Machines.GetByIdAsync(tx.MachineId, ct);
        var branch = await _uow.Branches.GetByIdAsync(tx.BranchId, ct);

        return Result.Success(new TransactionDto
        {
            Id = tx.Id,
            TransactionNumber = tx.TransactionNumber,
            TransactionDate = tx.TransactionDate,
            Amount = tx.Amount,
            TaxAmount = tx.TaxAmount,
            TotalAmount = tx.TotalAmount,
            DiscountAmount = tx.DiscountAmount,
            PaymentMethod = tx.PaymentMethod,
            PaymentStatus = tx.PaymentStatus,
            Status = tx.Status,
            PaymentGateway = tx.PaymentGateway,
            AuthorizationNumber = tx.AuthorizationNumber,
            DurationMinutes = tx.DurationMinutes,
            StartTime = tx.StartTime,
            EndTime = tx.EndTime,
            MachineId = tx.MachineId,
            MachineName = machine?.Name ?? "Unknown",
            MachineNumber = machine?.Number ?? 0,
            BranchId = tx.BranchId,
            BranchName = branch?.Name ?? string.Empty
        });
    }
}
