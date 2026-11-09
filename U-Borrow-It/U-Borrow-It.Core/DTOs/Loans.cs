namespace U_Borrow_It.Core.DTOs;

public class NewLoan
{
    public Guid BorrowerAccountID { get; set; }
    public List<Guid> LentItemIDs { get; set; } = new();
    public DateTime DateOfLoan { get; set; }
    public DateTime DateOfExpectedReturn { get; set; }
    public decimal? RentalPrice { get; set; }
    public string? ReturnConditionRequirements { get; set; }
    public string? LateReturnPenalty { get; set; }
    public string? LossPenalty { get; set; }
}

// Used to let users copy their previous loan policices
public class PreviousLoan
{
    public Guid LoanID { get; set; }
    public DateTime DateOfLoan { get; set; }
    public string BorrowerName { get; set; } = "";
    public string ItemTitles { get; set; } = "";
    public string? ReturnConditionRequirements { get; set; }
    public string? LateReturnPenalty { get; set; }
    public string? LossPenalty { get; set; }
}