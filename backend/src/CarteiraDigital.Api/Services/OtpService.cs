using CarteiraDigital.Api.Data;
using CarteiraDigital.Api.Domain;
using CarteiraDigital.Core;
using Microsoft.EntityFrameworkCore;

namespace CarteiraDigital.Api.Services;

/// <summary>Envio de SMS. Aqui é um stub; em produção seria um provedor real.</summary>
public interface ISmsSender
{
    Task SendAsync(string phone, string message, CancellationToken ct = default);
}

public class FakeSmsSender(ILogger<FakeSmsSender> logger, IHostEnvironment env) : ISmsSender
{
    public Task SendAsync(string phone, string message, CancellationToken ct = default)
    {
        // O texto (que contém o código) só vai para o log em Development.
        if (env.IsDevelopment())
            logger.LogInformation("SMS simulado para {Phone}: {Message}", phone, message);
        else
            logger.LogInformation("SMS simulado enviado para {Phone}", phone);
        return Task.CompletedTask;
    }
}

public enum OtpVerifyResult { Ok, NoChallenge, Expired, TooManyAttempts, Invalid }

public sealed record OtpRequestResult(bool Sent, bool Cooldown, DateTime ExpiresAt, string? Code);

public class OtpService(AppDbContext db, ISmsSender sms)
{
    public const int MaxAttempts = 5;
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan Cooldown = TimeSpan.FromSeconds(30);

    public async Task<OtpRequestResult> RequestAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null) return new OtpRequestResult(false, false, DateTime.UtcNow, null);

        var now = DateTime.UtcNow;
        var last = await db.OtpChallenges
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync(ct);
        if (last is not null && now - last.CreatedAt < Cooldown)
            return new OtpRequestResult(false, true, last.ExpiresAt, null);

        // Um novo código invalida os anteriores.
        var pending = await db.OtpChallenges.Where(c => c.UserId == userId && !c.Used).ToListAsync(ct);
        foreach (var p in pending) p.Used = true;

        var code = OtpCode.Generate();
        var salt = OtpCode.NewSalt();
        var challenge = new OtpChallenge
        {
            UserId = userId,
            Salt = salt,
            CodeHash = OtpCode.Hash(code, salt),
            CreatedAt = now,
            ExpiresAt = now.Add(Lifetime)
        };
        db.OtpChallenges.Add(challenge);
        await db.SaveChangesAsync(ct);

        await sms.SendAsync(user.Phone, $"Seu código de verificação é {code}. Ele expira em 5 minutos.", ct);
        return new OtpRequestResult(true, false, challenge.ExpiresAt, code);
    }

    public async Task<OtpVerifyResult> VerifyAsync(Guid userId, string code, CancellationToken ct = default)
    {
        var challenge = await db.OtpChallenges
            .Where(c => c.UserId == userId && !c.Used)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (challenge is null) return OtpVerifyResult.NoChallenge;
        if (challenge.ExpiresAt < DateTime.UtcNow) return OtpVerifyResult.Expired;
        if (challenge.Attempts >= MaxAttempts) return OtpVerifyResult.TooManyAttempts;

        challenge.Attempts++;
        if (!OtpCode.Verify(code, challenge.Salt, challenge.CodeHash))
        {
            await db.SaveChangesAsync(ct);
            return OtpVerifyResult.Invalid;
        }

        challenge.Used = true;
        var user = await db.Users.FirstAsync(u => u.Id == userId, ct);
        user.PhoneVerified = true;
        await db.SaveChangesAsync(ct);
        return OtpVerifyResult.Ok;
    }
}
