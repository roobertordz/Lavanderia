using LaundryPOS.Domain.Enums;

namespace LaundryPOS.Domain.Entities;

public class User : AuditableEntity
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public UserRole Role { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public int FailedLoginAttempts { get; set; }
    public DateTime? LockoutEnd { get; set; }

    public string FullName => $"{FirstName} {LastName}";

    // Navigation properties
    public ICollection<UserBranch> UserBranches { get; set; } = new List<UserBranch>();
    public ICollection<UserPermission> Permissions { get; set; } = new List<UserPermission>();
}

public class UserBranch
{
    public Guid UserId { get; set; }
    public Guid BranchId { get; set; }
    public bool IsPrimary { get; set; }
    
    public User User { get; set; } = null!;
    public Branch Branch { get; set; } = null!;
}

public class UserPermission : BaseEntity
{
    public Guid UserId { get; set; }
    public string Module { get; set; } = string.Empty;
    public bool CanRead { get; set; }
    public bool CanWrite { get; set; }
    public bool CanDelete { get; set; }
    public bool CanExport { get; set; }

    public User User { get; set; } = null!;
}
