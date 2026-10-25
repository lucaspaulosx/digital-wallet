using CarteiraDigital.Api.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CarteiraDigital.Api.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, IConfiguration config)
    {
        var db = services.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();
        await EnsureSystemAccountAsync(db);

        // Admin inicial: só é criado se as duas configurações existirem (appsettings.Development.json ou variáveis de ambiente).
        var adminEmail = config["Seed:AdminEmail"]?.Trim().ToLowerInvariant();
        var adminPassword = config["Seed:AdminPassword"];
        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword)) return;
        if (await db.Users.AnyAsync(u => u.Email == adminEmail)) return;

        var hasher = services.GetRequiredService<IPasswordHasher<User>>();
        var admin = new User { Name = "Administrador", Email = adminEmail, Role = Roles.Admin, PhoneVerified = true };
        admin.PasswordHash = hasher.HashPassword(admin, adminPassword);
        db.Users.Add(admin);
        await db.SaveChangesAsync();
    }

    public static async Task EnsureSystemAccountAsync(AppDbContext db)
    {
        if (await db.Accounts.AnyAsync(a => a.Id == Account.SystemAccountId)) return;
        db.Accounts.Add(new Account { Id = Account.SystemAccountId, IsSystem = true });
        await db.SaveChangesAsync();
    }
}
