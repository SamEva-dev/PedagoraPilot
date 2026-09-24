namespace PedagoraPilot.Contracts.Common;
public sealed record ApiErrorResponse(string Code, int Status, string TraceId, IReadOnlyDictionary<string, object?>? Parameters = null, IReadOnlyDictionary<string, string[]>? Errors = null);
