using LaundryPOS.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore.Storage;

namespace LaundryPOS.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly LaundryDbContext _context;
    private IDbContextTransaction? _transaction;

    public IMachineRepository Machines { get; }
    public ITransactionRepository Transactions { get; }
    public IBranchRepository Branches { get; }
    public IUserRepository Users { get; }
    public IMaintenanceRepository Maintenance { get; }
    public IAlertRepository Alerts { get; }
    public IIoTControllerRepository IoTControllers { get; }
    public ISystemSettingRepository SystemSettings { get; }
    public IPromotionRepository Promotions { get; }
    public IAuditLogRepository AuditLogs { get; }
    public IProductRepository Products { get; }
    public IStockMovementRepository StockMovements { get; }

    public UnitOfWork(LaundryDbContext context)
    {
        _context = context;
        Machines = new MachineRepository(context);
        Transactions = new TransactionRepository(context);
        Branches = new BranchRepository(context);
        Users = new UserRepository(context);
        Maintenance = new MaintenanceRepository(context);
        Alerts = new AlertRepository(context);
        IoTControllers = new IoTControllerRepository(context);
        SystemSettings = new SystemSettingRepository(context);
        Promotions = new PromotionRepository(context);
        AuditLogs = new AuditLogRepository(context);
        Products = new ProductRepository(context);
        StockMovements = new StockMovementRepository(context);
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct);

    public async Task BeginTransactionAsync(CancellationToken ct = default)
    {
        _transaction = await _context.Database.BeginTransactionAsync(ct);
    }

    public async Task CommitTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync(ct);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync(ct);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}
