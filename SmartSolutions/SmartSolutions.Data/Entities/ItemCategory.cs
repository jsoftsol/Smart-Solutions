// SmartSolutions.Data/Entities/ItemCategory.cs
namespace SmartSolutions.Data.Entities;

public class ItemCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public ICollection<ItemName> ItemNames { get; set; } = [];
}
