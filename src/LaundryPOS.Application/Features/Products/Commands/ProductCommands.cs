using FluentValidation;
using LaundryPOS.Application.Common;
using LaundryPOS.Application.Common.Interfaces;
using LaundryPOS.Application.Common.Models;
using LaundryPOS.Domain.Entities;
using LaundryPOS.Domain.Enums;
using LaundryPOS.Domain.Interfaces.Repositories;
using LaundryPOS.Domain.Interfaces.Services;
using MediatR;

namespace LaundryPOS.Application.Features.Products.Commands;

internal static class ProductMapper
{
    public static ProductDto ToDto(Product p, string branchName) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Brand = p.Brand,
        Category = p.Category,
        Presentation = p.Presentation,
        Sku = p.Sku,
        Barcode = p.Barcode,
        ImageUrl = p.ImageUrl,
        PurchasePrice = p.PurchasePrice,
        SalePrice = p.SalePrice,
        StockQuantity = p.StockQuantity,
        MinStockThreshold = p.MinStockThreshold,
        IsLowStock = p.IsLowStock,
        Notes = p.Notes,
        IsActive = p.IsActive,
        BranchId = p.BranchId,
        BranchName = branchName
    };
}

// ─── Create Product ───
public record CreateProductCommand : ICommand<ProductDto>
{
    public string Name { get; init; } = string.Empty;
    public string Brand { get; init; } = string.Empty;
    public ProductCategory Category { get; init; }
    public string Presentation { get; init; } = string.Empty;
    public string? Sku { get; init; }
    public string? Barcode { get; init; }
    public string? ImageUrl { get; init; }
    public decimal PurchasePrice { get; init; }
    public decimal SalePrice { get; init; }
    public int StockQuantity { get; init; }
    public int MinStockThreshold { get; init; } = 5;
    public string? Notes { get; init; }
    public Guid BranchId { get; init; }
}

public class CreateProductValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Brand).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Presentation).NotEmpty().MaximumLength(50);
        RuleFor(x => x.PurchasePrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.SalePrice).GreaterThan(0);
        RuleFor(x => x.StockQuantity).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MinStockThreshold).GreaterThanOrEqualTo(0);
        RuleFor(x => x.BranchId).NotEmpty();
    }
}

public class CreateProductHandler : IRequestHandler<CreateProductCommand, Result<ProductDto>>
{
    private readonly IUnitOfWork _uow;

    public CreateProductHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Result<ProductDto>> Handle(CreateProductCommand request, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(request.Sku))
        {
            var existing = await _uow.Products.GetBySkuAsync(request.Sku, request.BranchId, ct);
            if (existing != null)
                return Result.Failure<ProductDto>("A product with this SKU already exists in this branch.", "DUPLICATE_SKU");
        }

        var product = new Product
        {
            Name = request.Name,
            Brand = request.Brand,
            Category = request.Category,
            Presentation = request.Presentation,
            Sku = request.Sku,
            Barcode = request.Barcode,
            ImageUrl = request.ImageUrl,
            PurchasePrice = request.PurchasePrice,
            SalePrice = request.SalePrice,
            StockQuantity = request.StockQuantity,
            MinStockThreshold = request.MinStockThreshold,
            Notes = request.Notes,
            BranchId = request.BranchId
        };

        await _uow.Products.AddAsync(product, ct);

        if (request.StockQuantity > 0)
        {
            await _uow.StockMovements.AddAsync(new StockMovement
            {
                ProductId = product.Id,
                Type = StockMovementType.InitialStock,
                Quantity = request.StockQuantity,
                PreviousStock = 0,
                NewStock = request.StockQuantity,
                Reason = "Stock inicial"
            }, ct);
        }

        await _uow.SaveChangesAsync(ct);

        var branch = await _uow.Branches.GetByIdAsync(request.BranchId, ct);
        return Result.Success(ProductMapper.ToDto(product, branch?.Name ?? string.Empty));
    }
}

// ─── Update Product ───
public record UpdateProductCommand : ICommand<ProductDto>
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Brand { get; init; } = string.Empty;
    public ProductCategory Category { get; init; }
    public string Presentation { get; init; } = string.Empty;
    public string? Sku { get; init; }
    public string? Barcode { get; init; }
    public string? ImageUrl { get; init; }
    public decimal PurchasePrice { get; init; }
    public decimal SalePrice { get; init; }
    public int MinStockThreshold { get; init; }
    public string? Notes { get; init; }
}

public class UpdateProductValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Brand).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Presentation).NotEmpty().MaximumLength(50);
        RuleFor(x => x.PurchasePrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.SalePrice).GreaterThan(0);
        RuleFor(x => x.MinStockThreshold).GreaterThanOrEqualTo(0);
    }
}

public class UpdateProductHandler : IRequestHandler<UpdateProductCommand, Result<ProductDto>>
{
    private readonly IUnitOfWork _uow;

