using LaundryPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LaundryPOS.Infrastructure.Persistence.Configurations;

public class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> builder)
    {
        builder.ToTable("Branches");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Name).IsRequired().HasMaxLength(200);
        builder.Property(b => b.Code).IsRequired().HasMaxLength(20);
        builder.HasIndex(b => b.Code).IsUnique();
        builder.Property(b => b.Address).IsRequired().HasMaxLength(500);
        builder.Property(b => b.City).IsRequired().HasMaxLength(100);
        builder.Property(b => b.State).IsRequired().HasMaxLength(100);
        builder.Property(b => b.ZipCode).HasMaxLength(20);
        builder.Property(b => b.Country).HasMaxLength(100);
        builder.Property(b => b.Phone).HasMaxLength(20);
        builder.Property(b => b.Email).HasMaxLength(200);
        builder.Property(b => b.Currency).HasMaxLength(10).HasDefaultValue("MXN");
        builder.Property(b => b.TaxRate).HasPrecision(5, 2);
        builder.HasQueryFilter(b => !b.IsDeleted);
    }
}

public class MachineConfiguration : IEntityTypeConfiguration<Machine>
{
    public void Configure(EntityTypeBuilder<Machine> builder)
    {
        builder.ToTable("Machines");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Name).IsRequired().HasMaxLength(100);
        builder.Property(m => m.Capacity).HasMaxLength(50);
        builder.Property(m => m.Price).HasPrecision(10, 2);
        builder.Property(m => m.Location).HasMaxLength(200);
        builder.Property(m => m.IpAddress).HasMaxLength(50);
        builder.Property(m => m.Model).HasMaxLength(100);
        builder.Property(m => m.Brand).HasMaxLength(100);
        builder.Property(m => m.SerialNumber).HasMaxLength(100);
        builder.HasIndex(m => new { m.Number, m.BranchId }).IsUnique();
        builder.HasOne(m => m.Branch).WithMany(b => b.Machines).HasForeignKey(m => m.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(m => m.IoTController).WithMany(c => c.Machines).HasForeignKey(m => m.IoTControllerId).OnDelete(DeleteBehavior.SetNull);
        builder.HasQueryFilter(m => !m.IsDeleted);
    }
}

