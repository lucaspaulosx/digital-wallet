using System.Net.Mail;
using CarteiraDigital.Api.Data;
using CarteiraDigital.Api.Domain;
using CarteiraDigital.Core;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CarteiraDigital.Api.Services;

public sealed record RegisterResult(bool Success, string? Error);

public class AuthService(
    AppDbContext db,
    IPasswordHasher<User> hasher,
    TokenService tokens,
    LedgerService ledger)
{
    public const long WelcomeBonusCents = 100_000; // R$ 1.000,00 de saldo fictício para a demonstração

    public async Task<RegisterResult> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var name = request.Name?.Trim() ?? "";
        var email = request.Email?.Trim().ToLowerInvariant() ?? "";
        var phone = new string((request.Phone ?? "").Where(char.IsDigit).ToArray());

        if (name.Length < 2 || name.Length > 100) return new(false, "Informe um nome entre 2 e 100 caracteres.");
        if (!MailAddress.TryCreate(email, out _) || email.Length > 256) return new(false, "E-mail inválido.");
        if (!PasswordPolicy.IsValid(request.Password))
            return new(false, $"A senha deve ter ao menos {PasswordPolicy.MinLength} caracteres, com letras e números.");
        if (phone.Length < 10 || phone.Length > 13) return new(false, "Informe um telefone com DDD.");

        if (await db.Users.AnyAsync(u => u.Email == email, ct)) return new(false, "E-mail já cadastrado.");

        var user = new User { Name = name, Email = email, Phone = phone };
        user.PasswordHash = hasher.HashPassword(user, request.Password!);
        var account = new Account { UserId = user.Id };
        db.Users.Add(user);
        db.Accounts.Add(account);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Duas requisições simultâneas com o mesmo e-mail: o índice único barrou a segunda.
            return new(false, "E-mail já cadastrado.");
        }

        await ledger.CreditFromSystemAsync(account.Id, WelcomeBonusCents, "Bônus de boas-vindas", ct);
        return new(true, null);
    }

    public async Task<TokenResponse?> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var email = request.Email?.Trim().ToLowerInvariant() ?? "";
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        if (user is null || string.IsNullOrEmpty(request.Password)) return null;

        var result = hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed) return null;

        var (token, expires) = tokens.Create(user);
        return new TokenResponse(token, expires);
    }
}
