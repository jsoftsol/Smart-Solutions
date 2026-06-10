// SmartSolutions.Data/Entities/Vendor.cs
namespace SmartSolutions.Data.Entities;

public class Vendor
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Phone { get; set; }
    public string? Notes { get; set; }
}
