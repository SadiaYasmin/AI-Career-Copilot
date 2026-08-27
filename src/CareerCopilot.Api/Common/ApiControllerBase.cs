using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CareerCopilot.API.Common;

public abstract class ApiControllerBase : ControllerBase
{
    private ISender? _mediator;

    protected ISender Mediator => _mediator ??= HttpContext.RequestServices.GetRequiredService<ISender>();

    protected async Task<ActionResult<T>> SendAsync<T>(IRequest<T> request, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(request, cancellationToken));

    protected ActionResult<object> CreatedWithData<T>(string routeName, object? routeValues, T data)
    {
        var payload = new SuccessResponse<T>(data);
        return CreatedAtRoute(routeName, routeValues, payload);
    }
}

public sealed record SuccessResponse<T>(bool Success, T Data)
{
    public SuccessResponse(T data)
        : this(true, data)
    {
    }
}