    public UpdateProductHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Result<ProductDto>> Handle(UpdateProductCommand request, CancellationToken ct)
    {
        var product = await _uow.Products.GetByIdAsync(request.Id, ct);
        if (product == null)
            return Result.Failure<ProductDto>("Product not found.", "NOT_FOUND");

        if (!string.IsNullOrWhiteSpace(request.Sku) && request.Sku != product.Sku)
        {
            var existing = await _uow.Products.GetBySkuAsync(request.Sku, product.BranchId, ct);
            if (existing != null && existing.Id != product.Id)
                return Result.Failure<ProductDto>("A product with this SKU already exists in this branch.", "DUPLICATE_SKU");
        }

        product.Name = request.Name;
        product.Brand = request.Brand;
        product.Category = request.Category;
        product.Presentation = request.Presentation;
        product.Sku = request.Sku;
        product.Barcode = request.Barcode;
        product.ImageUrl = request.ImageUrl;
        product.PurchasePrice = request.PurchasePrice;
        product.SalePrice = request.SalePrice;
        product.MinStockThreshold = request.MinStockThreshold;
        product.Notes = request.Notes;

        await _uow.Products.UpdateAsync(product, ct);
        await _uow.SaveChangesAsync(ct);

        var branch = await _uow.Branches.GetByIdAsync(product.BranchId, ct);
        return Result.Success(ProductMapper.ToDto(product, branch?.Name ?? string.Empty));
    }
}

// ─── Delete Product (Soft) ───
public record DeleteProductCommand(Guid ProductId) : ICommand;

public class DeleteProductHandler : IRequestHandler<DeleteProductCommand, Result>
{
    private readonly IUnitOfWork _uow;

    public DeleteProductHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Result> Handle(DeleteProductCommand request, CancellationToken ct)
    {
        var product = await _uow.Products.GetByIdAsync(request.ProductId, ct);
        if (product == null)
            return Result.Failure("Product not found.", "NOT_FOUND");

        product.IsDeleted = true;
        product.IsActive = false;
        product.DeletedAt = DateTime.UtcNow;

        await _uow.Products.UpdateAsync(product, ct);
        await _uow.SaveChangesAsync(ct);

        return Result.Success();
    }
}

// ─── Adjust Stock (manual +/-) ───
public record AdjustStockCommand : ICommand<ProductDto>
{
    public Guid ProductId { get; init; }
    public int Quantity { get; init; } // signed: positive = add, negative = remove
    public string Reason { get; init; } = string.Empty;
    public Guid? UserId { get; init; }
}

public class AdjustStockValidator : AbstractValidator<AdjustStockCommand>
{
    public AdjustStockValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Quantity).NotEqual(0);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

public class AdjustStockHandler : IRequestHandler<AdjustStockCommand, Result<ProductDto>>
{
    private readonly IUnitOfWork _uow;

    public AdjustStockHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Result<ProductDto>> Handle(AdjustStockCommand request, CancellationToken ct)
    {
        var product = await _uow.Products.GetByIdAsync(request.ProductId, ct);
        if (product == null)
            return Result.Failure<ProductDto>("Product not found.", "NOT_FOUND");

        var newStock = product.StockQuantity + request.Quantity;
        if (newStock < 0)
            return Result.Failure<ProductDto>("Stock cannot be negative.", "INVALID_STOCK");

        await _uow.StockMovements.AddAsync(new StockMovement
        {
            ProductId = product.Id,
            Type = StockMovementType.ManualAdjustment,
            Quantity = request.Quantity,
            PreviousStock = product.StockQuantity,
            NewStock = newStock,
            Reason = request.Reason,
            UserId = request.UserId
        }, ct);

        product.StockQuantity = newStock;
        await _uow.Products.UpdateAsync(product, ct);
        await _uow.SaveChangesAsync(ct);

        var branch = await _uow.Branches.GetByIdAsync(product.BranchId, ct);
        return Result.Success(ProductMapper.ToDto(product, branch?.Name ?? string.Empty));
    }
}

// ─── Sell Product (quick sale) ───
public record SellProductCommand : ICommand<ProductDto>
{
    public Guid ProductId { get; init; }
    public int Quantity { get; init; }
    public Guid? UserId { get; init; }
}

public class SellProductValidator : AbstractValidator<SellProductCommand>
{
    public SellProductValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
    }
}

public class SellProductHandler : IRequestHandler<SellProductCommand, Result<ProductDto>>
{
    private readonly IUnitOfWork _uow;

