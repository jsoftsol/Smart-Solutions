// SmartSolutions.Data/Entities/AppUser.cs
namespace SmartSolutions.Data.Entities;

public class AppUser
{
    public int    Id       { get; set; }
    public string Username { get; set; } = "";
    public string PinHash  { get; set; } = "";
    public bool   IsActive { get; set; } = true;
}
