namespace U_Borrow_It.Core.Interfaces;

public interface IAccountRepository
{
    Task<Guid> CreateAsync(string identityUserId, string firstName, string lastName);
    Task<Guid?> GetAccountIdForUserAsync(string identityUserId);
}