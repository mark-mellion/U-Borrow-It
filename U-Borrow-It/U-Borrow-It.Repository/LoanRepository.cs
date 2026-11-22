using Microsoft.Data.SqlClient;
using U_Borrow_It.Core.DTOs;
using U_Borrow_It.Core.Interfaces;

namespace U_Borrow_It.Repository;

// Handles finding borrowers, listing available items, and creating loans.
public class LoanRepository : ILoanRepository
{
    private readonly string _connectionString;

    public LoanRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    // Finds an active registered user by exact email address
    // May add other ways to find clients, maybe similar to Venmo
    public async Task<BorrowerAccount?> FindBorrowerByEmailAsync(string email)
    {
        const string sql = @"
            SELECT a.AccountID, CONCAT_WS(' ', a.FirstName, a.LastName)
            FROM dbo.Account a
            JOIN dbo.AspNetUsers u ON u.Id = a.IdentityUserID
            WHERE u.NormalizedEmail = @NormalizedEmail
              AND a.Active = 1;";

        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@NormalizedEmail", email.Trim().ToUpperInvariant());

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new BorrowerAccount
            {
                AccountID = reader.GetGuid(0),
                Name = reader.GetString(1)
            };
        }
        return null;
    }

    // Returns the lender's active items that aren't on a loan
    // whose status reserves them (Pending or Active).
    public async Task<List<LentItem>> GetAvailableItemsAsync(Guid lenderId)
    {
        const string sql = @"
            SELECT li.LentItemID, li.Title, li.[Description], li.AccountID, li.DateAdded, li.Active
            FROM dbo.LentItem li
            WHERE li.AccountID = @LenderID
              AND li.Active = 1
              AND NOT EXISTS (
                  SELECT 1
                  FROM dbo.Loan_LentItem lli
                  JOIN dbo.Loan l ON l.LoanID = lli.LoanID
                  JOIN dbo.LoanStatus s ON s.Name = l.Status
                  WHERE lli.LentItemID = li.LentItemID
                    AND s.ReservesItems = 1)
            ORDER BY li.Title;";

        var items = new List<LentItem>();
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@LenderID", lenderId);
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

    // Creates the loan and links its items in a single transaction.
    public async Task<Guid?> CreateAsync(Guid lenderId, NewLoan loan)
    {
        const string insertLoanSql = @"
            INSERT INTO dbo.Loan (LenderID, BorrowerAccountID, DateOfLoan, DateOfExpectedReturn,
                                  RentalPrice, ReturnConditionRequirements, LateReturnPenalty, LossPenalty, Status)
            OUTPUT inserted.LoanID
            VALUES (@LenderID, @BorrowerAccountID, @DateOfLoan, @DateOfExpectedReturn,
                    @RentalPrice, @ReturnConditionRequirements, @LateReturnPenalty, @LossPenalty,
                    @Status);";

        const string insertItemSql = @"
            INSERT INTO dbo.Loan_LentItem (LoanID, LentItemID)
            SELECT @LoanID, li.LentItemID
            FROM dbo.LentItem li WITH (UPDLOCK, HOLDLOCK)
            WHERE li.LentItemID = @LentItemID
              AND li.AccountID = @LenderID
              AND li.Active = 1
              AND NOT EXISTS (
                  SELECT 1
                  FROM dbo.Loan_LentItem lli
                  JOIN dbo.Loan l ON l.LoanID = lli.LoanID
                  JOIN dbo.LoanStatus s ON s.Name = l.Status
                  WHERE lli.LentItemID = li.LentItemID
                    AND s.ReservesItems = 1)";

        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var transaction = conn.BeginTransaction();

        Guid loanId;
        await using (var loanCmd = new SqlCommand(insertLoanSql, conn, transaction))
        {
            loanCmd.Parameters.AddWithValue("@LenderID", lenderId);
            loanCmd.Parameters.AddWithValue("@BorrowerAccountID", loan.BorrowerAccountID);
            loanCmd.Parameters.AddWithValue("@DateOfLoan", loan.DateOfLoan);
            loanCmd.Parameters.AddWithValue("@DateOfExpectedReturn", loan.DateOfExpectedReturn);
            loanCmd.Parameters.AddWithValue("@RentalPrice", (object?)loan.RentalPrice ?? DBNull.Value);
            loanCmd.Parameters.AddWithValue("@ReturnConditionRequirements", ToDbValue(loan.ReturnConditionRequirements));
            loanCmd.Parameters.AddWithValue("@LateReturnPenalty", ToDbValue(loan.LateReturnPenalty));
            loanCmd.Parameters.AddWithValue("@LossPenalty", ToDbValue(loan.LossPenalty));
            loanCmd.Parameters.AddWithValue("@Status", LoanStatuses.Pending);

            loanId = (Guid)(await loanCmd.ExecuteScalarAsync())!;
        }

        foreach (var lentItemId in loan.LentItemIDs)
        {
            await using var itemCmd = new SqlCommand(insertItemSql, conn, transaction);
            itemCmd.Parameters.AddWithValue("@LoanID", loanId);
            itemCmd.Parameters.AddWithValue("@LentItemID", lentItemId);
            itemCmd.Parameters.AddWithValue("@LenderID", lenderId);

            // If no rows, it means one of the checks failed for an item
            int rowsAffected = await itemCmd.ExecuteNonQueryAsync();
            if (rowsAffected == 0)
            {
                await transaction.RollbackAsync();
                return null;
            }
        }

        await transaction.CommitAsync();
        return loanId;
    }

    // Trims text and turns blank values into NULL.
    private static object ToDbValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();
    }

    // Returns the lender's open loans with the borrower's name
    // and the loaned item titles, ordered by due date.
    public async Task<List<ActiveLoan>> GetActiveLoansAsync(Guid lenderId)
    {
        const string sql = @"
            SELECT l.LoanID,
                   CONCAT_WS(' ', a.FirstName, a.LastName) AS BorrowerName,
                   items.ItemTitles,
                   l.DateOfLoan,
                   l.DateOfExpectedReturn,
                   l.Status
            FROM dbo.Loan l
            JOIN dbo.Account a ON a.AccountID = l.BorrowerAccountID
            JOIN dbo.LoanStatus s ON s.Name = l.Status
            CROSS APPLY (
                SELECT STRING_AGG(li.Title, ', ') WITHIN GROUP (ORDER BY li.Title) AS ItemTitles
                FROM dbo.Loan_LentItem lli
                JOIN dbo.LentItem li ON li.LentItemID = lli.LentItemID
                WHERE lli.LoanID = l.LoanID) items
            WHERE l.LenderID = @LenderID
              AND s.ReservesItems = 1
            ORDER BY l.DateOfExpectedReturn;";

        var loans = new List<ActiveLoan>();
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@LenderID", lenderId);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            loans.Add(new ActiveLoan
            {
                LoanID               = reader.GetGuid(0),
                BorrowerName         = reader.GetString(1),
                ItemTitles           = reader.IsDBNull(2) ? "" : reader.GetString(2),
                DateOfLoan           = reader.GetDateTime(3),
                DateOfExpectedReturn = reader.GetDateTime(4),
                Status               = reader.GetString(5)
            });
        }
        return loans;
    }

    public Task<bool> ActivateAsync(Guid loanId, Guid lenderId) =>
    ChangeStatusAsync(loanId, lenderId, LoanStatuses.Pending, LoanStatuses.Active, setReturnDate: false);

    public Task<bool> CancelAsync(Guid loanId, Guid lenderId) =>
        ChangeStatusAsync(loanId, lenderId, LoanStatuses.Pending, LoanStatuses.Canceled, setReturnDate: false);

    public Task<bool> CloseAsync(Guid loanId, Guid lenderId) =>
        ChangeStatusAsync(loanId, lenderId, LoanStatuses.Active, LoanStatuses.Closed, setReturnDate: true);

    private async Task<bool> ChangeStatusAsync(
        Guid loanId, Guid lenderId, string fromStatus, string toStatus, bool setReturnDate)
    {
        const string sql = @"
            UPDATE dbo.Loan
            SET Status = @ToStatus,
                DateReturned = CASE WHEN @SetReturnDate = 1 THEN GETDATE() ELSE DateReturned END
            WHERE LoanID = @LoanID
              AND LenderID = @LenderID
              AND Status = @FromStatus;";

        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@ToStatus", toStatus);
        cmd.Parameters.AddWithValue("@SetReturnDate", setReturnDate);
        cmd.Parameters.AddWithValue("@LoanID", loanId);
        cmd.Parameters.AddWithValue("@LenderID", lenderId);
        cmd.Parameters.AddWithValue("@FromStatus", fromStatus);
        int rowsAffected = await cmd.ExecuteNonQueryAsync();
        return rowsAffected > 0;
    }
}