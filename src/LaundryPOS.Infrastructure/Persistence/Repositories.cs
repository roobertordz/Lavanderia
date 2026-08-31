using System.Linq.Expressions;
using LaundryPOS.Domain.Entities;
using LaundryPOS.Domain.Enums;
using LaundryPOS.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LaundryPOS.Infrastructure.Persistence;

public class Repository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly LaundryDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(LaundryDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _dbSet.FindAsync(new object[] { id }, ct);

    public virtual async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default)
        => await _dbSet.ToListAsync(ct);

    public virtual async Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => await _dbSet.Where(predicate).ToListAsync(ct);

    public virtual async Task<T> AddAsync(T entity, CancellationToken ct = default)
    {
        await _dbSet.AddAsync(entity, ct);
        return entity;
    }

    public virtual Task UpdateAsync(T entity, CancellationToken ct = default)
    {
        _context.Entry(entity).State = EntityState.Modified;
        return Task.CompletedTask;
    }

    public virtual Task DeleteAsync(T entity, CancellationToken ct = default)
    {
        _dbSet.Remove(entity);
        return Task.CompletedTask;
    }

    public virtual async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default)
        => predicate == null ? await _dbSet.CountAsync(ct) : await _dbSet.CountAsync(predicate, ct);

    public virtual async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => await _dbSet.AnyAsync(predicate, ct);
}

public class MachineRepository : Repository<Machine>, IMachineRepository
{
    public MachineRepository(LaundryDbContext context) : base(context) { }

    public async Task<IReadOnlyList<Machine>> GetByBranchAsync(Guid branchId, CancellationToken ct = default)
        => await _dbSet.Where(m => m.BranchId == branchId && m.IsActive).OrderBy(m => m.Number).ToListAsync(ct);

    public async Task<Machine?> GetByNumberAndBranchAsync(int number, Guid branchId, CancellationToken ct = default)
        => await _dbSet.FirstOrDefaultAsync(m => m.Number == number && m.BranchId == branchId, ct);

    public async Task<IReadOnlyList<Machine>> GetAvailableByBranchAsync(Guid branchId, CancellationToken ct = default)
        => await _dbSet.Where(m => m.BranchId == branchId && m.Status == MachineStatus.Available && m.IsActive)
            .OrderBy(m => m.Number).ToListAsync(ct);

    public async Task<Machine?> GetWithControllerAsync(Guid machineId, CancellationToken ct = default)
        => await _dbSet.Include(m => m.IoTController).FirstOrDefaultAsync(m => m.Id == machineId, ct);
}

public class TransactionRepository : Repository<Transaction>, ITransactionRepository
{
    public TransactionRepository(LaundryDbContext context) : base(context) { }

    public async Task<IReadOnlyList<Transaction>> GetByBranchAsync(Guid branchId, DateTime from, DateTime to, CancellationToken ct = default)
        => await _dbSet.Where(t => t.BranchId == branchId && t.TransactionDate >= from && t.TransactionDate < to)
            .OrderByDescending(t => t.TransactionDate).ToListAsync(ct);

    public async Task<IReadOnlyList<Transaction>> GetByMachineAsync(Guid machineId, DateTime from, DateTime to, CancellationToken ct = default)
        => await _dbSet.Where(t => t.MachineId == machineId && t.TransactionDate >= from && t.TransactionDate < to)
            .OrderByDescending(t => t.TransactionDate).ToListAsync(ct);

    public async Task<decimal> GetTotalRevenueAsync(Guid branchId, DateTime from, DateTime to, CancellationToken ct = default)
        => await _dbSet.Where(t => t.BranchId == branchId && t.TransactionDate >= from && t.TransactionDate < to
            && (t.Status == TransactionStatus.Completed || t.Status == TransactionStatus.InProgress))
            .SumAsync(t => t.TotalAmount, ct);

    public async Task<string> GenerateTransactionNumberAsync(Guid branchId, CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.Date;
        var count = await _dbSet.CountAsync(t => t.BranchId == branchId && t.TransactionDate >= today, ct);
        return $"TX-{today:yyyyMMdd}-{(count + 1):D5}";
    }
}

