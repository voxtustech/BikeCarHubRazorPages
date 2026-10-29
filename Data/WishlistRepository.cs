using Dapper;

namespace BikeCarHub.Data;

public sealed class WishlistRow
{
    public int TwoWheelerID { get; set; }
    public DateTime CreatedAt { get; set; }
}

public interface IWishlistRepository
{
    /// <summary>Saved models for one account, newest first, one row per model.</summary>
    Task<IReadOnlyList<WishlistRow>> GetAsync(int userId);
    /// <summary>Adds the models that are not saved yet. Writes go through IWriteGate.</summary>
    Task AddAsync(int userId, string? userName, IReadOnlyCollection<int> bikeIds);
    Task RemoveAsync(int userId, int bikeId);
}

/// <summary>The existing Wishlist table. It has no unique key on user + model, so inserts check first.</summary>
public sealed class SqlWishlistRepository(IDbConnectionFactory db, IWriteGate gate) : IWishlistRepository
{
    public async Task<IReadOnlyList<WishlistRow>> GetAsync(int userId)
    {
        await using var conn = await db.OpenAsync();
        return (await conn.QueryAsync<WishlistRow>(
            "SELECT TwoWheelerID, MIN(CreatedAt) AS CreatedAt FROM Wishlist WHERE UserID = @userId GROUP BY TwoWheelerID ORDER BY MIN(CreatedAt) DESC",
            new { userId })).AsList();
    }

    public async Task AddAsync(int userId, string? userName, IReadOnlyCollection<int> bikeIds)
    {
        if (!gate.Allowed) throw new DatabaseWritesDisabledException();
        if (bikeIds.Count == 0) return;
        await using var conn = await db.OpenAsync();
        await conn.ExecuteAsync(
            """
            INSERT INTO Wishlist (TwoWheelerID, Username, UserID, CreatedAt)
            SELECT @bikeId, @userName, @userId, SYSUTCDATETIME()
            WHERE NOT EXISTS (SELECT 1 FROM Wishlist WHERE UserID = @userId AND TwoWheelerID = @bikeId)
            """,
            bikeIds.Select(bikeId => new { bikeId, userName, userId }));
    }

    public async Task RemoveAsync(int userId, int bikeId)
    {
        if (!gate.Allowed) throw new DatabaseWritesDisabledException();
        await using var conn = await db.OpenAsync();
        await conn.ExecuteAsync("DELETE FROM Wishlist WHERE UserID = @userId AND TwoWheelerID = @bikeId", new { userId, bikeId });
    }
}
