// SmartSolutions.Data/Entities/Expense.cs
namespace SmartSolutions.Data.Entities;

public class Expense
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public ExpenseCategory Category { get; set; } = null!;
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public int ChannelId { get; set; }
    public PaymentChannel Channel { get; set; } = null!;
    public DateTime Date { get; set; }
    public int?     CreatedById { get; set; }
    public AppUser? CreatedBy   { get; set; }
}
