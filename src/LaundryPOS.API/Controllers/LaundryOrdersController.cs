using LaundryPOS.Application.Features.LaundryOrders.Commands;
using LaundryPOS.Application.Features.LaundryOrders.Queries;
using LaundryPOS.Domain.Enums;
using LaundryPOS.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LaundryPOS.API.Controllers;

/// <summary>
/// "Lavado por encargo": servicio de lavado con recepción de ropa (por kilo
/// o edredones) que el cliente recoge después, distinto del autoservicio de
/// máquinas por transacción.
/// </summary>
[Authorize]
public class LaundryOrdersController : BaseApiController
{
    private readonly ICurrentUserService _currentUser;

    public LaundryOrdersController(ICurrentUserService currentUser)
    {
        _currentUser = currentUser;
    }

    /// <summary>
    /// Get all laundry orders for a branch, optionally filtered by status.
    /// </summary>
    [HttpGet("branch/{branchId:guid}")]
    public async Task<IActionResult> GetByBranch(Guid branchId, [FromQuery] LaundryOrderStatus? status)
    {
        var result = await Mediator.Send(new GetLaundryOrdersByBranchQuery(branchId, status));
        return HandleResult(result);
    }

    /// <summary>
    /// Get a specific laundry order.
    /// </summary>
    [HttpGet("{orderId:guid}")]
    public async Task<IActionResult> GetById(Guid orderId)
    {
        var result = await Mediator.Send(new GetLaundryOrderByIdQuery(orderId));
        return HandleResult(result);
    }

    /// <summary>
    /// Create a new drop-off wash order (by weight or comforter).
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "CashierOrAbove")]
    public async Task<IActionResult> Create([FromBody] CreateLaundryOrderCommand command)
    {
        var result = await Mediator.Send(command with { UserId = _currentUser.UserId });
        return HandleResult(result);
    }

    /// <summary>
    /// Advance/change the order's status (Received -> InProgress -> Ready -> Delivered, or Cancelled).
    /// </summary>
    [HttpPatch("{orderId:guid}/status")]
    [Authorize(Policy = "CashierOrAbove")]
    public async Task<IActionResult> UpdateStatus(Guid orderId, [FromBody] UpdateStatusRequest request)
    {
        var result = await Mediator.Send(new UpdateLaundryOrderStatusCommand { Id = orderId, NewStatus = request.Status });
        return HandleResult(result);
    }

    /// <summary>
    /// Register payment for an order (cash/card/etc. — collected at drop-off or pickup).
    /// </summary>
    [HttpPost("{orderId:guid}/payment")]
    [Authorize(Policy = "CashierOrAbove")]
    public async Task<IActionResult> RegisterPayment(Guid orderId, [FromBody] RegisterPaymentRequest request)
    {
        var result = await Mediator.Send(new RegisterLaundryOrderPaymentCommand { Id = orderId, PaymentMethod = request.PaymentMethod });
        return HandleResult(result);
    }

    /// <summary>
    /// Cancel/soft-delete an order.
    /// </summary>
    [HttpDelete("{orderId:guid}")]
    [Authorize(Policy = "SupervisorOrAbove")]
    public async Task<IActionResult> Delete(Guid orderId)
    {
        var result = await Mediator.Send(new DeleteLaundryOrderCommand(orderId));
        return HandleResult(result);
    }
}

public record UpdateStatusRequest(LaundryOrderStatus Status);
public record RegisterPaymentRequest(PaymentMethod PaymentMethod);
