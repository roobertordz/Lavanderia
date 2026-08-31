using LaundryPOS.Application.Common;
using LaundryPOS.Application.Common.Interfaces;
using LaundryPOS.Application.Common.Models;
using LaundryPOS.Application.Features.LaundryOrders.Commands;
using LaundryPOS.Domain.Enums;
using LaundryPOS.Domain.Interfaces.Repositories;
using MediatR;

namespace LaundryPOS.Application.Features.LaundryOrders.Queries;

// ─── Get Laundry Orders by Branch (opcionalmente filtrado por estado) ───
public record GetLaundryOrdersByBranchQuery(Guid BranchId, LaundryOrderStatus? Status = null) : IQuery<IReadOnlyList<LaundryOrderDto>>;

public class GetLaundryOrdersByBranchHandler : IRequestHandler<GetLaundryOrdersByBranchQuery, Result<IReadOnlyList<LaundryOrderDto>>>
{
    private readonly IUnitOfWork _uow;

    public GetLaundryOrdersByBranchHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Result<IReadOnlyList<LaundryOrderDto>>> Handle(GetLaundryOrdersByBranchQuery request, CancellationToken ct)
    {
        var orders = await _uow.LaundryOrders.GetByBranchAsync(request.BranchId, request.Status, ct);
        var branch = await _uow.Branches.GetByIdAsync(request.BranchId, ct);
        var branchName = branch?.Name ?? string.Empty;

        var list = new List<LaundryOrderDto>();
        foreach (var o in orders)
        {
            var user = o.ProcessedByUserId.HasValue ? await _uow.Users.GetByIdAsync(o.ProcessedByUserId.Value, ct) : null;
            list.Add(LaundryOrderMapper.ToDto(o, branchName, user != null ? $"{user.FirstName} {user.LastName}" : null));
        }

        return Result.Success<IReadOnlyList<LaundryOrderDto>>(list);
    }
}

// ─── Get Laundry Order By Id ───
public record GetLaundryOrderByIdQuery(Guid Id) : IQuery<LaundryOrderDto>;

public class GetLaundryOrderByIdHandler : IRequestHandler<GetLaundryOrderByIdQuery, Result<LaundryOrderDto>>
{
    private readonly IUnitOfWork _uow;

    public GetLaundryOrderByIdHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Result<LaundryOrderDto>> Handle(GetLaundryOrderByIdQuery request, CancellationToken ct)
    {
        var order = await _uow.LaundryOrders.GetByIdAsync(request.Id, ct);
        if (order == null)
            return Result.Failure<LaundryOrderDto>("Encargo no encontrado.", "NOT_FOUND");

        var branch = await _uow.Branches.GetByIdAsync(order.BranchId, ct);
        var user = order.ProcessedByUserId.HasValue ? await _uow.Users.GetByIdAsync(order.ProcessedByUserId.Value, ct) : null;

        return Result.Success(LaundryOrderMapper.ToDto(order, branch?.Name ?? string.Empty,
            user != null ? $"{user.FirstName} {user.LastName}" : null));
    }
}
