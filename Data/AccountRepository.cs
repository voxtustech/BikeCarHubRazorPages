using BikeCarHub.Domain;
using Dapper;

namespace BikeCarHub.Data;

public interface IUserRepository
{
    Task<AppUser?> FindByEmailAsync(string email);
    Task<AppUser?> FindByIdAsync(int id);
    Task CreateAsync(string fullName, string email, string passwordHash);
    Task UpdatePasswordAsync(int id, string passwordHash);
}

/// <summary>Reads and writes the existing ASP.NET Identity AspNetUsers table. Writes go through IWriteGate.</summary>
public sealed class SqlUserRepository(IDbConnectionFactory db, IWriteGate gate) : IUserRepository
{
    private const string Columns = "Id, FullName, UserName, Email, PasswordHash, SecurityStamp";

    public async Task<AppUser?> FindByEmailAsync(string email)
    {
        await using var conn = await db.OpenAsync();
        return await conn.QueryFirstOrDefaultAsync<AppUser>(
            $"SELECT TOP 1 {Columns} FROM AspNetUsers WHERE NormalizedEmail = @e OR NormalizedUserName = @e",
            new { e = email.Trim().ToUpperInvariant() });
    }

    public async Task<AppUser?> FindByIdAsync(int id)
    {
        await using var conn = await db.OpenAsync();
        return await conn.QueryFirstOrDefaultAsync<AppUser>($"SELECT {Columns} FROM AspNetUsers WHERE Id = @id", new { id });
    }

    public async Task CreateAsync(string fullName, string email, string passwordHash)
    {
        if (!gate.Allowed) throw new DatabaseWritesDisabledException();
        await using var conn = await db.OpenAsync();
        await conn.ExecuteAsync(
            """
            INSERT INTO AspNetUsers (FullName, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed,
                PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount)
            VALUES (@fullName, @email, @normalized, @email, @normalized, 0,
                @passwordHash, @securityStamp, @concurrencyStamp, 0, 0, 1, 0)
            """,
            new
            {
                fullName,
                email,
                normalized = email.ToUpperInvariant(),
                passwordHash,
                securityStamp = Guid.NewGuid().ToString("N").ToUpperInvariant(),
                concurrencyStamp = Guid.NewGuid().ToString()
            });
    }

    public async Task UpdatePasswordAsync(int id, string passwordHash)
    {
        if (!gate.Allowed) throw new DatabaseWritesDisabledException();
        await using var conn = await db.OpenAsync();
        await conn.ExecuteAsync(
            "UPDATE AspNetUsers SET PasswordHash = @passwordHash, SecurityStamp = @stamp, ConcurrencyStamp = @concurrency WHERE Id = @id",
            new { id, passwordHash, stamp = Guid.NewGuid().ToString("N").ToUpperInvariant(), concurrency = Guid.NewGuid().ToString() });
    }
}

public interface IFormRepository
{
    Task SaveQuestionAsync(string name, string email, string question);
    Task SaveNewsletterAsync(string email);
}

public sealed class SqlFormRepository(IDbConnectionFactory db, IWriteGate gate) : IFormRepository
{
    public async Task SaveQuestionAsync(string name, string email, string question)
    {
        if (!gate.Allowed) throw new DatabaseWritesDisabledException();
        await using var conn = await db.OpenAsync();
        await conn.ExecuteAsync(
            "INSERT INTO TWOrFWContactUs (Name, Email, Message, CreatedDateTime, FormType) VALUES (@name, @email, @question, SYSDATETIME(), 'AskQuestion')",
            new { name, email, question });
    }

    public async Task SaveNewsletterAsync(string email)
    {
        if (!gate.Allowed) throw new DatabaseWritesDisabledException();
        await using var conn = await db.OpenAsync();
        await conn.ExecuteAsync(
            "IF NOT EXISTS (SELECT 1 FROM NewsLetters WHERE Email = @email) INSERT INTO NewsLetters (Email, CreatedDateTime) VALUES (@email, SYSDATETIME())",
            new { email });
    }
}
