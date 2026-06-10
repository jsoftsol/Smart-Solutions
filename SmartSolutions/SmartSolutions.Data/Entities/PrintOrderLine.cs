// SmartSolutions.Data/Entities/PrintOrderLine.cs
namespace SmartSolutions.Data.Entities;

public class PrintOrderLine
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public PrintOrder Order { get; set; } = null!;
    public int ItemNameId { get; set; }
    public ItemName ItemName { get; set; } = null!;

    public RateType     RateType { get; set; } = RateType.PerSqft;
    public DimensionUnit Unit    { get; set; } = DimensionUnit.Feet;
    public decimal? Height       { get; set; }
    public decimal? Width        { get; set; }
    public int      Quantity     { get; set; }
    public decimal  Rate         { get; set; }

    // Never stored — computed in application layer
    public decimal ComputeTotal()
    {
        if (RateType == RateType.PerPiece)
            return Quantity * Rate;

        var h = Height ?? 0m;
        var w = Width  ?? 0m;
        if (Unit == DimensionUnit.Inches)
        {
            h /= 12m;
            w /= 12m;
        }
        return h * w * Quantity * Rate;
    }
}
