using System.Linq.Expressions;
using LaundryPOS.Domain.Entities;

namespace LaundryPOS.Domain.Interfaces.Repositories;

public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    Task<T> AddAsync(T entity, CancellationToken ct = default);
    Task UpdateAsync(T entity, CancellationToken ct = default);
    Task DeleteAsync(T entity, CancellationToken ct = default);
    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default);
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
}

public interface IMachineRepository : IRepository<Machine>
{
    Task<IReadOnlyList<Machine>> GetByBranchAsync(Guid branchId, CancellationToken ct = default);
    Task<Machine?> GetByNumberAndBranchAsync(int number, Guid branchId, CancellationToken ct = default);
    Task<IReadOnlyList<Machine>> GetAvailableByBranchAsync(Guid branchId, CancellationToken ct = default);
    Task<Machine?> GetWithControllerAsync(Guid machineId, CancellationToken ct = default);
}

public interface ITransactionRepository : IRepository<Transaction>
{
    Task<IReadOnlyList<Transaction>> GetByBranchAsync(Guid branchId, DateTime from, DateTime to, CancellationToken ct = default);
    Task<IReadOnlyList<Transaction>> GetByMachineAsync(Guid machineId, DateTime from, DateTime to, CancellationToken ct = default);
    Task<decimal> GetTotalRevenueAsync(Guid branchId, DateTime from, DateTime to, CancellationToken ct = default);
    Task<string> GenerateTransactionNumberAsync(Guid branchId, CancellationToken ct = default);
}

public interface IBranchRepository : IRepository<Branch>
{
    Task<Branch?> GetWithMachinesAsync(Guid branchId, CancellationToken ct = default);
    Task<Branch?> GetByCodeAsync(string code, CancellationToken ct = default);
}

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<IReadOnlyList<User>> GetByBranchAsync(Guid branchId, CancellationToken ct = default);
    Task<User?> GetByRefreshTokenAsync(string refreshToken, CancellationToken ct = default);
}

public interface IMaintenanceRepository : IRepository<MaintenanceRecord>
{
    Task<IReadOnlyList<MaintenanceRecord>> GetByMachineAsync(Guid machineId, CancellationToken ct = default);
    Task<IReadOnlyList<MaintenanceRecord>> GetScheduledAsync(Guid branchId, CancellationToken ct = default);
    Task<IReadOnlyList<MaintenanceRecord>> GetByBranchAsync(Guid branchId, DateTime from, DateTime to, CancellationToken ct = default);
}

public interface IAlertRepository : IRepository<MachineAlert>
{
    Task<IReadOnlyList<MachineAlert>> GetUnresolvedByBranchAsync(Guid branchId, CancellationToken ct = default);
    Task<IReadOnlyList<MachineAlert>> GetByMachineAsync(Guid machineId, CancellationToken ct = default);
}

public interface IIoTControllerRepository : IRepository<IoTController>
{
    Task<IReadOnlyList<IoTController>> GetByBranchAsync(Guid branchId, CancellationToken ct = default);
    Task<IoTController?> GetByMachineAsync(Guid machineId, CancellationToken ct = default);
}

public interface ISystemSettingRepository : IRepository<SystemSetting>
{
    Task<SystemSetting?> GetByKeyAsync(string key, Guid? branchId = null, CancellationToken ct = default);
    Task<IReadOnlyList<SystemSetting>> GetByCategoryAsync(string category, Guid? branchId = null, CancellationToken ct = default);
}

public interface IPromotionRepository : IRepository<Promotion>
{
    Task<IReadOnlyList<Promotion>> GetActiveAsync(Guid? branchId = null, CancellationToken ct = default);
}

public interface IAuditLogRepository : IRepository<AuditLog>
{
    Task<IReadOnlyList<AuditLog>> GetByEntityAsync(string entityName, Guid entityId, CancellationToken ct = default);
}

public interface IProductRepository : IRepository<Product>
{
    Task<IReadOnlyList<Product>> GetByBranchAsync(Guid branchId, CancellationToken ct = default);
    Task<IReadOnlyList<Product>> GetLowStockByBranchAsync(Guid branchId, CancellationToken ct = default);
    Task<Product?> GetBySkuAsync(string sku, Guid branchId, CancellationToken ct = default);
}

public interface IStockMovementRepository : IRepository<StockMovement>
{
    Task<IReadOnlyList<StockMovement>> GetByProductAsync(Guid productId, CancellationToken ct = default);
}

public interface IUnitOfWork : IDisposable
{
    IMachineRepository Machines { get; }
    ITransactionRepository Transactions { get; }
    IBranchRepository Branches { get; }
    IUserRepository Users { get; }
    IMaintenanceRepository Maintenance { get; }
    IAlertRepository Alerts { get; }
    IIoTControllerRepository IoTControllers { get; }
    ISystemSettingRepository SystemSettings { get; }
    IPromotionRepository Promotions { get; }
    IAuditLogRepository AuditLogs { get; }
    IProductRepository Products { get; }
    IStockMovementRepository StockMovements { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task BeginTransactionAsync(CancellationToken ct = default);
    Task CommitTransactionAsync(CancellationToken ct = default);
    Task RollbackTransactionAsync(CancellationToken ct = default);
}
