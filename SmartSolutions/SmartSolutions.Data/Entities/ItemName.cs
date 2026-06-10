// SmartSolutions.Data/Entities/ItemName.cs
namespace SmartSolutions.Data.Entities;

public class ItemName
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int CategoryId { get; set; }
    public ItemCategory Category { get; set; } = null!;
}
