// SmartSolutions.Tests/Helpers/TestSessionService.cs
using SmartSolutions.Core.Interfaces;
using SmartSolutions.Data.Entities;

namespace SmartSolutions.Tests.Helpers;

public class TestSessionService(int userId = 1, string username = "testuser") : ISessionService
{
    public bool    IsLoggedIn  => true;
    public AppUser CurrentUser => new() { Id = userId, Username = username, PinHash = "", IsActive = true };
    public void    Login(AppUser user) { }
}
