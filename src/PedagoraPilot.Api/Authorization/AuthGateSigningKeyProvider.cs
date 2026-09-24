using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace PedagoraPilot.Api.Authorization;
public sealed class AuthGateSigningKeyProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptionsMonitor<AuthGateOptions> _options;
    private readonly ILogger<AuthGateSigningKeyProvider> _logger;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private volatile IReadOnlyList<SecurityKey> _keys = Array.Empty<SecurityKey>();
    public AuthGateSigningKeyProvider(IHttpClientFactory httpClientFactory, IOptionsMonitor<AuthGateOptions> options, ILogger<AuthGateSigningKeyProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options;
        _logger = logger;
    }

    public bool HasKeys => _keys.Count > 0;

    public bool ContainsKey(string? kid)
    {
        if (string.IsNullOrWhiteSpace(kid))
            return HasKeys;

        var snapshot = _keys;
        return snapshot.Any(key => string.Equals(key.KeyId, kid, StringComparison.Ordinal));
    }

    public IEnumerable<SecurityKey> Resolve(string? kid)
    {
        var snapshot = _keys;
        return string.IsNullOrWhiteSpace(kid) ? snapshot : snapshot.Where(key => string.Equals(key.KeyId, kid, StringComparison.Ordinal));
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        await _refreshLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var options = _options.CurrentValue;
            var client = _httpClientFactory.CreateClient("AuthGateJwks");
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
            var path = options.JwksPath.TrimStart('/');
            var document = await client.GetFromJsonAsync<JwksDocument>(path, cancellationToken).ConfigureAwait(false);
            if (document?.Keys is null || document.Keys.Count == 0)
                throw new InvalidOperationException("AuthGate JWKS does not contain any signing key.");
            _keys = document.Keys.Select(ToSecurityKey).Cast<SecurityKey>().ToArray();
            _logger.LogInformation("Loaded {Count} AuthGate signing key(s). Kids={Kids}", _keys.Count, string.Join(",", _keys.Select(x => x.KeyId)));
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private static JsonWebKey ToSecurityKey(JwksKey key) => new()
    {
        Kty = key.Kty,
        Use = key.Use,
        Kid = key.Kid,
        Alg = key.Alg,
        N = key.N,
        E = key.E
    };
    private sealed class JwksDocument
    {
        public List<JwksKey> Keys { get; set; } = [];
    }

    private sealed class JwksKey
    {
        public string Kty { get; set; } = string.Empty;
        public string Use { get; set; } = string.Empty;
        public string Kid { get; set; } = string.Empty;
        public string Alg { get; set; } = string.Empty;
        public string N { get; set; } = string.Empty;
        public string E { get; set; } = string.Empty;
    }
}
