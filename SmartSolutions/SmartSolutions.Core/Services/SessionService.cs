// SmartSolutions.Core/Services/SessionService.cs
using SmartSolutions.Core.Interfaces;
using SmartSolutions.Data.Entities;

namespace SmartSolutions.Core.Services;

public class SessionService : ISessionService
{
    private AppUser? _currentUser;

    public bool IsLoggedIn => _currentUser is not null;

    public AppUser CurrentUser => _currentUser
        ?? throw new InvalidOperationException("No user is logged in.");

    public void Login(AppUser user)
    {
        if (_currentUser is not null)
            throw new InvalidOperationException("A user is already logged in.");
        _currentUser = user;
    }
}
