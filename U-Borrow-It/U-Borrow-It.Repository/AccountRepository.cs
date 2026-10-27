using Microsoft.Data.SqlClient;
using U_Borrow_It.Core.Interfaces;

namespace U_Borrow_It.Repository;

public class AccountRepository : IAccountRepository
{
    private readonly string _connectionString;

    public AccountRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<Guid> CreateAsync(string identityUserId, string firstName, string lastName)
    {
        const string sql = @"
            INSERT INTO dbo.Account (FirstName, LastName, CreatedDate, Active, IdentityUserID)
            OUTPUT inserted.AccountID
            VALUES (@FirstName, @LastName, GETDATE(), 1, @IdentityUserID);";

        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@FirstName", firstName);
        cmd.Parameters.AddWithValue("@LastName", lastName);
        cmd.Parameters.AddWithValue("@IdentityUserID", identityUserId);

        var newId = await cmd.ExecuteScalarAsync();
        return (Guid)newId!;
    }

    public async Task<Guid?> GetAccountIdForUserAsync(string identityUserId)
    {
        const string sql = @"
            SELECT AccountID
            FROM dbo.Account
            WHERE IdentityUserID = @IdentityUserID
              AND Active = 1;";

        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@IdentityUserID", identityUserId);

        var result = await cmd.ExecuteScalarAsync();
        return result is Guid accountId ? accountId : null;
    }
}