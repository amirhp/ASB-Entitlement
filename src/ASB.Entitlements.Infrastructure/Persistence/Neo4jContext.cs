using ASB.Entitlements.Infrastructure.Persistence.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neo4j.Driver;

namespace ASB.Entitlements.Infrastructure.Persistence;

public interface INeo4jContext : IDisposable
{
    IAsyncSession GetSession();
}

public sealed class Neo4jContext : INeo4jContext
{
    private readonly IDriver _driver;
    private readonly ILogger<Neo4jContext> _logger;
    private readonly Neo4jSettings _settings;

    public Neo4jContext(
        IOptions<Neo4jSettings> settings,
        ILogger<Neo4jContext> logger)
    {
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        try
        {
            // For neo4j+s:// or bolt+s:// schemes, don't specify encryption level
            // The scheme itself handles SSL/TLS configuration
            var usesSecureScheme = _settings.Uri.StartsWith("neo4j+s://", StringComparison.OrdinalIgnoreCase) ||
                                   _settings.Uri.StartsWith("bolt+s://", StringComparison.OrdinalIgnoreCase);

            _driver = GraphDatabase.Driver(
                _settings.Uri,
                AuthTokens.Basic(_settings.Username, _settings.Password),
                config => config
                    .WithMaxConnectionPoolSize(_settings.MaxConnectionPoolSize)
                    .WithConnectionTimeout(_settings.ConnectionTimeout));

            _logger.LogInformation(
                "Neo4j connection established successfully to {Uri} (Secure: {Secure})",
                _settings.Uri,
                usesSecureScheme);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to establish Neo4j connection to {Uri}", _settings.Uri);
            throw;
        }
    }

    public IAsyncSession GetSession()
    {
        return _driver.AsyncSession(o => o.WithDatabase(_settings.Database));
    }

    public void Dispose()
    {
        _driver?.Dispose();
        _logger.LogInformation("Neo4j connection disposed");
    }
}
