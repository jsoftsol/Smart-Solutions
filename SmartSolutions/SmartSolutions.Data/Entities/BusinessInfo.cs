// SmartSolutions.Data/Entities/BusinessInfo.cs
namespace SmartSolutions.Data.Entities;

public class BusinessInfo
{
    public int Id { get; set; }  // Always 1 — singleton row
    public string Name    { get; set; } = "";
    public string Ntn     { get; set; } = "";
    public string Address { get; set; } = "";
    public string Phone1  { get; set; } = "";
    public string? Phone2 { get; set; }
    public string? Email  { get; set; }
    public byte[]? Logo   { get; set; }
}
