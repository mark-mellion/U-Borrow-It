using Microsoft.Data.SqlClient;
using U_Borrow_It.Core.DTOs;
using U_Borrow_It.Core.Interfaces;

namespace U_Borrow_It.Repository;

// Implements the database operations defined by ILentItemRepository.
// Handles creating, retrieving, updating, deleting, and searching
// items in SQL Server.
public class LentItemRepository : ILentItemRepository
{
    private readonly string _connectionString;

    public LentItemRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

// Creates a new item in the SQL Server database.
// Associates the item with the specified account ID.
// New items are marked active and assigned the current date.
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

// Performs a soft delete by setting Active to 0.
// The item remains in the database but is excluded from normal item lists.
// Returns true if an active item belonging to the account was updated.
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

// Searches for active items belonging to the specified account.
// Uses CHARINDEX to find titles containing the search text.
// Returns matching items ordered from newest to oldest.
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

 // Bind the account ID and search text to the SQL parameters.
// Parameters keep user input separate from the SQL command.

            cmd.Parameters.AddWithValue("@AccountID", accountId);
            cmd.Parameters.AddWithValue("@SearchText", searchText);

            await using var reader = await cmd.ExecuteReaderAsync();

// Read each returned SQL row, convert it into a LentItem
// object, and add it to the results list.
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