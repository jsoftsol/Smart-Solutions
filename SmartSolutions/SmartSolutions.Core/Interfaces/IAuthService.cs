// SmartSolutions.Core/Interfaces/IAuthService.cs
using SmartSolutions.Data.Entities;

namespace SmartSolutions.Core.Interfaces;

public interface IAuthService
{
    Task<AppUser?>       ValidateAsync(string username, string pin);
    Task<IList<AppUser>> GetAllAsync();
    Task                 CreateAsync(string username, string pin);
    Task                 UpdatePinAsync(int userId, string newPin);
    Task                 SetActiveAsync(int userId, bool isActive);
    Task<bool>           AnyUserExistsAsync();
}
