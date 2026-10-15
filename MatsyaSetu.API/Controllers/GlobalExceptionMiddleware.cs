
using Microsoft.AspNetCore.Http;

namespace MatsyaSetu.API.Middleware
{
    public class GlobalExceptionMiddleware
    {
        // References the next middleware
        private readonly RequestDelegate _next;

        // ASP.NET provides this logger through DI
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        // Constructor injection
        public GlobalExceptionMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                // Continue processing the HTTP request
                await _next(context);
            }
            catch (Exception ex)
            {
                // Get the correlation ID from our previous middleware
                string correlationId =
                    context.Items["CorrelationId"]?.ToString()
                    ?? context.TraceIdentifier;

                // Record the actual exception for developers
                _logger.LogError(
                    ex,
                    "Unhandled exception. CorrelationId: {CorrelationId}",
                    correlationId);

                // An already-started response cannot be replaced
                if (context.Response.HasStarted)
                {
                    throw;
                }

                // Return a controlled error response
                context.Response.StatusCode =
                    StatusCodes.Status500InternalServerError;

                await context.Response.WriteAsJsonAsync(new
                {
                    status = 500,
                    message = "An unexpected error occurred.",
                    correlationId = correlationId
                });
            }
        }
    }
}
