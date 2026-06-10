// SmartSolutions.Core/Services/AuthService.cs
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using SmartSolutions.Core.Interfaces;
using SmartSolutions.Data;
using SmartSolutions.Data.Entities;

namespace SmartSolutions.Core.Services;

public class AuthService(IDbContextFactory<AppDbContext> factory) : IAuthService
{
    public async Task<AppUser?> ValidateAsync(string username, string pin)
    {
        await using var db = factory.CreateDbContext();
        var user = await db.AppUsers
            .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);
        if (user is null) return null;
        return VerifyPin(pin, user.PinHash) ? user : null;
    }

    public async Task<IList<AppUser>> GetAllAsync()
    {
        await using var db = factory.CreateDbContext();
        return await db.AppUsers.OrderBy(u => u.Username).ToListAsync();
    }

    public async Task CreateAsync(string username, string pin)
    {
        await using var db = factory.CreateDbContext();
        if (await db.AppUsers.AnyAsync(u => u.Username == username))
            throw new InvalidOperationException($"Username '{username}' already exists.");
        db.AppUsers.Add(new AppUser { Username = username, PinHash = HashPin(pin) });
        await db.SaveChangesAsync();
    }

    public async Task UpdatePinAsync(int userId, string newPin)
    {
        await using var db = factory.CreateDbContext();
        var user = await db.AppUsers.FindAsync(userId)
            ?? throw new InvalidOperationException($"AppUser {userId} not found.");
        user.PinHash = HashPin(newPin);
        await db.SaveChangesAsync();
    }

    public async Task SetActiveAsync(int userId, bool isActive)
    {
        await using var db = factory.CreateDbContext();
        var user = await db.AppUsers.FindAsync(userId)
            ?? throw new InvalidOperationException($"AppUser {userId} not found.");
        user.IsActive = isActive;
        await db.SaveChangesAsync();
    }

    public async Task<bool> AnyUserExistsAsync()
    {
        await using var db = factory.CreateDbContext();
        return await db.AppUsers.AnyAsync();
    }

    private static string HashPin(string pin)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            pin, salt, 100_000, HashAlgorithmName.SHA256, 32);
        var combined = new byte[48];
        salt.CopyTo(combined, 0);
        hash.CopyTo(combined, 16);
        return Convert.ToBase64String(combined);
    }

    private static bool VerifyPin(string pin, string storedHash)
    {
        byte[] combined;
        try { combined = Convert.FromBase64String(storedHash); }
        catch { return false; }
        if (combined.Length != 48) return false;
        var salt     = combined[..16];
        var stored   = combined[16..];
        var computed = Rfc2898DeriveBytes.Pbkdf2(
            pin, salt, 100_000, HashAlgorithmName.SHA256, 32);
        return CryptographicOperations.FixedTimeEquals(stored, computed);
    }
}
