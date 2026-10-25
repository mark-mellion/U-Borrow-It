namespace U_Borrow_It.Core.DTOs;

public class LentItem
{
    public Guid LentItemID { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public Guid AccountID { get; set; }
    public DateTime DateAdded { get; set; }
    public bool Active { get; set; }
}