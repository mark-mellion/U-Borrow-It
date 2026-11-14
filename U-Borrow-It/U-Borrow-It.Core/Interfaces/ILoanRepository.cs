using U_Borrow_It.Core.DTOs;

namespace U_Borrow_It.Core.Interfaces;

public interface ILoanRepository
{
    Task<BorrowerAccount?> FindBorrowerByEmailAsync(string email);
    Task<List<LentItem>> GetAvailableItemsAsync(Guid lenderId);

    // Returns the new LoanID, or null if any item is unavailable
    // or doesn't belong to the lender. Saves nothing in that case.
    Task<Guid?> CreateAsync(Guid lenderId, NewLoan loan);
}