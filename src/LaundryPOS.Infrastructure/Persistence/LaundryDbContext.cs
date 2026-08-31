using LaundryPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LaundryPOS.Infrastructure.Persistence;

public class LaundryDbContext : DbContext
{
    public LaundryDbContext(DbContextOptions<LaundryDbContext> options) : base(options) { }

    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<Machine> Machines => Set<Machine>();
    public DbSet<IoTController> IoTControllers => Set<IoTController>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserBranch> UserBranches => Set<UserBranch>();
    public DbSet<UserPermission> UserPermissions => Set<UserPermission>();
    public DbSet<MaintenanceRecord> MaintenanceRecords => Set<MaintenanceRecord>();
    public DbSet<MachineAlert> MachineAlerts => Set<MachineAlert>();
    public DbSet<Promotion> Promotions => Set<Promotion>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<LaundryOrder> LaundryOrders => Set<LaundryOrder>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LaundryDbContext).Assembly);
    }
}
