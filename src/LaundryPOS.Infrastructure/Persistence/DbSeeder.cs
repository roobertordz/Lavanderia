using LaundryPOS.Domain.Entities;
using LaundryPOS.Domain.Enums;
using LaundryPOS.Domain.Interfaces.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LaundryPOS.Infrastructure.Persistence;

/// <summary>
/// Seeds the default branch and administrator account on first run.
/// Runs after migrations are applied, and is a no-op if data already exists.
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var context = services.GetRequiredService<LaundryDbContext>();
        var passwordHasher = services.GetRequiredService<IPasswordHasher>();

        if (await context.Users.AnyAsync())
        {
            // Core data already seeded on a previous run; still make sure newer
            // seed additions (like demo products/cashier user) get applied independently.
            await SeedProductsAsync(context);
            await SeedCashierAsync(context, passwordHasher);
            return;
        }

        var branch = new Branch
        {
            Name = "Sucursal Centro",
            Code = "SUC-001",
            Address = "Av. Reforma 100, Col. Centro",
            City = "Ciudad de México",
            State = "CDMX",
            ZipCode = "06000",
            Country = "México",
            Phone = "55-1234-5678",
            Email = "centro@laundrypos.com",
            TaxRate = 16.00m,
            Currency = "MXN",
            GracePeriodMinutes = 5,
            OpeningTime = "07:00",
            ClosingTime = "22:00"
        };

        var admin = new User
        {
            Username = "admin",
            Email = "admin@laundrypos.com",
            PasswordHash = passwordHasher.Hash("Admin@123456"),
            FirstName = "Administrador",
            LastName = "Sistema",
            Role = UserRole.Administrator
        };

        admin.UserBranches.Add(new UserBranch { Branch = branch, IsPrimary = true });

        context.Branches.Add(branch);
        context.Users.Add(admin);

        for (var i = 1; i <= 6; i++)
        {
            var isWasher = i <= 4;
            context.Machines.Add(new Machine
            {
                Number = i,
                Name = $"{(isWasher ? "Lavadora" : "Secadora")} {i}",
                Type = isWasher ? MachineType.Washer : MachineType.Dryer,
                Capacity = isWasher ? "18 kg" : "20 kg",
                Price = isWasher ? 35.00m : 25.00m,
                DurationMinutes = isWasher ? 40 : 60,
                Status = MachineStatus.Available,
                Location = $"Fila {(isWasher ? 1 : 2)}, Posición {(isWasher ? i : i - 4)}",
                Branch = branch
            });
        }

        await SeedProductsAsync(context, branch);
        await SeedCashierAsync(context, passwordHasher, branch);

        await context.SaveChangesAsync();
    }

    private static async Task SeedCashierAsync(LaundryDbContext context, IPasswordHasher passwordHasher, Branch? branch = null)
    {
        if (await context.Users.AnyAsync(u => u.Role == UserRole.Cashier))
        {
            return;
        }

        branch ??= await context.Branches.FirstOrDefaultAsync();
        if (branch is null)
        {
            return;
        }

        var cashier = new User
        {
            Username = "cajero",
            Email = "cajero@laundrypos.com",
            PasswordHash = passwordHasher.Hash("Cajero@123456"),
            FirstName = "Cajero",
            LastName = "Demo",
            Role = UserRole.Cashier
        };

        cashier.UserBranches.Add(new UserBranch { Branch = branch, IsPrimary = true });

        context.Users.Add(cashier);
        await context.SaveChangesAsync();
    }

    private static async Task SeedProductsAsync(LaundryDbContext context, Branch? branch = null)
    {
        if (await context.Products.AnyAsync())
        {
            return;
        }

        branch ??= await context.Branches.FirstOrDefaultAsync();
        if (branch is null)
        {
            return;
        }

        foreach (var seed in DemoProducts)
        {
            var product = new Product
            {
                Name = seed.Name,
                Brand = seed.Brand,
                Category = seed.Category,
                Presentation = seed.Presentation,
                Sku = seed.Sku,
                PurchasePrice = seed.PurchasePrice,
                SalePrice = seed.SalePrice,
                StockQuantity = seed.StockQuantity,
                MinStockThreshold = seed.MinStockThreshold,
                Branch = branch
            };

            context.Products.Add(product);
            context.StockMovements.Add(new StockMovement
            {
                Product = product,
                Type = StockMovementType.InitialStock,
                Quantity = seed.StockQuantity,
                PreviousStock = 0,
                NewStock = seed.StockQuantity,
                Reason = "Stock inicial (demo)"
            });
        }

        await context.SaveChangesAsync();
    }

    private static readonly (string Name, string Brand, ProductCategory Category, string Presentation, string Sku, decimal PurchasePrice, decimal SalePrice, int StockQuantity, int MinStockThreshold)[] DemoProducts =
    {
        ("Detergente Líquido Ariel", "Ariel", ProductCategory.Detergent, "1 L", "DET-ARI-1L", 45.00m, 69.00m, 30, 8),
        ("Detergente Líquido Ariel", "Ariel", ProductCategory.Detergent, "5 L", "DET-ARI-5L", 180.00m, 259.00m, 15, 5),
        ("Detergente en Polvo Roma", "Roma", ProductCategory.Detergent, "1 kg", "DET-ROM-1K", 28.00m, 42.00m, 40, 10),
        ("Detergente en Polvo Foca", "Foca", ProductCategory.Detergent, "4 kg", "DET-FOC-4K", 95.00m, 139.00m, 20, 6),
        ("Suavizante Suavitel Primavera", "Suavitel", ProductCategory.FabricSoftener, "1 L", "SUA-SVT-1L", 38.00m, 58.00m, 25, 8),
        ("Suavizante Downy Pasión", "Downy", ProductCategory.FabricSoftener, "900 ml", "SUA-DOW-900", 42.00m, 63.00m, 25, 8),
        ("Blanqueador Cloralex Regular", "Cloralex", ProductCategory.Bleach, "950 ml", "BLA-CLX-950", 22.00m, 34.00m, 30, 10),
        ("Blanqueador Cloralex Floral", "Cloralex", ProductCategory.Bleach, "950 ml", "BLA-CLX-950F", 23.00m, 35.00m, 20, 8),
        ("Quitamanchas Vanish Oxi Action", "Vanish", ProductCategory.StainRemover, "450 ml", "QUI-VAN-450", 55.00m, 79.00m, 15, 5),
        ("Bolsa para Ropa Grande", "LaundryPOS", ProductCategory.Bags, "1 pieza", "BOL-GDE-01", 3.50m, 8.00m, 100, 20),
        ("Bolsa para Ropa Chica", "LaundryPOS", ProductCategory.Bags, "1 pieza", "BOL-CHI-01", 2.00m, 5.00m, 100, 20),
        ("Ganchos de Plástico (paquete)", "LaundryPOS", ProductCategory.Accessories, "Paquete de 10", "ACC-GAN-10", 15.00m, 25.00m, 40, 10),
    };
}

