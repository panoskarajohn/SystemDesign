using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Shared.Web.Middleware;

public class ErrorMiddleware : IMiddleware {
    private readonly ILogger<ErrorMiddleware> logger;
    public ErrorMiddleware(ILogger<ErrorMiddleware> logger) {
        this.logger = logger;
    }
    // TODO: Enhance error handling (custom exceptions, error responses, etc.)
    public async Task InvokeAsync(HttpContext context, RequestDelegate next) {
        try {
            await next(context);
        }
        catch (Exception e) {
            logger.LogError(e, "An unhandled exception occurred while processing the request.");
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        }
    }
}