    public SellProductHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Result<ProductDto>> Handle(SellProductCommand request, CancellationToken ct)
    {
        var product = await _uow.Products.GetByIdAsync(request.ProductId, ct);
        if (product == null)
            return Result.Failure<ProductDto>("Product not found.", "NOT_FOUND");

        if (product.StockQuantity < request.Quantity)
            return Result.Failure<ProductDto>("Insufficient stock for this sale.", "INSUFFICIENT_STOCK");

        var newStock = product.StockQuantity - request.Quantity;

        await _uow.StockMovements.AddAsync(new StockMovement
        {
            ProductId = product.Id,
            Type = StockMovementType.Sale,
            Quantity = -request.Quantity,
            PreviousStock = product.StockQuantity,
            NewStock = newStock,
            Reason = "Venta directa de mostrador",
            UserId = request.UserId
        }, ct);

        product.StockQuantity = newStock;
        await _uow.Products.UpdateAsync(product, ct);
        await _uow.SaveChangesAsync(ct);

        var branch = await _uow.Branches.GetByIdAsync(product.BranchId, ct);
        return Result.Success(ProductMapper.ToDto(product, branch?.Name ?? string.Empty));
    }
}

// ─── Import Products from Excel ───
public record ImportProductsCommand : ICommand<ProductImportSummaryDto>
{
    public Guid BranchId { get; init; }
    public byte[] FileContent { get; init; } = [];
    public Guid? UserId { get; init; }
}

public class ImportProductsValidator : AbstractValidator<ImportProductsCommand>
{
    public ImportProductsValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.FileContent).NotEmpty();
    }
}

public class ImportProductsHandler : IRequestHandler<ImportProductsCommand, Result<ProductImportSummaryDto>>
{
    private readonly IUnitOfWork _uow;
    private readonly IProductExcelService _excelService;

    public ImportProductsHandler(IUnitOfWork uow, IProductExcelService excelService)
    {
        _uow = uow;
        _excelService = excelService;
    }

    public async Task<Result<ProductImportSummaryDto>> Handle(ImportProductsCommand request, CancellationToken ct)
    {
        using var stream = new MemoryStream(request.FileContent);
        var parsed = _excelService.ImportProducts(stream);

        var errors = new List<string>(parsed.Errors);
        var imported = 0;

        foreach (var row in parsed.Rows)
        {
            var category = ParseCategory(row.Category);

            Domain.Entities.Product? product = null;
            if (!string.IsNullOrWhiteSpace(row.Sku))
                product = await _uow.Products.GetBySkuAsync(row.Sku, request.BranchId, ct);

            if (product != null)
            {
                // Existing product: update details and add the imported quantity as a stock movement
                var previousStock = product.StockQuantity;
                var newStock = previousStock + row.StockQuantity;

                product.Name = row.Name;
                product.Brand = row.Brand;
                product.Category = category;
                product.Presentation = row.Presentation;
                product.Barcode = row.Barcode ?? product.Barcode;
                product.PurchasePrice = row.PurchasePrice;
                product.SalePrice = row.SalePrice;
                product.MinStockThreshold = row.MinStockThreshold;
                product.StockQuantity = newStock;

                await _uow.Products.UpdateAsync(product, ct);

                if (row.StockQuantity != 0)
                {
                    await _uow.StockMovements.AddAsync(new StockMovement
                    {
                        ProductId = product.Id,
                        Type = StockMovementType.Import,
                        Quantity = row.StockQuantity,
                        PreviousStock = previousStock,
                        NewStock = newStock,
                        Reason = "Importación desde Excel",
                        UserId = request.UserId
                    }, ct);
                }
            }
            else
            {
                product = new Domain.Entities.Product
                {
                    Name = row.Name,
                    Brand = row.Brand,
                    Category = category,
                    Presentation = row.Presentation,
                    Sku = row.Sku,
                    Barcode = row.Barcode,
                    PurchasePrice = row.PurchasePrice,
                    SalePrice = row.SalePrice,
                    StockQuantity = row.StockQuantity,
                    MinStockThreshold = row.MinStockThreshold,
                    BranchId = request.BranchId
                };

                await _uow.Products.AddAsync(product, ct);

                if (row.StockQuantity > 0)
                {
                    await _uow.StockMovements.AddAsync(new StockMovement
                    {
                        ProductId = product.Id,
                        Type = StockMovementType.Import,
                        Quantity = row.StockQuantity,
                        PreviousStock = 0,
                        NewStock = row.StockQuantity,
                        Reason = "Importación desde Excel (nuevo producto)",
                        UserId = request.UserId
                    }, ct);
                }
            }

            imported++;
        }

        await _uow.SaveChangesAsync(ct);

        return Result.Success(new ProductImportSummaryDto
        {
            Imported = imported,
            Failed = errors.Count,
            Errors = errors
        });
    }

    private static ProductCategory ParseCategory(string label) => label.Trim().ToLowerInvariant() switch
    {
        "detergente" or "jabon" or "jabón" or "detergent" => ProductCategory.Detergent,
        "suavizante" or "fabricsoftener" => ProductCategory.FabricSoftener,
        "blanqueador" or "bleach" => ProductCategory.Bleach,
        "quitamanchas" or "stainremover" => ProductCategory.StainRemover,
        "bolsas" or "bags" => ProductCategory.Bags,
        "accesorios" or "accessories" => ProductCategory.Accessories,
        _ => ProductCategory.Other,
    };
}