public class IoTControllerConfiguration : IEntityTypeConfiguration<IoTController>
{
    public void Configure(EntityTypeBuilder<IoTController> builder)
    {
        builder.ToTable("IoTControllers");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).IsRequired().HasMaxLength(100);
        builder.Property(c => c.IpAddress).HasMaxLength(50);
        builder.Property(c => c.MacAddress).HasMaxLength(20);
        builder.Property(c => c.FirmwareVersion).HasMaxLength(50);
        builder.Property(c => c.ProtocolType).HasMaxLength(20);
        builder.Property(c => c.ConnectionString).HasMaxLength(500);
        builder.Property(c => c.MqttTopic).HasMaxLength(200);
        builder.HasOne(c => c.Branch).WithMany().HasForeignKey(c => c.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasQueryFilter(c => !c.IsDeleted);
    }
}

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("Transactions");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.TransactionNumber).IsRequired().HasMaxLength(50);
        builder.HasIndex(t => t.TransactionNumber).IsUnique();
        builder.Property(t => t.Amount).HasPrecision(10, 2);
        builder.Property(t => t.TaxAmount).HasPrecision(10, 2);
        builder.Property(t => t.TotalAmount).HasPrecision(10, 2);
        builder.Property(t => t.DiscountAmount).HasPrecision(10, 2);
        builder.Property(t => t.PaymentGateway).HasMaxLength(50);
        builder.Property(t => t.AuthorizationNumber).HasMaxLength(100);
        builder.Property(t => t.PaymentReference).HasMaxLength(200);
        builder.Property(t => t.GatewayTransactionId).HasMaxLength(200);
        builder.Property(t => t.ErrorMessage).HasMaxLength(500);
        builder.Property(t => t.Notes).HasMaxLength(1000);
        builder.HasIndex(t => new { t.BranchId, t.TransactionDate });
        builder.HasIndex(t => new { t.MachineId, t.TransactionDate });
        builder.HasOne(t => t.Machine).WithMany(m => m.Transactions).HasForeignKey(t => t.MachineId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(t => t.Branch).WithMany(b => b.Transactions).HasForeignKey(t => t.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(t => t.ProcessedByUser).WithMany().HasForeignKey(t => t.ProcessedByUserId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(t => t.Promotion).WithMany().HasForeignKey(t => t.PromotionId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Username).IsRequired().HasMaxLength(50);
        builder.HasIndex(u => u.Username).IsUnique();
        builder.Property(u => u.Email).IsRequired().HasMaxLength(200);
        builder.HasIndex(u => u.Email).IsUnique();
        builder.Property(u => u.PasswordHash).IsRequired().HasMaxLength(200);
        builder.Property(u => u.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(u => u.LastName).IsRequired().HasMaxLength(100);
        builder.Property(u => u.Phone).HasMaxLength(20);
        builder.Property(u => u.RefreshToken).HasMaxLength(500);
        builder.Ignore(u => u.FullName);
        builder.HasQueryFilter(u => !u.IsDeleted);
    }
}

public class UserBranchConfiguration : IEntityTypeConfiguration<UserBranch>
{
    public void Configure(EntityTypeBuilder<UserBranch> builder)
    {
        builder.ToTable("UserBranches");
        builder.HasKey(ub => new { ub.UserId, ub.BranchId });
        builder.HasOne(ub => ub.User).WithMany(u => u.UserBranches).HasForeignKey(ub => ub.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(ub => ub.Branch).WithMany(b => b.UserBranches).HasForeignKey(ub => ub.BranchId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class UserPermissionConfiguration : IEntityTypeConfiguration<UserPermission>
{
    public void Configure(EntityTypeBuilder<UserPermission> builder)
    {
        builder.ToTable("UserPermissions");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Module).IsRequired().HasMaxLength(50);
        builder.HasIndex(p => new { p.UserId, p.Module }).IsUnique();
        builder.HasOne(p => p.User).WithMany(u => u.Permissions).HasForeignKey(p => p.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class MaintenanceRecordConfiguration : IEntityTypeConfiguration<MaintenanceRecord>
{
    public void Configure(EntityTypeBuilder<MaintenanceRecord> builder)
    {
        builder.ToTable("MaintenanceRecords");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Title).IsRequired().HasMaxLength(200);
        builder.Property(m => m.Description).HasMaxLength(2000);
        builder.Property(m => m.Cost).HasPrecision(10, 2);
        builder.Property(m => m.PartsReplaced).HasMaxLength(1000);
        builder.Property(m => m.Notes).HasMaxLength(2000);
        builder.Property(m => m.TechnicianNotes).HasMaxLength(2000);
        builder.HasOne(m => m.Machine).WithMany(mac => mac.MaintenanceRecords).HasForeignKey(m => m.MachineId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(m => m.Technician).WithMany().HasForeignKey(m => m.TechnicianId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(m => m.Branch).WithMany().HasForeignKey(m => m.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class MachineAlertConfiguration : IEntityTypeConfiguration<MachineAlert>
{
    public void Configure(EntityTypeBuilder<MachineAlert> builder)
    {
        builder.ToTable("MachineAlerts");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Title).IsRequired().HasMaxLength(200);
        builder.Property(a => a.Message).IsRequired().HasMaxLength(1000);
        builder.Property(a => a.ResolvedBy).HasMaxLength(100);
        builder.HasIndex(a => new { a.BranchId, a.IsResolved });
        builder.HasOne(a => a.Machine).WithMany(m => m.Alerts).HasForeignKey(a => a.MachineId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.Branch).WithMany().HasForeignKey(a => a.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class PromotionConfiguration : IEntityTypeConfiguration<Promotion>
{
    public void Configure(EntityTypeBuilder<Promotion> builder)
    {
        builder.ToTable("Promotions");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Description).HasMaxLength(500);
        builder.Property(p => p.DiscountPercentage).HasPrecision(5, 2);
        builder.Property(p => p.DiscountFixedAmount).HasPrecision(10, 2);
        builder.Property(p => p.ApplicableDays).HasMaxLength(200);
        builder.HasOne(p => p.Branch).WithMany().HasForeignKey(p => p.BranchId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class SystemSettingConfiguration : IEntityTypeConfiguration<SystemSetting>
{
    public void Configure(EntityTypeBuilder<SystemSetting> builder)
    {
        builder.ToTable("SystemSettings");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Key).IsRequired().HasMaxLength(100);
        builder.Property(s => s.Value).IsRequired().HasMaxLength(2000);
        builder.Property(s => s.Description).HasMaxLength(500);
        builder.Property(s => s.Category).IsRequired().HasMaxLength(50);
        builder.Property(s => s.DataType).HasMaxLength(20);
        builder.HasIndex(s => new { s.Key, s.BranchId }).IsUnique();
        builder.HasOne(s => s.Branch).WithMany().HasForeignKey(s => s.BranchId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.EntityName).IsRequired().HasMaxLength(100);
        builder.Property(a => a.Action).IsRequired().HasMaxLength(50);
        builder.Property(a => a.UserName).HasMaxLength(100);
        builder.Property(a => a.IpAddress).HasMaxLength(50);
        builder.HasIndex(a => new { a.EntityName, a.EntityId });
    }
}

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Brand).IsRequired().HasMaxLength(100);
        builder.Property(p => p.Presentation).IsRequired().HasMaxLength(50);
        builder.Property(p => p.Sku).HasMaxLength(50);
        builder.Property(p => p.Barcode).HasMaxLength(50);
        builder.Property(p => p.ImageUrl).HasMaxLength(500);
        builder.Property(p => p.Notes).HasMaxLength(1000);
        builder.Property(p => p.PurchasePrice).HasPrecision(10, 2);
        builder.Property(p => p.SalePrice).HasPrecision(10, 2);
        builder.Ignore(p => p.IsLowStock);
        builder.HasIndex(p => new { p.Sku, p.BranchId }).IsUnique().HasFilter("[Sku] IS NOT NULL");
        builder.HasOne(p => p.Branch).WithMany().HasForeignKey(p => p.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}

public class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("StockMovements");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Reason).HasMaxLength(500);
        builder.HasOne(m => m.Product).WithMany(p => p.StockMovements).HasForeignKey(m => m.ProductId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(m => m.User).WithMany().HasForeignKey(m => m.UserId).OnDelete(DeleteBehavior.SetNull);
        builder.HasIndex(m => m.ProductId);
    }
}

public class LaundryOrderConfiguration : IEntityTypeConfiguration<LaundryOrder>
{
    public void Configure(EntityTypeBuilder<LaundryOrder> builder)
    {
        builder.ToTable("LaundryOrders");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.OrderNumber).IsRequired().HasMaxLength(50);
        builder.Property(o => o.CustomerName).IsRequired().HasMaxLength(200);
        builder.Property(o => o.CustomerPhone).HasMaxLength(30);
        builder.Property(o => o.ComforterSize).HasMaxLength(50);
        builder.Property(o => o.Notes).HasMaxLength(1000);
        builder.Property(o => o.WeightKg).HasPrecision(10, 2);
        builder.Property(o => o.PricePerKg).HasPrecision(10, 2);
        builder.Property(o => o.PricePerComforter).HasPrecision(10, 2);
        builder.Property(o => o.TotalPrice).HasPrecision(10, 2);
        builder.HasIndex(o => o.OrderNumber).IsUnique();
        builder.HasOne(o => o.Branch).WithMany().HasForeignKey(o => o.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(o => o.ProcessedByUser).WithMany().HasForeignKey(o => o.ProcessedByUserId).OnDelete(DeleteBehavior.SetNull);
        builder.HasQueryFilter(o => !o.IsDeleted);
    }
}

