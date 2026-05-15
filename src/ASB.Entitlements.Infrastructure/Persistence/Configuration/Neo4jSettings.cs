namespace ASB.Entitlements.Infrastructure.Persistence.Configuration;

public sealed class Neo4jSettings
{
    public const string SectionName = "Neo4j";

    public string Uri { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Database { get; set; } = "neo4j";
    public int MaxConnectionPoolSize { get; set; } = 50;
    public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(30);
}
