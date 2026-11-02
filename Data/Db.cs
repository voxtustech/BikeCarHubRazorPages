using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BikeCarHub.Data;

public interface IDbConnectionFactory
{
    bool IsConfigured { get; }
    /// <summary>Connection for reading. Uses ConnectionStrings:Default, which should be a read-only login.</summary>
    Task<DbConnection> OpenAsync(CancellationToken ct = default);
    /// <summary>Connection for statements that change data. Uses ConnectionStrings:Write when set, otherwise Default.</summary>
    Task<DbConnection> OpenWriteAsync(CancellationToken ct = default);
}

public sealed class SqlConnectionFactory(IConfiguration config) : IDbConnectionFactory
{
    private readonly string? _connectionString = config.GetConnectionString("Default");
    private readonly string? _writeConnectionString = config.GetConnectionString("Write");

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_connectionString);

    public Task<DbConnection> OpenAsync(CancellationToken ct = default) => OpenAsync(_connectionString, ct);

    public Task<DbConnection> OpenWriteAsync(CancellationToken ct = default) =>
        OpenAsync(string.IsNullOrWhiteSpace(_writeConnectionString) ? _connectionString : _writeConnectionString, ct);

    private static async Task<DbConnection> OpenAsync(string? connectionString, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("ConnectionStrings:Default is not set. Use 'dotnet user-secrets set' (see README).");
        var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(ct);
        return conn;
    }
}

/// <summary>Single switch for every statement that changes data. Off unless Database:AllowWrites is true.</summary>
public interface IWriteGate
{
    bool Allowed { get; }
}

public sealed class ConfigWriteGate(IConfiguration config) : IWriteGate
{
    public bool Allowed => config.GetValue("Database:AllowWrites", false);
}

public sealed class DatabaseWritesDisabledException() : InvalidOperationException("Database writes are disabled (Database:AllowWrites is false).");

public sealed class DbHealthCheck(IDbConnectionFactory db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        if (!db.IsConfigured) return HealthCheckResult.Unhealthy("Connection string not configured");
        try
        {
            await using var conn = await db.OpenAsync(ct);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT 1";
            await cmd.ExecuteScalarAsync(ct);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(ex.GetType().Name);
        }
    }
}
