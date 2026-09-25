using Itech.Emailing.Persistence;
using Itech.Emailing.Webhooks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using PedagoraPilot.Api;
using PedagoraPilot.Api.Audit;
using PedagoraPilot.Api.Extensions;
using PedagoraPilot.Api.Hubs;
using PedagoraPilot.Api.Middleware;
using PedagoraPilot.Api.Readiness;
using PedagoraPilot.Application;
using PedagoraPilot.Application.Abstractions.Audit;
using PedagoraPilot.Application.Common.Realtime;
using PedagoraPilot.Infrastructure;
using PedagoraPilot.Infrastructure.Outbox;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration().MinimumLevel.Information().MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning).Enrich.FromLogContext().WriteTo.Console().CreateBootstrapLogger();
try
{
    var builder = WebApplication.CreateBuilder(args);
    ConfigureSerilogFilePaths(builder);
    builder.Host.UseSerilog((context, services, configuration) => configuration.ReadFrom.Configuration(context.Configuration).ReadFrom.Services(services).Enrich.FromLogContext());
    builder.Services.AddControllers();
    builder.Services.AddOptions<ProductionReadinessOptions>().Bind(builder.Configuration.GetSection(ProductionReadinessOptions.SectionName)).ValidateOnStart();
    builder.Services.AddSingleton<IValidateOptions<ProductionReadinessOptions>, ProductionReadinessValidator>();
    builder.Services.AddHostedService<ProductionReadinessHostedService>();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddPedagoraAuthentication(builder.Configuration);
    builder.Services.AddScoped<IAuditWriter, HttpAuditWriter>();
    builder.Services.Configure<IdempotencyOptions>(builder.Configuration.GetSection(IdempotencyOptions.SectionName));
    builder.Services.AddSignalR(options =>
    {
        options.EnableDetailedErrors = builder.Environment.IsDevelopment();
        options.MaximumReceiveMessageSize = 64 * 1024;
    });
    builder.Services.AddScoped<IRealtimeOutboxTransport, SignalROutboxTransport>();
    builder.Services.AddScoped<DomainRelay.Abstractions.INotificationHandler<RealtimeDomainEventNotification>, SignalRRealtimeNotificationHandler>();
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("Frontend", policy =>
        {
            if (allowedOrigins.Length == 0)
                return;
            policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
        });
    });
    var app = builder.Build();

    MigrationManager.ApplyMigrations(app);

    app.UseForwardedHeaders(new ForwardedHeadersOptions { ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto });
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseMiddleware<SecurityHeadersMiddleware>();
    app.UseSerilogRequestLogging();
    app.UseMiddleware<ExceptionHandlingMiddleware>();
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();
    app.UseCors("Frontend");
    app.UseAuthentication();
    app.UseMiddleware<TenantIdentityMiddleware>();
    app.UseAuthorization();
    app.UseMiddleware<IdempotencyMiddleware>();
    app.MapControllers();
    app.MapHub<NotificationsHub>(builder.Configuration["Realtime:HubPath"] ?? "/hubs/notifications");
    app.MapBrevoTransactionalWebhook("/api/webhooks/brevo/transactional");
    await ApplyEmailingMigrationsAsync(app);
    await WarmAuthGateSigningKeysAsync(app);
    Log.Information("Pedagora Pilot API starting. Environment={Environment}", app.Environment.EnvironmentName);
    await app.RunAsync();
}
catch (Exception exception)
{
    Log.Fatal(exception, "Pedagora Pilot API terminated unexpectedly.");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}


static async Task ApplyEmailingMigrationsAsync(WebApplication app)
{
    if (!app.Configuration.GetValue("Emailing:ApplyMigrationsOnStartup", false))
        return;

    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<EmailingDbContext>();

    try
    {
        await db.Database.MigrateAsync();
        Log.Information("Itech.Emailing database migrations applied successfully.");
    }
    catch (Exception exception)
    {
        Log.Fatal(exception, "Unable to apply Itech.Emailing database migrations.");
        throw;
    }
}

static async Task WarmAuthGateSigningKeysAsync(WebApplication app)
{
    try
    {
        var provider = app.Services.GetRequiredService<PedagoraPilot.Api.Authorization.AuthGateSigningKeyProvider>();
        await provider.RefreshAsync();
    }
    catch (Exception exception)
    {
        Log.Warning(exception, "AuthGate JWKS warm-up failed. The first authenticated request will retry.");
    }
}

static void ConfigureSerilogFilePaths(WebApplicationBuilder builder)
{
    const string envVar = "PEDAGORA_PILOT_HOME";
    var home = Environment.GetEnvironmentVariable(envVar);
    if (string.IsNullOrWhiteSpace(home))
    {
        var baseDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(baseDirectory))
            baseDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (string.IsNullOrWhiteSpace(baseDirectory))
            baseDirectory = AppContext.BaseDirectory;
        home = Path.Combine(baseDirectory, "PedagoraPilot");
        Environment.SetEnvironmentVariable(envVar, home, EnvironmentVariableTarget.Process);
    }

    var applicationLog = Path.Combine(home, "log", "PedagoraPilot", "PedagoraPilotService_log.txt");
    var efLog = Path.Combine(home, "log", "PedagoraPilot", "EntityFramework", "EntityFramework_log.txt");
    Directory.CreateDirectory(Path.GetDirectoryName(applicationLog)!);
    Directory.CreateDirectory(Path.GetDirectoryName(efLog)!);
    // Keep the indexes synchronized with appsettings*.json WriteTo sections.
    builder.Configuration["Serilog:WriteTo:1:Args:configureLogger:WriteTo:0:Args:path"] = applicationLog;
    builder.Configuration["Serilog:WriteTo:2:Args:configureLogger:WriteTo:0:Args:path"] = efLog;
}

public partial class Program;
