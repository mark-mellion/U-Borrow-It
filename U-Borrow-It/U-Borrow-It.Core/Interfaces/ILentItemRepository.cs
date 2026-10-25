using U_Borrow_It.Core.DTOs;

namespace U_Borrow_It.Core.Interfaces;

public interface ILentItemRepository
{
    Task AddAsync(Guid accountId, string title, string description);
    Task<List<LentItem>> GetForAccountAsync(Guid accountId);
}