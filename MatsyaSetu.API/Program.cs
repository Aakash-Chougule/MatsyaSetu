using MatsyaSetu.API.Middleware;
using MatsyaSetu.Application.Handlers;
using MatsyaSetu.Application.Interfaces;
using MatsyaSetu.Application.Services;
using MatsyaSetu.Infrastructure.Data;
using MatsyaSetu.Infrastructure.Repositories;
using MatsyaSetu.Infrastructure.Services;
using System.Security.Cryptography;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
// Register our applicant service
builder.Services.AddScoped<IApplicantService, ApplicantService>();
// New instance for every resolution
builder.Services.AddTransient<ITransientService, TransientService>();
// One instance per request
builder.Services.AddScoped<IScopedService, ScopedService>();
// Shared instance across requests
builder.Services.AddSingleton<ISingletonService, SingletonService>();

// Read connection string from configuration
string connectionString = builder.Configuration.GetConnectionString("MatsyaSetuDb") ?? throw new InvalidOperationException( "Database connection string is missing.");

// Register our connection factory
builder.Services.AddScoped<IDbConnectionFactory>(_ => new SqlConnectionFactory(connectionString));
// Register repository using Dependency Injection
builder.Services.AddScoped<IApplicantRepository,ApplicantRepository>();
// Register WhatsApp service with HttpClient
builder.Services.AddHttpClient<IWhatsAppService, WhatsAppService>();

builder.Services.AddScoped<IBotMenuRepository, BotMenuRepository>();

builder.Services.AddScoped<ISchemeRepository, SchemeRepository>();

builder.Services.AddScoped<IBotActionDispatcher, BotActionDispatcher>();

builder.Services.AddScoped<IBotActionHandler, ShowSchemesHandler>();
builder.Services.AddScoped<IBotActionHandler, StartApplicationHandler>();
builder.Services.AddScoped<IBotActionHandler, CheckStatusHandler>();
builder.Services.AddScoped<IBotActionHandler, HelpHandler>();

var app = builder.Build();




// Temporary endpoint for testing WhatsApp
// Remove after successful testing.
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
            // Authenticate our development request
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
                Encoding.UTF8.GetBytes(expectedKey);

            byte[] provided =
                Encoding.UTF8.GetBytes(providedKey);

            if (!CryptographicOperations.FixedTimeEquals(
                    expected, provided))
            {
                return Results.Unauthorized();
            }

            // Read our authorized test recipient
            string? recipient =
                configuration[
                    "WhatsApp:AllowedTestRecipientWaId"];

            if (string.IsNullOrWhiteSpace(recipient))
            {
                return Results.Problem(
                    "Test recipient is not configured.");
            }

            // Call our existing WhatsAppService
            await whatsAppService.SendWelcomeMenuAsync(
                recipient,
                cancellationToken);

            return Results.Ok(new
            {
                message = "Marathi menu submitted to Meta."
            });
        });
}






// Add custom middleware to the pipeline
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();

// Configure the HTTP request pipeline.

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // Temporary API to test our database connection
    app.MapGet("/api/dev/db-check",
        async (IDbConnectionFactory connectionFactory) =>
        {
            // Step 1: Create a database connection
            await using var connection =
                connectionFactory.CreateConnection();

            // Step 2: Open the SQL Server connection
            await connection.OpenAsync();

            // Step 3: Create an SQL command
            await using var command =
                connection.CreateCommand();

            // Step 4: Write our SQL query
            command.CommandText = "SELECT DB_NAME()";

            // Step 5: Execute the query
            var databaseName =
                await command.ExecuteScalarAsync();

            // Step 6: Return the result
            return Results.Ok(new
            {
                message = "Database connected successfully",
                database = databaseName
            });
        });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
