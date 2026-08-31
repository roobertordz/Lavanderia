using LaundryPOS.Application.Common;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LaundryPOS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    private ISender? _mediator;
    protected ISender Mediator => _mediator ??= HttpContext.RequestServices.GetRequiredService<ISender>();

    protected IActionResult HandleResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
            return Ok(new ApiResponse<T> { Success = true, Data = result.Value });

        return result.ErrorCode switch
        {
            "NOT_FOUND" => NotFound(new ApiResponse { Success = false, Error = result.Error, ErrorCode = result.ErrorCode }),
            "UNAUTHORIZED" => Unauthorized(new ApiResponse { Success = false, Error = result.Error, ErrorCode = result.ErrorCode }),
            _ => BadRequest(new ApiResponse { Success = false, Error = result.Error, ErrorCode = result.ErrorCode })
        };
    }

    protected IActionResult HandleResult(Result result)
    {
        if (result.IsSuccess)
            return Ok(new ApiResponse { Success = true });

        return result.ErrorCode switch
        {
            "NOT_FOUND" => NotFound(new ApiResponse { Success = false, Error = result.Error, ErrorCode = result.ErrorCode }),
            _ => BadRequest(new ApiResponse { Success = false, Error = result.Error, ErrorCode = result.ErrorCode })
        };
    }
}

public record ApiResponse
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public string? ErrorCode { get; init; }
}

public record ApiResponse<T> : ApiResponse
{
    public T? Data { get; init; }
}
