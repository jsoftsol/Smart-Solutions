// SmartSolutions.Core/Interfaces/ISessionService.cs
using SmartSolutions.Data.Entities;

namespace SmartSolutions.Core.Interfaces;

public interface ISessionService
{
    bool    IsLoggedIn  { get; }
    AppUser CurrentUser { get; }
    void    Login(AppUser user);
}
