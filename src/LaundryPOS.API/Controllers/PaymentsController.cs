using LaundryPOS.Application.Features.Payments.Commands;
using LaundryPOS.Application.Features.Transactions.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LaundryPOS.API.Controllers;

[Authorize]
public class PaymentsController : BaseApiController
{
    /// <summary>
    /// Process a payment and start the machine.
    /// This is the core kiosk endpoint.
    /// Flow: Select machine → Show cost → Pay → Start machine
    /// </summary>
    [HttpPost("process")]
    [AllowAnonymous] // Kiosk doesn't require user login
    public async Task<IActionResult> ProcessPayment([FromBody] ProcessPaymentCommand command)
    {
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }
}

public class TransactionsController : BaseApiController
{
    /// <summary>
    /// Get transactions for a branch within a date range
    /// </summary>
    [HttpGet("branch/{branchId:guid}")]
    [Authorize(Policy = "SupervisorOrAbove")]
    public async Task<IActionResult> GetByBranch(Guid branchId, [FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        var result = await Mediator.Send(new GetTransactionsByBranchQuery(branchId, from, to));
        return HandleResult(result);
    }

    /// <summary>
    /// Get a specific transaction
    /// </summary>
    [HttpGet("{transactionId:guid}")]
    [Authorize]
    public async Task<IActionResult> GetById(Guid transactionId)
    {
        var result = await Mediator.Send(new GetTransactionByIdQuery(transactionId));
        return HandleResult(result);
    }
}
