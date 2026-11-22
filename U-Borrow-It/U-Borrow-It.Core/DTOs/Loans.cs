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

// The registered user a loan is being made to
public class BorrowerAccount
{
    public Guid AccountID { get; set; }
    public string Name { get; set; } = "";
}

// A loan that hasn't been returned yet, for the Active Loans page
public class ActiveLoan
{
    public Guid LoanID { get; set; }
    public string BorrowerName { get; set; } = "";
    public string ItemTitles { get; set; } = "";
    public DateTime DateOfLoan { get; set; }
    public DateTime DateOfExpectedReturn { get; set; }
    public string Status { get; set; } = "";
}

public static class LoanStatuses
{
    public const string Pending = "Pending";
    public const string Active = "Active";
    public const string Closed = "Closed";
    public const string Canceled = "Canceled";
}

public class LoanDetails
{
    public Guid LoanID { get; set; }
    public string Status { get; set; } = "";
    public string BorrowerName { get; set; } = "";
    public string ItemTitles { get; set; } = "";
    public DateTime DateOfLoan { get; set; }
    public DateTime DateOfExpectedReturn { get; set; }
    public DateTime? DateReturned { get; set; }
    public decimal? RentalPrice { get; set; }
    public string? ReturnConditionRequirements { get; set; }
    public string? LateReturnPenalty { get; set; }
    public string? LossPenalty { get; set; }
    public bool? ReturnConditionsMet { get; set; }
    public bool? ReturnedDamaged { get; set; }
    public string? ReturnNotes { get; set; }
}

public class LoanReturn
{
    public DateTime DateReturned { get; set; }
    public bool? ConditionsMet { get; set; }
    public bool Damaged { get; set; }
    public string? Notes { get; set; }
}