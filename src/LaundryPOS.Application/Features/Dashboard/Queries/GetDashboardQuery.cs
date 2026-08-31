using LaundryPOS.Application.Common;
using LaundryPOS.Application.Common.Interfaces;
using LaundryPOS.Application.Common.Models;
using LaundryPOS.Domain.Enums;
using LaundryPOS.Domain.Interfaces.Repositories;
using MediatR;

namespace LaundryPOS.Application.Features.Dashboard.Queries;

public record GetDashboardQuery(Guid BranchId) : IQuery<DashboardDto>;

public class GetDashboardHandler : IRequestHandler<GetDashboardQuery, Result<DashboardDto>>
{
    private readonly IUnitOfWork _uow;

    public GetDashboardHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Result<DashboardDto>> Handle(GetDashboardQuery request, CancellationToken ct)
    {
        var today = DateTime.UtcNow.Date;
        var monthStart = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var tomorrow = today.AddDays(1);

        // Machines
        var machines = await _uow.Machines.GetByBranchAsync(request.BranchId, ct);
        var occupied = machines.Count(m => m.Status == MachineStatus.Occupied || m.Status == MachineStatus.InCycle);
        var available = machines.Count(m => m.Status == MachineStatus.Available);
        var outOfService = machines.Count(m => m.Status == MachineStatus.OutOfService || m.Status == MachineStatus.Error);
        var maintenance = machines.Count(m => m.Status == MachineStatus.Maintenance);

        // Revenue
        var todaySales = await _uow.Transactions.GetTotalRevenueAsync(request.BranchId, today, tomorrow, ct);
        var monthSales = await _uow.Transactions.GetTotalRevenueAsync(request.BranchId, monthStart, tomorrow, ct);

        // Today's transactions
        var todayTx = await _uow.Transactions.GetByBranchAsync(request.BranchId, today, tomorrow, ct);

        // Alerts
        var alerts = await _uow.Alerts.GetUnresolvedByBranchAsync(request.BranchId, ct);

        // Machine statuses for visual map
        var machineStatuses = machines.Select(m => new MachineStatusSummaryDto
        {
            MachineId = m.Id,
            Number = m.Number,
            Name = m.Name,
            Type = m.Type,
            Status = m.Status,
            CommunicationStatus = m.CommunicationStatus,
            RemainingMinutes = m.Status == MachineStatus.InCycle
                ? (int?)Math.Max(0, (todayTx
                    .Where(t => t.MachineId == m.Id && t.EndTime.HasValue)
                    .Select(t => (t.EndTime!.Value - DateTime.UtcNow).TotalMinutes)
                    .FirstOrDefault()))
                : null
        }).ToList().AsReadOnly();

        // Recent transactions
        var recentTx = todayTx
            .OrderByDescending(t => t.TransactionDate)
            .Take(10)
            .Select(t => new RecentTransactionDto
            {
                TransactionNumber = t.TransactionNumber,
                TransactionDate = t.TransactionDate,
                TotalAmount = t.TotalAmount,
                MachineName = machines.FirstOrDefault(m => m.Id == t.MachineId)?.Name ?? "Unknown",
                Status = t.Status
            }).ToList().AsReadOnly();

        // Recent alerts
        var recentAlerts = alerts
            .OrderByDescending(a => a.CreatedAt)
            .Take(5)
            .Select(a => new AlertDto
            {
                Id = a.Id,
                Title = a.Title,
                Message = a.Message,
                Severity = a.Severity,
                CreatedAt = a.CreatedAt,
                MachineName = machines.FirstOrDefault(m => m.Id == a.MachineId)?.Name ?? "Unknown",
                IsResolved = a.IsResolved
            }).ToList().AsReadOnly();

        return Result.Success(new DashboardDto
        {
            TodaySales = todaySales,
            MonthSales = monthSales,
            TotalRevenue = monthSales,
            OccupiedMachines = occupied,
            AvailableMachines = available,
            OutOfServiceMachines = outOfService,
            MaintenanceMachines = maintenance,
            TotalMachines = machines.Count,
            TodayTransactions = todayTx.Count,
            ActiveAlerts = alerts.Count,
            MachineStatuses = machineStatuses,
            RecentTransactions = recentTx,
            RecentAlerts = recentAlerts
        });
    }
}
