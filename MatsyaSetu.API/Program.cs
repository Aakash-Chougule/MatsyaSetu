using MatsyaSetu.API.Middleware;
using MatsyaSetu.Application.Handlers;
using MatsyaSetu.Application.Interfaces;
using MatsyaSetu.Application.Services;
using MatsyaSetu.Infrastructure.Database;
using MatsyaSetu.Infrastructure.Repositories;
using MatsyaSetu.Infrastructure.Services;
using System.Security.Cryptography;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// Add services to the container
// ============================================================

builder.Services.AddControllers();

// OpenAPI
builder.Services.AddOpenApi();


// ============================================================
// Applicant services
// ============================================================

// Applicant service
builder.Services.AddScoped<IApplicantService, ApplicantService>();


// ============================================================
// Lifetime demonstration services
// ============================================================

// New instance every time it is requested
builder.Services.AddTransient<ITransientService, TransientService>();

// One instance per HTTP request
builder.Services.AddScoped<IScopedService, ScopedService>();

// One shared instance for the application lifetime
builder.Services.AddSingleton<ISingletonService, SingletonService>();


// ============================================================
// Database
// ============================================================

// Centralized SQL connection factory
builder.Services.AddScoped<
    IDbConnectionFactory,
    SqlConnectionFactory>();


// ============================================================
// Repositories
// ============================================================

builder.Services.AddScoped<
    IApplicantRepository,
    ApplicantRepository>();

builder.Services.AddScoped<
    IBotMenuRepository,
    BotMenuRepository>();

builder.Services.AddScoped<
    ISchemeRepository,
    SchemeRepository>();


// ============================================================
// WhatsApp
// ============================================================

// Register WhatsAppService with HttpClient
builder.Services.AddHttpClient<
    IWhatsAppService,
    WhatsAppService>();


// ============================================================
// Bot action architecture
// ============================================================

// Dispatcher
builder.Services.AddScoped<
    IBotActionDispatcher,
    BotActionDispatcher>();

// Action handlers
builder.Services.AddScoped<
    IBotActionHandler,
    ShowSchemesHandler>();

builder.Services.AddScoped<
    IBotActionHandler,
    StartApplicationHandler>();

builder.Services.AddScoped<
    IBotActionHandler,
    CheckStatusHandler>();

builder.Services.AddScoped<
    IBotActionHandler,
    HelpHandler>();


// ============================================================
// Build application
// ============================================================

var app = builder.Build();


// ============================================================
// Temporary WhatsApp development endpoint
// ============================================================
// Remove this later after architecture/testing is completed.
// ============================================================

if (app.Environment.IsDevelopment())
{
    app.MapPost(
        "/api/dev/whatsapp/send-menu",
        async (
            HttpContext context,
            IConfiguration configuration,
            IWhatsAppService whatsAppService,
            CancellationToken cancellationToken) =>
        {
            // ----------------------------------------------------
            // Authenticate development request
            // ----------------------------------------------------

            string? expectedKey =
                configuration["WhatsApp:DevTestKey"];

            string providedKey =
                context.Request.Headers[
                    "X-Dev-Test-Key"].ToString();

            if (string.IsNullOrWhiteSpace(expectedKey) ||
                string.IsNullOrWhiteSpace(providedKey))
            {
                return Results.Unauthorized();
            }

            byte[] expected =
                Encoding.UTF8.GetBytes(
                    expectedKey);

            byte[] provided =
                Encoding.UTF8.GetBytes(
                    providedKey);

            if (!CryptographicOperations.FixedTimeEquals(
                    expected,
                    provided))
            {
                return Results.Unauthorized();
            }


            // ----------------------------------------------------
            // Read authorized test recipient
            // ----------------------------------------------------

            string? recipient =
                configuration[
                    "WhatsApp:AllowedTestRecipientWaId"];

            if (string.IsNullOrWhiteSpace(recipient))
            {
                return Results.Problem(
                    "Test recipient is not configured.");
            }


            // ----------------------------------------------------
            // Send WhatsApp menu
            // ----------------------------------------------------

            await whatsAppService.SendWelcomeMenuAsync(
                recipient,
                cancellationToken);


            return Results.Ok(new
            {
                message =
                    "Marathi menu submitted to Meta."
            });
        });
}


// ============================================================
// Custom middleware
// ============================================================

app.UseMiddleware<CorrelationIdMiddleware>();

app.UseMiddleware<GlobalExceptionMiddleware>();


// ============================================================
// Development-only endpoints
// ============================================================

if (app.Environment.IsDevelopment())
{
    // --------------------------------------------------------
    // OpenAPI
    // --------------------------------------------------------

    app.MapOpenApi();


    // --------------------------------------------------------
    // Temporary database connection test
    // --------------------------------------------------------
    // This now uses the new connection factory.
    // The factory creates AND opens the connection.
    // --------------------------------------------------------

    app.MapGet(
        "/api/dev/db-check",
        async (
            IDbConnectionFactory connectionFactory,
            CancellationToken cancellationToken) =>
        {
            // Create and open SQL connection
            await using var connection =
                await connectionFactory
                    .CreateOpenConnectionAsync(
                        cancellationToken);


            // Create SQL command
            await using var command =
                connection.CreateCommand();


            // SQL query
            command.CommandText =
                "SELECT DB_NAME()";


            // Execute query
            var databaseName =
                await command.ExecuteScalarAsync(
                    cancellationToken);


            // Return result
            return Results.Ok(new
            {
                message =
                    "Database connected successfully",

                database =
                    databaseName
            });
        });
}


// ============================================================
// HTTP request pipeline
// ============================================================

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();


// ============================================================
// Start application
// ============================================================

app.Run();