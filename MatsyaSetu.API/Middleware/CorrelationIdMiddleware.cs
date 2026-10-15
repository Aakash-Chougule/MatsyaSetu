
using Microsoft.AspNetCore.Http;

namespace MatsyaSetu.API.Middleware
{
    public class CorrelationIdMiddleware
    {
        // Used to call the next middleware
        private readonly RequestDelegate _next;

        // Used to write logs
        private readonly ILogger<CorrelationIdMiddleware> _logger;

        // Constructor: ASP.NET injects dependencies
        public CorrelationIdMiddleware(
            RequestDelegate next,
            ILogger<CorrelationIdMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        // Executes for every request passing through middleware
        public async Task InvokeAsync(HttpContext context)
        {
            // Generate a unique ID for this request
            string correlationId = Guid.NewGuid().ToString();

            // Store it for use during the current request
            context.Items["CorrelationId"] = correlationId;

            // Return the ID in the HTTP response header
            context.Response.Headers["X-Correlation-ID"] =
                correlationId;

            // Write the ID and requested path to the logs
            _logger.LogInformation(
                "Request started. CorrelationId: {CorrelationId}, Path: {Path}",
                correlationId,
                context.Request.Path);

            // Pass the request to the next middleware
            await _next(context);
        }
    }
}
