using LaundryPOS.Application.Features.Products.Commands;
using LaundryPOS.Application.Features.Products.Queries;
using LaundryPOS.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LaundryPOS.API.Controllers;

[Authorize]
public class ProductsController : BaseApiController
{
    private readonly ICurrentUserService _currentUser;

    public ProductsController(ICurrentUserService currentUser)
    {
        _currentUser = currentUser;
    }

    /// <summary>
    /// Get all products for a branch
    /// </summary>
    [HttpGet("branch/{branchId:guid}")]
    public async Task<IActionResult> GetByBranch(Guid branchId)
    {
        var result = await Mediator.Send(new GetProductsByBranchQuery(branchId));
        return HandleResult(result);
    }

    /// <summary>
    /// Get low-stock products for a branch
    /// </summary>
    [HttpGet("branch/{branchId:guid}/low-stock")]
    public async Task<IActionResult> GetLowStock(Guid branchId)
    {
        var result = await Mediator.Send(new GetLowStockProductsQuery(branchId));
        return HandleResult(result);
    }

    /// <summary>
    /// Get a specific product
    /// </summary>
    [HttpGet("{productId:guid}")]
    public async Task<IActionResult> GetById(Guid productId)
    {
        var result = await Mediator.Send(new GetProductByIdQuery(productId));
        return HandleResult(result);
    }

    /// <summary>
    /// Get stock movement history for a product
    /// </summary>
    [HttpGet("{productId:guid}/movements")]
    public async Task<IActionResult> GetMovements(Guid productId)
    {
        var result = await Mediator.Send(new GetProductStockMovementsQuery(productId));
        return HandleResult(result);
    }

    /// <summary>
    /// Create a new product
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "ProductManagementAccess")]
    public async Task<IActionResult> Create([FromBody] CreateProductCommand command)
    {
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Update an existing product
    /// </summary>
    [HttpPut("{productId:guid}")]
    [Authorize(Policy = "ProductManagementAccess")]
    public async Task<IActionResult> Update(Guid productId, [FromBody] UpdateProductCommand command)
    {
        if (productId != command.Id)
            return BadRequest(new ApiResponse { Success = false, Error = "Product ID mismatch" });

        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Soft-delete a product
    /// </summary>
    [HttpDelete("{productId:guid}")]
    [Authorize(Policy = "ProductManagementAccess")]
    public async Task<IActionResult> Delete(Guid productId)
    {
        var result = await Mediator.Send(new DeleteProductCommand(productId));
        return HandleResult(result);
    }

    /// <summary>
    /// Manually adjust stock (add or remove units, with a reason)
    /// </summary>
    [HttpPatch("{productId:guid}/stock")]
    [Authorize(Policy = "EmployeeOrAbove")]
    public async Task<IActionResult> AdjustStock(Guid productId, [FromBody] AdjustStockRequest request)
    {
        var result = await Mediator.Send(new AdjustStockCommand
        {
            ProductId = productId,
            Quantity = request.Quantity,
            Reason = request.Reason,
            UserId = _currentUser.UserId
        });
        return HandleResult(result);
    }

    /// <summary>
    /// Quick-sell units of a product at the counter
    /// </summary>
    [HttpPost("{productId:guid}/sell")]
    [Authorize(Policy = "CashierOrAbove")]
    public async Task<IActionResult> Sell(Guid productId, [FromBody] SellProductRequest request)
    {
        var result = await Mediator.Send(new SellProductCommand
        {
            ProductId = productId,
            Quantity = request.Quantity,
            UserId = _currentUser.UserId
        });
        return HandleResult(result);
    }

    /// <summary>
    /// Export the product catalog to an Excel (.xlsx) file
    /// </summary>
    [HttpGet("branch/{branchId:guid}/export")]
    [Authorize(Policy = "ProductManagementAccess")]
    public async Task<IActionResult> Export(Guid branchId)
    {
        var result = await Mediator.Send(new ExportProductsQuery(branchId));
        if (!result.IsSuccess || result.Value == null)
            return HandleResult(result);

        return File(result.Value, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"productos-{DateTime.UtcNow:yyyyMMdd}.xlsx");
    }

    /// <summary>
    /// Import products in bulk from an Excel (.xlsx) file
    /// </summary>
    [HttpPost("branch/{branchId:guid}/import")]
    [Authorize(Policy = "ProductManagementAccess")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> Import(Guid branchId, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new ApiResponse { Success = false, Error = "No file uploaded." });

        await using var ms = new MemoryStream();
        await file.CopyToAsync(ms);

        var result = await Mediator.Send(new ImportProductsCommand
        {
            BranchId = branchId,
            FileContent = ms.ToArray(),
            UserId = _currentUser.UserId
        });

        return HandleResult(result);
    }
}

public record AdjustStockRequest(int Quantity, string Reason);
public record SellProductRequest(int Quantity);
