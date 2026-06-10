// SmartSolutions.Data/Entities/Customer.cs
namespace SmartSolutions.Data.Entities;

public class Customer
{
    public int Id { get; set; }
    public string Name    { get; set; } = "";
    public string? Phone   { get; set; }
    public string? Address { get; set; }
    public string? Notes   { get; set; }
    public int?     CreatedById { get; set; }
    public AppUser? CreatedBy   { get; set; }
}
