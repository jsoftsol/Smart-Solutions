// SmartSolutions.Data/Entities/Enums.cs
namespace SmartSolutions.Data.Entities;

public enum PrintOrderStatus { Draft, Confirmed, SentToVendor, Ready, Delivered }
public enum HaierJobType    { Warranty, OutOfWarranty }
public enum HaierJobStatus  { Pending, InProgress, Completed }
public enum RateType        { PerSqft, PerPiece }
public enum DimensionUnit   { Feet, Inches }
