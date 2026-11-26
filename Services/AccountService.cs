using HumaHr.Data;
using HumaHr.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HumaHr.Services;

public class AccountService(HumaHrDbContext db)
{
    private readonly PasswordHasher<AppUser> hasher = new();

    public AppUser? Find(string email) => db.AppUsers.FirstOrDefault(x => x.Email == Normalize(email));

    public AppUser? Find(int userId) => db.AppUsers.FirstOrDefault(x => x.Id == userId);

    public AppUser? ForEmployee(int employeeId) => db.AppUsers.FirstOrDefault(x => x.EmployeeId == employeeId);

    public bool EmailExists(string email) => db.AppUsers.Any(x => x.Email == Normalize(email));
    public bool EmailInUseByOtherEmployee(string email, int employeeId) => db.AppUsers.Any(x => x.Email == Normalize(email) && x.EmployeeId != employeeId);

    public string CreateEmployeeAccount(int employeeId, string email, string fullName)
    {
        var temporaryPassword = GenerateTemporaryPassword();
        var user = new AppUser
        {
            EmployeeId = employeeId,
            Email = Normalize(email),
            FullName = fullName.Trim(),
            Role = "Employee",
            MustChangePassword = true
        };
        user.PasswordHash = hasher.HashPassword(user, temporaryPassword);
        db.AppUsers.Add(user);
        db.SaveChanges();
        return temporaryPassword;
    }

    public string? ResetEmployeePassword(int employeeId)
    {
        var user = ForEmployee(employeeId);
        if (user is null) return null;
        var temporaryPassword = GenerateTemporaryPassword();
        user.PasswordHash = hasher.HashPassword(user, temporaryPassword);
        user.MustChangePassword = true;
        db.SaveChanges();
        return temporaryPassword;
    }

    public bool ChangePassword(AppUser user, string currentPassword, string newPassword)
    {
        if (!Verify(user, currentPassword)) return false;
        user.PasswordHash = hasher.HashPassword(user, newPassword);
        user.MustChangePassword = false;
        db.SaveChanges();
        return true;
    }

    public void SetEmployeeActive(int employeeId, bool active)
    {
        var account = ForEmployee(employeeId);
        if (account is null) return;
        account.IsEnabled = active;
        db.SaveChanges();
    }

    private void Create(string email, string fullName, string password)
    {
        email = Normalize(email);
        var user = new AppUser { Email = email, FullName = fullName.Trim(), Role = "Admin", MustChangePassword = false };
        user.PasswordHash = hasher.HashPassword(user, password);
        db.AppUsers.Add(user);
        db.SaveChanges();
    }

    public void EnsureInitialAdmin(string email, string fullName, string password)
    {
        var adminEmail = Normalize(email);
        var admin = db.AppUsers.FirstOrDefault(x => x.Email == adminEmail);
        if (admin is null)
        {
            if (db.AppUsers.Any()) return;
            Create(email, fullName, password);
            admin = db.AppUsers.First(x => x.Email == adminEmail);
        }
        admin.Role = "Admin";
        admin.EmployeeId = null;
        admin.IsEnabled = true;
        db.SaveChanges();
    }

    public bool Verify(AppUser user, string password) =>
        hasher.VerifyHashedPassword(user, user.PasswordHash, password) != PasswordVerificationResult.Failed;

    public bool IsEmployeeActive(int employeeId) => db.Employees.Any(x => x.Id == employeeId && x.IsActive);

    public void UpgradePasswordHash(AppUser user, string password)
    {
        if (hasher.VerifyHashedPassword(user, user.PasswordHash, password) == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = hasher.HashPassword(user, password);
            db.SaveChanges();
        }
    }

    public bool EmployeeAccountExists(int employeeId) => db.AppUsers.Any(x => x.EmployeeId == employeeId);

    public void SyncEmployeeAccount(int employeeId, string email, string fullName)
    {
        var account = ForEmployee(employeeId);
        if (account is null) return;
        account.Email = Normalize(email);
        account.FullName = fullName.Trim();
        db.SaveChanges();
    }

    private static string Normalize(string email) => email.Trim().ToLowerInvariant();

    private static string GenerateTemporaryPassword() => Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(9))
        .Replace("+", "A").Replace("/", "B").TrimEnd('=') + "a1!";
}
