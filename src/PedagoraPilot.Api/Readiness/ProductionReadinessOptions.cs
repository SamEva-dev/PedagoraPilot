namespace PedagoraPilot.Api.Readiness;
public sealed class ProductionReadinessOptions
{
    public const string SectionName = "ProductionReadiness";
    public bool FailFast { get; init; } = true;
    public bool RequireHttpsAuthGate { get; init; } = true;
    public bool RequireSecurityScanner { get; init; } = true;
    public bool ForbidDemoMode { get; init; } = true;
    public bool ForbidFileSystemStorage { get; init; } = true;
    public bool RequireEmailSending { get; init; } = true;
    public bool RequireExplicitCorsOrigins { get; init; } = true;
}
