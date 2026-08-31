using LaundryPOS.Application.Common;
using LaundryPOS.Application.Common.Interfaces;
using LaundryPOS.Application.Common.Models;
using LaundryPOS.Application.Features.Products.Commands;
using LaundryPOS.Domain.Interfaces.Repositories;
using LaundryPOS.Domain.Interfaces.Services;
using MediatR;

namespace LaundryPOS.Application.Features.Products.Queries;

// ─── Get Products by Branch ───
public record GetProductsByBranchQuery(Guid BranchId) : IQuery<IReadOnlyList<ProductDto>>;

public class GetProductsByBranchHandler : IRequestHandler<GetProductsByBranchQuery, Result<IReadOnlyList<ProductDto>>>
{
    private readonly IUnitOfWork _uow;

    public GetProductsByBranchHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Result<IReadOnlyList<ProductDto>>> Handle(GetProductsByBranchQuery request, CancellationToken ct)
    {
        var products = await _uow.Products.GetByBranchAsync(request.BranchId, ct);
        var branch = await _uow.Branches.GetByIdAsync(request.BranchId, ct);
        var branchName = branch?.Name ?? string.Empty;

        IReadOnlyList<ProductDto> dtos = products.Select(p => ProductMapper.ToDto(p, branchName)).ToList();
        return Result.Success(dtos);
    }
}

// ─── Get Product By Id ───
public record GetProductByIdQuery(Guid ProductId) : IQuery<ProductDto>;

public class GetProductByIdHandler : IRequestHandler<GetProductByIdQuery, Result<ProductDto>>
{
    private readonly IUnitOfWork _uow;

    public GetProductByIdHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Result<ProductDto>> Handle(GetProductByIdQuery request, CancellationToken ct)
    {
        var product = await _uow.Products.GetByIdAsync(request.ProductId, ct);
        if (product == null)
            return Result.Failure<ProductDto>("Product not found.", "NOT_FOUND");

        var branch = await _uow.Branches.GetByIdAsync(product.BranchId, ct);
        return Result.Success(ProductMapper.ToDto(product, branch?.Name ?? string.Empty));
    }
}

// ─── Get Low Stock Products ───
public record GetLowStockProductsQuery(Guid BranchId) : IQuery<IReadOnlyList<ProductDto>>;

public class GetLowStockProductsHandler : IRequestHandler<GetLowStockProductsQuery, Result<IReadOnlyList<ProductDto>>>
{
    private readonly IUnitOfWork _uow;

    public GetLowStockProductsHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Result<IReadOnlyList<ProductDto>>> Handle(GetLowStockProductsQuery request, CancellationToken ct)
    {
        var products = await _uow.Products.GetLowStockByBranchAsync(request.BranchId, ct);
        var branch = await _uow.Branches.GetByIdAsync(request.BranchId, ct);
        var branchName = branch?.Name ?? string.Empty;

        IReadOnlyList<ProductDto> dtos = products.Select(p => ProductMapper.ToDto(p, branchName)).ToList();
        return Result.Success(dtos);
    }
}

// ─── Get Stock Movements for a Product ───
public record GetProductStockMovementsQuery(Guid ProductId) : IQuery<IReadOnlyList<StockMovementDto>>;

public class GetProductStockMovementsHandler : IRequestHandler<GetProductStockMovementsQuery, Result<IReadOnlyList<StockMovementDto>>>
{
    private readonly IUnitOfWork _uow;

    public GetProductStockMovementsHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Result<IReadOnlyList<StockMovementDto>>> Handle(GetProductStockMovementsQuery request, CancellationToken ct)
    {
        var product = await _uow.Products.GetByIdAsync(request.ProductId, ct);
        if (product == null)
            return Result.Failure<IReadOnlyList<StockMovementDto>>("Product not found.", "NOT_FOUND");

        var movements = await _uow.StockMovements.GetByProductAsync(request.ProductId, ct);

        IReadOnlyList<StockMovementDto> dtos = movements.Select(m => new StockMovementDto
        {
            Id = m.Id,
            ProductId = m.ProductId,
            ProductName = product.Name,
            Type = m.Type,
            Quantity = m.Quantity,
            PreviousStock = m.PreviousStock,
            NewStock = m.NewStock,
            Reason = m.Reason,
            UserName = m.User != null ? $"{m.User.FirstName} {m.User.LastName}" : null,
            CreatedAt = m.CreatedAt
        }).ToList();

        return Result.Success(dtos);
    }
}

// ─── Export Products to Excel ───
public record ExportProductsQuery(Guid BranchId) : IQuery<byte[]>;

public class ExportProductsHandler : IRequestHandler<ExportProductsQuery, Result<byte[]>>
{
    private readonly IUnitOfWork _uow;
    private readonly IProductExcelService _excelService;

    public ExportProductsHandler(IUnitOfWork uow, IProductExcelService excelService)
    {
        _uow = uow;
        _excelService = excelService;
    }

    public async Task<Result<byte[]>> Handle(ExportProductsQuery request, CancellationToken ct)
    {
        var products = await _uow.Products.GetByBranchAsync(request.BranchId, ct);
        var bytes = _excelService.ExportProducts(products);
        return Result.Success(bytes);
    }
}
