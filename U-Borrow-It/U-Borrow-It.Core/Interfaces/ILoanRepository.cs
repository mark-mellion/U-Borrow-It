using U_Borrow_It.Core.DTOs;

namespace U_Borrow_It.Core.Interfaces;

public interface ILoanRepository
{
    Task<BorrowerAccount?> FindBorrowerByEmailAsync(string email);
    Task<List<LentItem>> GetAvailableItemsAsync(Guid lenderId);

    // Returns the new LoanID, or null if any item is unavailable
    // or doesn't belong to the lender.
    Task<Guid?> CreateAsync(Guid lenderId, NewLoan loan);
    Task<List<ActiveLoan>> GetActiveLoansAsync(Guid lenderId);
    // Each returns false if the loan isn't the lender's or isn't in the right status for that change.
    Task<bool> ActivateAsync(Guid loanId, Guid lenderId);
    Task<bool> CancelAsync(Guid loanId, Guid lenderId);
    Task<bool> CloseAsync(Guid loanId, Guid lenderId);
}