public class BranchRepository : Repository<Branch>, IBranchRepository
{
    public BranchRepository(LaundryDbContext context) : base(context) { }

    public async Task<Branch?> GetWithMachinesAsync(Guid branchId, CancellationToken ct = default)
        => await _dbSet.Include(b => b.Machines).FirstOrDefaultAsync(b => b.Id == branchId, ct);

    public async Task<Branch?> GetByCodeAsync(string code, CancellationToken ct = default)
        => await _dbSet.FirstOrDefaultAsync(b => b.Code == code, ct);
}

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(LaundryDbContext context) : base(context) { }

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default)
        => await _dbSet.Include(u => u.UserBranches).Include(u => u.Permissions)
            .FirstOrDefaultAsync(u => u.Username == username, ct);

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
        => await _dbSet.FirstOrDefaultAsync(u => u.Email == email, ct);

    public async Task<IReadOnlyList<User>> GetByBranchAsync(Guid branchId, CancellationToken ct = default)
        => await _dbSet.Include(u => u.UserBranches)
            .Where(u => u.UserBranches.Any(ub => ub.BranchId == branchId)).ToListAsync(ct);

    public async Task<User?> GetByRefreshTokenAsync(string refreshToken, CancellationToken ct = default)
        => await _dbSet.Include(u => u.UserBranches)
            .FirstOrDefaultAsync(u => u.RefreshToken == refreshToken, ct);
}

public class MaintenanceRepository : Repository<MaintenanceRecord>, IMaintenanceRepository
{
    public MaintenanceRepository(LaundryDbContext context) : base(context) { }

    public async Task<IReadOnlyList<MaintenanceRecord>> GetByMachineAsync(Guid machineId, CancellationToken ct = default)
        => await _dbSet.Where(m => m.MachineId == machineId).OrderByDescending(m => m.ScheduledDate).ToListAsync(ct);

    public async Task<IReadOnlyList<MaintenanceRecord>> GetScheduledAsync(Guid branchId, CancellationToken ct = default)
        => await _dbSet.Where(m => m.BranchId == branchId && m.Status == MaintenanceStatus.Scheduled)
            .OrderBy(m => m.ScheduledDate).ToListAsync(ct);

    public async Task<IReadOnlyList<MaintenanceRecord>> GetByBranchAsync(Guid branchId, DateTime from, DateTime to, CancellationToken ct = default)
        => await _dbSet.Where(m => m.BranchId == branchId && m.ScheduledDate >= from && m.ScheduledDate < to)
            .OrderByDescending(m => m.ScheduledDate).ToListAsync(ct);
}

public class AlertRepository : Repository<MachineAlert>, IAlertRepository
{
    public AlertRepository(LaundryDbContext context) : base(context) { }

    public async Task<IReadOnlyList<MachineAlert>> GetUnresolvedByBranchAsync(Guid branchId, CancellationToken ct = default)
        => await _dbSet.Where(a => a.BranchId == branchId && !a.IsResolved).OrderByDescending(a => a.CreatedAt).ToListAsync(ct);

    public async Task<IReadOnlyList<MachineAlert>> GetByMachineAsync(Guid machineId, CancellationToken ct = default)
        => await _dbSet.Where(a => a.MachineId == machineId).OrderByDescending(a => a.CreatedAt).ToListAsync(ct);
}

public class IoTControllerRepository : Repository<IoTController>, IIoTControllerRepository
{
    public IoTControllerRepository(LaundryDbContext context) : base(context) { }

    public async Task<IReadOnlyList<IoTController>> GetByBranchAsync(Guid branchId, CancellationToken ct = default)
        => await _dbSet.Where(c => c.BranchId == branchId).ToListAsync(ct);

    public async Task<IoTController?> GetByMachineAsync(Guid machineId, CancellationToken ct = default)
        => await _dbSet.Include(c => c.Machines).FirstOrDefaultAsync(c => c.Machines.Any(m => m.Id == machineId), ct);
}

