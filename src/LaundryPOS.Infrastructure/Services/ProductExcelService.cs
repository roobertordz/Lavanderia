using ClosedXML.Excel;
using LaundryPOS.Domain.Entities;
using LaundryPOS.Domain.Enums;
using LaundryPOS.Domain.Interfaces.Services;

namespace LaundryPOS.Infrastructure.Services;

public class ProductExcelService : IProductExcelService
{
    private static readonly string[] Headers =
    {
        "Nombre", "Marca", "Categoria", "Presentacion", "SKU", "CodigoBarras",
        "PrecioCompra", "PrecioVenta", "Stock", "StockMinimo"
    };

    private static readonly Dictionary<ProductCategory, string> CategoryLabels = new()
    {
        [ProductCategory.Detergent] = "Detergente",
        [ProductCategory.FabricSoftener] = "Suavizante",
        [ProductCategory.Bleach] = "Blanqueador",
        [ProductCategory.StainRemover] = "Quitamanchas",
        [ProductCategory.Bags] = "Bolsas",
        [ProductCategory.Accessories] = "Accesorios",
        [ProductCategory.Other] = "Otros",
    };

    public byte[] ExportProducts(IEnumerable<Product> products)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Productos");

        for (var i = 0; i < Headers.Length; i++)
        {
            sheet.Cell(1, i + 1).Value = Headers[i];
            sheet.Cell(1, i + 1).Style.Font.Bold = true;
        }

        var row = 2;
        foreach (var p in products)
        {
            sheet.Cell(row, 1).Value = p.Name;
            sheet.Cell(row, 2).Value = p.Brand;
            sheet.Cell(row, 3).Value = CategoryLabels.GetValueOrDefault(p.Category, "Otros");
            sheet.Cell(row, 4).Value = p.Presentation;
            sheet.Cell(row, 5).Value = p.Sku ?? string.Empty;
            sheet.Cell(row, 6).Value = p.Barcode ?? string.Empty;
            sheet.Cell(row, 7).Value = p.PurchasePrice;
            sheet.Cell(row, 8).Value = p.SalePrice;
            sheet.Cell(row, 9).Value = p.StockQuantity;
            sheet.Cell(row, 10).Value = p.MinStockThreshold;
            row++;
        }

        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public ProductImportResult ImportProducts(Stream fileStream)
    {
        var result = new ProductImportResult();

        using var workbook = new XLWorkbook(fileStream);
        var sheet = workbook.Worksheets.First();
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;

        for (var rowNumber = 2; rowNumber <= lastRow; rowNumber++)
        {
            var row = sheet.Row(rowNumber);
            if (row.IsEmpty()) continue;

            var name = row.Cell(1).GetString().Trim();
            var brand = row.Cell(2).GetString().Trim();
            var category = row.Cell(3).GetString().Trim();
            var presentation = row.Cell(4).GetString().Trim();
            var sku = row.Cell(5).GetString().Trim();
            var barcode = row.Cell(6).GetString().Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                result.Errors.Add($"Fila {rowNumber}: el nombre del producto es obligatorio.");
                continue;
            }

            if (!TryGetDecimal(row.Cell(7), out var purchasePrice))
            {
                result.Errors.Add($"Fila {rowNumber}: precio de compra inválido.");
                continue;
            }

            if (!TryGetDecimal(row.Cell(8), out var salePrice))
            {
                result.Errors.Add($"Fila {rowNumber}: precio de venta inválido.");
                continue;
            }

            if (!TryGetInt(row.Cell(9), out var stock))
            {
                result.Errors.Add($"Fila {rowNumber}: stock inválido.");
                continue;
            }

            if (!TryGetInt(row.Cell(10), out var minStock))
            {
                minStock = 5;
            }

            result.Rows.Add(new ProductImportRow(
                name, brand, category, presentation,
                string.IsNullOrWhiteSpace(sku) ? null : sku,
                string.IsNullOrWhiteSpace(barcode) ? null : barcode,
                purchasePrice, salePrice, stock, minStock));
        }

        return result;
    }

    public static ProductCategory ParseCategory(string label) => label.Trim().ToLowerInvariant() switch
    {
        "detergente" or "jabon" or "jabón" or "detergent" => ProductCategory.Detergent,
        "suavizante" or "fabricsoftener" => ProductCategory.FabricSoftener,
        "blanqueador" or "bleach" => ProductCategory.Bleach,
        "quitamanchas" or "stainremover" => ProductCategory.StainRemover,
        "bolsas" or "bags" => ProductCategory.Bags,
        "accesorios" or "accessories" => ProductCategory.Accessories,
        _ => ProductCategory.Other,
    };

    private static bool TryGetDecimal(IXLCell cell, out decimal value)
    {
        if (cell.TryGetValue(out double d)) { value = (decimal)d; return true; }
        return decimal.TryParse(cell.GetString(), out value);
    }

    private static bool TryGetInt(IXLCell cell, out int value)
    {
        if (cell.TryGetValue(out double d)) { value = (int)d; return true; }
        return int.TryParse(cell.GetString(), out value);
    }
}
