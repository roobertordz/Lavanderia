using FluentValidation;
using LaundryPOS.Application.Common;
using LaundryPOS.Application.Common.Interfaces;
using LaundryPOS.Application.Common.Models;
using LaundryPOS.Domain.Entities;
using LaundryPOS.Domain.Interfaces.Repositories;
using LaundryPOS.Domain.Interfaces.Services;
using MediatR;

namespace LaundryPOS.Application.Features.Users.Commands;

// ─── Login ───
public record LoginCommand : ICommand<AuthResponseDto>
{
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

public class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
    }
}

public class LoginHandler : IRequestHandler<LoginCommand, Result<AuthResponseDto>>
{
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokenService;

    public LoginHandler(IUnitOfWork uow, IPasswordHasher hasher, ITokenService tokenService)
    {
        _uow = uow;
        _hasher = hasher;
        _tokenService = tokenService;
    }

    public async Task<Result<AuthResponseDto>> Handle(LoginCommand request, CancellationToken ct)
    {
        var user = await _uow.Users.GetByUsernameAsync(request.Username, ct);
        if (user == null || !user.IsActive)
            return Result.Failure<AuthResponseDto>("Invalid credentials.", "INVALID_CREDENTIALS");

        if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.UtcNow)
            return Result.Failure<AuthResponseDto>("Account is locked. Try again later.", "ACCOUNT_LOCKED");

        if (!_hasher.Verify(request.Password, user.PasswordHash))
        {
            user.FailedLoginAttempts++;
            if (user.FailedLoginAttempts >= 5)
                user.LockoutEnd = DateTime.UtcNow.AddMinutes(15);

            await _uow.SaveChangesAsync(ct);
            return Result.Failure<AuthResponseDto>("Invalid credentials.", "INVALID_CREDENTIALS");
        }

        // Reset failed attempts
        user.FailedLoginAttempts = 0;
        user.LockoutEnd = null;
        user.LastLoginAt = DateTime.UtcNow;

        // Generate tokens
        var branchIds = user.UserBranches.Select(ub => ub.BranchId).ToList();
        var accessToken = _tokenService.GenerateAccessToken(user.Id, user.Username, user.Role.ToString(), branchIds);
        var refreshToken = _tokenService.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

        await _uow.SaveChangesAsync(ct);

        return Result.Success(new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            User = new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Phone = user.Phone,
                Role = user.Role,
                IsActive = user.IsActive,
                LastLoginAt = user.LastLoginAt,
                BranchIds = branchIds.AsReadOnly()
            }
        });
    }
}

// ─── Create User ───
public record CreateUserCommand : ICommand<UserDto>
{
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public Domain.Enums.UserRole Role { get; init; }
    public List<Guid> BranchIds { get; init; } = new();
}

public class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8)
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches(@"\d").WithMessage("Password must contain at least one digit.")
            .Matches(@"[^\da-zA-Z]").WithMessage("Password must contain at least one special character.");
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.BranchIds).NotEmpty().WithMessage("At least one branch must be assigned.");
    }
}

public class CreateUserHandler : IRequestHandler<CreateUserCommand, Result<UserDto>>
{
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _hasher;

    public CreateUserHandler(IUnitOfWork uow, IPasswordHasher hasher)
    {
        _uow = uow;
        _hasher = hasher;
    }

    public async Task<Result<UserDto>> Handle(CreateUserCommand request, CancellationToken ct)
    {
        var existingUser = await _uow.Users.GetByUsernameAsync(request.Username, ct);
        if (existingUser != null)
            return Result.Failure<UserDto>("Username already exists.", "DUPLICATE_USERNAME");

        var existingEmail = await _uow.Users.GetByEmailAsync(request.Email, ct);
        if (existingEmail != null)
            return Result.Failure<UserDto>("Email already exists.", "DUPLICATE_EMAIL");

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = _hasher.Hash(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            Phone = request.Phone,
            Role = request.Role
        };

        await _uow.Users.AddAsync(user, ct);

        // Assign branches
        foreach (var branchId in request.BranchIds)
        {
            user.UserBranches.Add(new UserBranch
            {
                UserId = user.Id,
                BranchId = branchId,
                IsPrimary = branchId == request.BranchIds.First()
            });
        }

        await _uow.SaveChangesAsync(ct);

        return Result.Success(new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Phone = user.Phone,
            Role = user.Role,
            IsActive = user.IsActive,
            BranchIds = request.BranchIds.AsReadOnly()
        });
    }
}

// ─── Refresh Token ───
public record RefreshTokenCommand : ICommand<AuthResponseDto>
{
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
}

public class RefreshTokenHandler : IRequestHandler<RefreshTokenCommand, Result<AuthResponseDto>>
{
    private readonly IUnitOfWork _uow;
    private readonly ITokenService _tokenService;

    public RefreshTokenHandler(IUnitOfWork uow, ITokenService tokenService)
    {
        _uow = uow;
        _tokenService = tokenService;
    }

    public async Task<Result<AuthResponseDto>> Handle(RefreshTokenCommand request, CancellationToken ct)
    {
        var principal = _tokenService.ValidateAccessToken(request.AccessToken);
        if (principal == null)
            return Result.Failure<AuthResponseDto>("Invalid access token.", "INVALID_TOKEN");

        var user = await _uow.Users.GetByRefreshTokenAsync(request.RefreshToken, ct);
        if (user == null || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            return Result.Failure<AuthResponseDto>("Invalid or expired refresh token.", "INVALID_REFRESH_TOKEN");

        var branchIds = user.UserBranches.Select(ub => ub.BranchId).ToList();
        var newAccessToken = _tokenService.GenerateAccessToken(user.Id, user.Username, user.Role.ToString(), branchIds);
        var newRefreshToken = _tokenService.GenerateRefreshToken();

        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        await _uow.SaveChangesAsync(ct);

        return Result.Success(new AuthResponseDto
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            User = new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = user.Role,
                IsActive = user.IsActive,
                BranchIds = branchIds.AsReadOnly()
            }
        });
    }
}
