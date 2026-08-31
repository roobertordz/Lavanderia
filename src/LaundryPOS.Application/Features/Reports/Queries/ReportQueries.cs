using LaundryPOS.Application.Common;
using LaundryPOS.Application.Common.Interfaces;
using LaundryPOS.Application.Common.Models;
using LaundryPOS.Domain.Enums;
using LaundryPOS.Domain.Interfaces.Repositories;
using MediatR;

namespace LaundryPOS.Application.Features.Reports.Queries;

// ─── Daily Revenue Report ───
public record GetDailyRevenueReportQuery(Guid BranchId, DateTime From, DateTime To) : IQuery<IReadOnlyList<RevenueReportDto>>;

public class GetDailyRevenueReportHandler : IRequestHandler<GetDailyRevenueReportQuery, Result<IReadOnlyList<RevenueReportDto>>>
{
    private readonly IUnitOfWork _uow;

    public GetDailyRevenueReportHandler(IUnitOfWork uow) { _uow = uow; }

    public async Task<Result<IReadOnlyList<RevenueReportDto>>> Handle(GetDailyRevenueReportQuery request, CancellationToken ct)
    {
        var transactions = await _uow.Transactions.GetByBranchAsync(request.BranchId, request.From, request.To, ct);

        var report = transactions
            .Where(t => t.Status == TransactionStatus.Completed || t.Status == TransactionStatus.InProgress)
            .GroupBy(t => t.TransactionDate.Date)
            .Select(g => new RevenueReportDto
            {
                Date = g.Key,
                Revenue = g.Sum(t => t.TotalAmount),
                TransactionCount = g.Count()
            })
            .OrderBy(r => r.Date)
            .ToList()
            .AsReadOnly();

        return Result.Success<IReadOnlyList<RevenueReportDto>>(report);
    }
}

// ─── Machine Usage Report ───
public record GetMachineUsageReportQuery(Guid BranchId, DateTime From, DateTime To) : IQuery<IReadOnlyList<MachineUsageReportDto>>;

public class GetMachineUsageReportHandler : IRequestHandler<GetMachineUsageReportQuery, Result<IReadOnlyList<MachineUsageReportDto>>>
{
    private readonly IUnitOfWork _uow;

    public GetMachineUsageReportHandler(IUnitOfWork uow) { _uow = uow; }

    public async Task<Result<IReadOnlyList<MachineUsageReportDto>>> Handle(GetMachineUsageReportQuery request, CancellationToken ct)
    {
        var transactions = await _uow.Transactions.GetByBranchAsync(request.BranchId, request.From, request.To, ct);
        var machines = await _uow.Machines.GetByBranchAsync(request.BranchId, ct);
        var alerts = await _uow.Alerts.GetUnresolvedByBranchAsync(request.BranchId, ct);

        var report = machines.Select(m =>
        {
            var machineTx = transactions.Where(t => t.MachineId == m.Id).ToList();
            return new MachineUsageReportDto
            {
                MachineId = m.Id,
                MachineName = $"{m.Number} - {m.Name}",
                TotalUses = machineTx.Count,
                TotalRevenue = machineTx.Sum(t => t.TotalAmount),
                AverageUsageMinutes = machineTx.Any() ? machineTx.Average(t => t.DurationMinutes) : 0,
                ErrorCount = alerts.Count(a => a.MachineId == m.Id)
            };
        })
        .OrderByDescending(r => r.TotalUses)
        .ToList()
        .AsReadOnly();

        return Result.Success<IReadOnlyList<MachineUsageReportDto>>(report);
    }
}

// ─── Branch Revenue Report ───
public record GetBranchRevenueReportQuery(DateTime From, DateTime To) : IQuery<IReadOnlyList<BranchRevenueReportDto>>;

public class GetBranchRevenueReportHandler : IRequestHandler<GetBranchRevenueReportQuery, Result<IReadOnlyList<BranchRevenueReportDto>>>
{
    private readonly IUnitOfWork _uow;

    public GetBranchRevenueReportHandler(IUnitOfWork uow) { _uow = uow; }

    public async Task<Result<IReadOnlyList<BranchRevenueReportDto>>> Handle(GetBranchRevenueReportQuery request, CancellationToken ct)
    {
        var branches = await _uow.Branches.GetAllAsync(ct);
        var results = new List<BranchRevenueReportDto>();

        foreach (var branch in branches.Where(b => b.IsActive))
        {
            var revenue = await _uow.Transactions.GetTotalRevenueAsync(branch.Id, request.From, request.To, ct);
            var transactions = await _uow.Transactions.GetByBranchAsync(branch.Id, request.From, request.To, ct);

            results.Add(new BranchRevenueReportDto
            {
                BranchId = branch.Id,
                BranchName = branch.Name,
                TotalRevenue = revenue,
                TotalTransactions = transactions.Count
            });
        }

        return Result.Success<IReadOnlyList<BranchRevenueReportDto>>(results.OrderByDescending(r => r.TotalRevenue).ToList().AsReadOnly());
    }
}
