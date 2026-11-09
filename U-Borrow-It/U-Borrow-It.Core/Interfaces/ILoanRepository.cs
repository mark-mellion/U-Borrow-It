using U_Borrow_It.Core.DTOs;

namespace U_Borrow_It.Core.Interfaces;

public interface ILoanRepository
{
    // Gets all items not currently loaned
    Task<List<LentItem>> GetAvailableItemsAsync(Guid lenderId);

    // Returns the new LoanID, or null if any item is unavailable
    // or doesn't belong to the lender. Saves nothing in that case.
    Task<Guid?> CreateAsync(Guid lenderId, NewLoan loan);

    // The lender's own past loans. A null or blank search ignores that filter.
    Task<List<PreviousLoan>> SearchPreviousLoansAsync(
        Guid lenderId, string? itemSearch, string? borrowerSearch);
}