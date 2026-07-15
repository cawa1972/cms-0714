namespace CMS.API.Middleware;

/// <summary>
/// Global safety net: catches any exception that escapes a controller or repository, logs the full
/// exception (message + stack trace) server-side, and returns one consistent JSON body with a
/// generic message — never the exception text, SQL, or connection details. Registered first in the
/// pipeline so it wraps everything. Responses the pipeline produces without throwing (401 from
/// authentication, 403 from authorization, 400 validation problems) pass through untouched.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    /// <summary>The only error detail a client ever sees for an unexpected failure.</summary>
    public const string GenericErrorMessage = "An unexpected error occurred.";

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            // LogError with the exception carries the message and full stack trace to the server log.
            _logger.LogError(ex, "Unhandled exception handling {Method} {Path}",
                context.Request.Method, context.Request.Path);

            // If the response already started streaming we cannot rewrite it — let the server abort.
            if (context.Response.HasStarted)
                throw;

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new { message = GenericErrorMessage });
        }
    }
}
