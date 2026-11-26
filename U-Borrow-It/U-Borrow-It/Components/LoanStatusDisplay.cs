using Radzen;
using U_Borrow_It.Core.DTOs;

namespace U_Borrow_It.Components;

public static class LoanStatusDisplay
{
    public static string Label(string status, DateTime expectedReturn) => status switch
    {
        LoanStatuses.Active when expectedReturn < DateTime.Today => "Overdue",
        LoanStatuses.Pending  => "Pending",
        LoanStatuses.Active   => "Active",
        LoanStatuses.Closed   => "Closed",
        LoanStatuses.Canceled => "Canceled",
        _ => status
    };

    public static BadgeStyle Style(string label) => label switch
    {
        "Overdue" => BadgeStyle.Danger,
        "Pending" => BadgeStyle.Warning,
        "Active"  => BadgeStyle.Success,
        _ => BadgeStyle.Light
    };
}