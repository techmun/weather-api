using Microsoft.AspNetCore.Authorization;

namespace WeatherAPI.Middleware;

public sealed class ProtectedEndpointAuditMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ILogger<ProtectedEndpointAuditMiddleware> logger)
    {
        await next(context);

        var endpoint = context.GetEndpoint();
        if (endpoint is null)
        {
            return;
        }

        var isProtected = endpoint.Metadata.GetMetadata<IAuthorizeData>() is not null;
        if (!isProtected)
        {
            return;
        }

        logger.LogInformation(
            "Protected endpoint call Path={Path} Method={Method} Status={StatusCode} CorrelationId={CorrelationId}",
            context.Request.Path,
            context.Request.Method,
            context.Response.StatusCode,
            context.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString());
    }
}
