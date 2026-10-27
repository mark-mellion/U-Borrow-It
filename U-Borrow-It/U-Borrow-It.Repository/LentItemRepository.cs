using Microsoft.Data.SqlClient;
using U_Borrow_It.Core.DTOs;
using U_Borrow_It.Core.Interfaces;

namespace U_Borrow_It.Repository;

public class LentItemRepository : ILentItemRepository
{
    private readonly string _connectionString;

    public LentItemRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task AddAsync(Guid accountId, string title, string description)
    {
        const string sql = @"
            INSERT INTO dbo.LentItem (Title, [Description], AccountID, DateAdded, Active)
            VALUES (@Title, @Description, @AccountID, GETDATE(), 1);";

        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Title", title);
        cmd.Parameters.AddWithValue("@Description", description);
        cmd.Parameters.AddWithValue("@AccountID", accountId);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<List<LentItem>> GetForAccountAsync(Guid accountId)
    {
        const string sql = @"
            SELECT LentItemID, Title, [Description], AccountID, DateAdded, Active
            FROM dbo.LentItem
            WHERE AccountID = @AccountID AND Active = 1
            ORDER BY DateAdded DESC;";

        var items = new List<LentItem>();
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@AccountID", accountId);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            items.Add(new LentItem
            {
                LentItemID  = reader.GetGuid(0),
                Title       = reader.GetString(1),
                Description = reader.IsDBNull(2) ? "" : reader.GetString(2),
                AccountID   = reader.GetGuid(3),
                DateAdded   = reader.GetDateTime(4),
                Active      = reader.GetBoolean(5)
            });
        }
        return items;
    }

        public async Task<bool> DeleteAsync(Guid lentItemId, Guid accountId)
    {
        const string sql = @"
            UPDATE dbo.LentItem
            SET Active = 0
            WHERE LentItemID = @LentItemID
              AND AccountID = @AccountID
              AND Active = 1;";

        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@LentItemID", lentItemId);
        cmd.Parameters.AddWithValue("@AccountID", accountId);
        int rowsAffected = await cmd.ExecuteNonQueryAsync();
        return rowsAffected > 0;
    }

        public async Task<bool> UpdateAsync(Guid lentItemId, Guid accountId, string title, string description)
    {
        const string sql = @"
            UPDATE dbo.LentItem
            SET Title = @Title,
                [Description] = @Description
            WHERE LentItemID = @LentItemID
              AND AccountID = @AccountID
              AND Active = 1;";

        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Title", title);
        cmd.Parameters.AddWithValue("@Description", description);
        cmd.Parameters.AddWithValue("@LentItemID", lentItemId);
        cmd.Parameters.AddWithValue("@AccountID", accountId);
        int rowsAffected = await cmd.ExecuteNonQueryAsync();
        return rowsAffected > 0;
    }
        
public async Task<List<LentItem>> SearchForAccountAsync(
    Guid accountId, string searchText)
{
    const string sql = @"
        SELECT LentItemID, Title, [Description], AccountID, DateAdded, Active
        FROM dbo.LentItem
        WHERE AccountID = @AccountID
          AND Active = 1
          AND CHARINDEX(@SearchText, Title) > 0
        ORDER BY DateAdded DESC;";

            var items = new List<LentItem>();

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@AccountID", accountId);
            cmd.Parameters.AddWithValue("@SearchText", searchText);

            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                items.Add(new LentItem
                {
                    LentItemID = reader.GetGuid(0),
                    Title = reader.GetString(1),
                    Description = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    AccountID = reader.GetGuid(3),
                    DateAdded = reader.GetDateTime(4),
                    Active = reader.GetBoolean(5)
                });
            }

            return items;
        }

    }