public class SystemSettingRepository : Repository<SystemSetting>, ISystemSettingRepository
{
    public SystemSettingRepository(LaundryDbContext context) : base(context) { }

    public async Task<SystemSetting?> GetByKeyAsync(string key, Guid? branchId = null, CancellationToken ct = default)
        => await _dbSet.FirstOrDefaultAsync(s => s.Key == key && s.BranchId == branchId, ct);

    public async Task<IReadOnlyList<SystemSetting>> GetByCategoryAsync(string category, Guid? branchId = null, CancellationToken ct = default)
        => await _dbSet.Where(s => s.Category == category && s.BranchId == branchId).ToListAsync(ct);
}

public class PromotionRepository : Repository<Promotion>, IPromotionRepository
{
    public PromotionRepository(LaundryDbContext context) : base(context) { }

    public async Task<IReadOnlyList<Promotion>> GetActiveAsync(Guid? branchId = null, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        return await _dbSet.Where(p => p.IsActive && p.StartDate <= now && p.EndDate >= now
            && (p.BranchId == null || p.BranchId == branchId)).ToListAsync(ct);
    }
}

public class AuditLogRepository : Repository<AuditLog>, IAuditLogRepository
{
    public AuditLogRepository(LaundryDbContext context) : base(context) { }

    public async Task<IReadOnlyList<AuditLog>> GetByEntityAsync(string entityName, Guid entityId, CancellationToken ct = default)
        => await _dbSet.Where(a => a.EntityName == entityName && a.EntityId == entityId)
            .OrderByDescending(a => a.CreatedAt).ToListAsync(ct);
}

public class ProductRepository : Repository<Product>, IProductRepository
{
    public ProductRepository(LaundryDbContext context) : base(context) { }

    public async Task<IReadOnlyList<Product>> GetByBranchAsync(Guid branchId, CancellationToken ct = default)
        => await _dbSet.Where(p => p.BranchId == branchId).OrderBy(p => p.Category).ThenBy(p => p.Name).ToListAsync(ct);

    public async Task<IReadOnlyList<Product>> GetLowStockByBranchAsync(Guid branchId, CancellationToken ct = default)
        => await _dbSet.Where(p => p.BranchId == branchId && p.StockQuantity <= p.MinStockThreshold)
            .OrderBy(p => p.Name).ToListAsync(ct);

    public async Task<Product?> GetBySkuAsync(string sku, Guid branchId, CancellationToken ct = default)
        => await _dbSet.FirstOrDefaultAsync(p => p.Sku == sku && p.BranchId == branchId, ct);
}

public class StockMovementRepository : Repository<StockMovement>, IStockMovementRepository
{
    public StockMovementRepository(LaundryDbContext context) : base(context) { }

    public async Task<IReadOnlyList<StockMovement>> GetByProductAsync(Guid productId, CancellationToken ct = default)
        => await _dbSet.Include(m => m.User).Where(m => m.ProductId == productId).OrderByDescending(m => m.CreatedAt).ToListAsync(ct);
}

public class LaundryOrderRepository : Repository<LaundryOrder>, ILaundryOrderRepository
{
    public LaundryOrderRepository(LaundryDbContext context) : base(context) { }

    public async Task<IReadOnlyList<LaundryOrder>> GetByBranchAsync(Guid branchId, LaundryOrderStatus? status = null, CancellationToken ct = default)
    {
        var query = _dbSet.Where(o => o.BranchId == branchId);
        if (status.HasValue)
            query = query.Where(o => o.Status == status.Value);

        return await query.OrderByDescending(o => o.ReceivedAt).ToListAsync(ct);
    }

    public async Task<string> GenerateOrderNumberAsync(Guid branchId, CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.Date;
        var count = await _dbSet.CountAsync(o => o.BranchId == branchId && o.ReceivedAt >= today, ct);
        return $"ENC-{today:yyyyMMdd}-{(count + 1):D5}";
    }
}

