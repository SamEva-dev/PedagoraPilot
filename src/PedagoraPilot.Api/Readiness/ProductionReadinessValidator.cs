using Microsoft.Extensions.Options;

namespace PedagoraPilot.Api.Readiness;
public sealed class ProductionReadinessValidator(IConfiguration configuration, IHostEnvironment environment) : IValidateOptions<ProductionReadinessOptions>
{
    public ValidateOptionsResult Validate(string? name, ProductionReadinessOptions options)
    {
        if (!environment.IsProduction())
            return ValidateOptionsResult.Success;
        var failures = new List<string>();
        var authGate = configuration["AuthGate:BaseUrl"];
        var scanner = configuration["Storage:SecurityScanner:Provider"];
        var storage = configuration["Storage:Provider"];
        var demo = configuration.GetValue<bool>("Demo:Enabled");
        var emailSending = configuration.GetValue<bool>("Brevo:EnableSending");
        var cors = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        if (options.RequireHttpsAuthGate && (string.IsNullOrWhiteSpace(authGate) || !authGate.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
            failures.Add("AuthGate:BaseUrl must use HTTPS in Production.");
        if (options.RequireSecurityScanner && string.Equals(scanner, "NoOp", StringComparison.OrdinalIgnoreCase))
            failures.Add("A real document security scanner is required in Production.");
        if (options.ForbidFileSystemStorage && string.Equals(storage, "FileSystem", StringComparison.OrdinalIgnoreCase))
            failures.Add("FileSystem document storage is forbidden in Production; configure S3/MinIO/object storage.");
        if (options.ForbidDemoMode && demo)
            failures.Add("Demo:Enabled must be false in Production.");
        if (options.RequireEmailSending && !emailSending)
            failures.Add("Brevo:EnableSending must be true in Production.");
        if (options.RequireExplicitCorsOrigins && (cors.Length == 0 || cors.Any(x => x == "*")))
            failures.Add("Production CORS origins must be explicit.");
        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
