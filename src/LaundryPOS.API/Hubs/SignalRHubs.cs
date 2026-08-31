using LaundryPOS.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace LaundryPOS.API.Hubs;

[Authorize]
public class MachineHub : Hub
{
    public async Task JoinBranchGroup(string branchId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"branch-{branchId}");
    }

    public async Task LeaveBranchGroup(string branchId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"branch-{branchId}");
    }

    public override async Task OnConnectedAsync()
    {
        var branchClaims = Context.User?.FindAll("branch");
        if (branchClaims != null)
        {
            foreach (var claim in branchClaims)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"branch-{claim.Value}");
            }
        }
        await base.OnConnectedAsync();
    }
}

[Authorize]
public class DashboardHub : Hub
{
    public async Task JoinBranchDashboard(string branchId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"dashboard-{branchId}");
    }

    public async Task LeaveBranchDashboard(string branchId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"dashboard-{branchId}");
    }
}

/// <summary>
/// Implementation of IRealTimeNotificationService using SignalR
/// </summary>
public class SignalRNotificationService : Domain.Interfaces.Services.IRealTimeNotificationService
{
    private readonly IHubContext<MachineHub> _machineHub;
    private readonly IHubContext<DashboardHub> _dashboardHub;

    public SignalRNotificationService(IHubContext<MachineHub> machineHub, IHubContext<DashboardHub> dashboardHub)
    {
        _machineHub = machineHub;
        _dashboardHub = dashboardHub;
    }

    public async Task NotifyMachineStatusChangedAsync(Guid branchId, Guid machineId, MachineStatus status, CancellationToken ct = default)
    {
        await _machineHub.Clients.Group($"branch-{branchId}")
            .SendAsync("MachineStatusChanged", new { MachineId = machineId, Status = status.ToString(), Timestamp = DateTime.UtcNow }, ct);
    }

    public async Task NotifyTransactionCompletedAsync(Guid branchId, Guid transactionId, CancellationToken ct = default)
    {
        await _dashboardHub.Clients.Group($"dashboard-{branchId}")
            .SendAsync("TransactionCompleted", new { TransactionId = transactionId, Timestamp = DateTime.UtcNow }, ct);
    }

    public async Task NotifyAlertCreatedAsync(Guid branchId, Guid alertId, AlertSeverity severity, string message, CancellationToken ct = default)
    {
        await _dashboardHub.Clients.Group($"dashboard-{branchId}")
            .SendAsync("AlertCreated", new { AlertId = alertId, Severity = severity.ToString(), Message = message, Timestamp = DateTime.UtcNow }, ct);
    }

    public async Task NotifyDashboardUpdateAsync(Guid branchId, CancellationToken ct = default)
    {
        await _dashboardHub.Clients.Group($"dashboard-{branchId}")
            .SendAsync("DashboardUpdate", new { Timestamp = DateTime.UtcNow }, ct);
    }
}
