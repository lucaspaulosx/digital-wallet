using CarteiraDigital.Api.Data;
using CarteiraDigital.Api.Domain;
using CarteiraDigital.Core;
using Microsoft.EntityFrameworkCore;

namespace CarteiraDigital.Api.Services;

public enum TransferError
{
    None,
    InvalidAmount,
    AccountNotFound,
    PhoneNotVerified,
    RecipientNotFound,
    SameAccount,
    InsufficientFunds,
    IdempotencyKeyReused
}

public sealed record TransferOutcome(bool Success, TransferError Error, Transfer? Transfer, bool Replayed);

public sealed record StatementItem(Guid TransferId, long AmountCents, string Description, DateTime CreatedAt);

public class LedgerService(AppDbContext db)
{
    // Serializa as transferências dentro de uma instância da API para que "checar saldo" e "gravar"
    // não corram em paralelo. Com mais de uma instância seria preciso lock no banco (ver docs/decisions.md).
    private static readonly SemaphoreSlim Gate = new(1, 1);

    public async Task<long> GetBalanceCentsAsync(Guid accountId, CancellationToken ct = default) =>
        await db.LedgerEntries.Where(e => e.AccountId == accountId).SumAsync(e => e.AmountCents, ct);

    public async Task<List<StatementItem>> GetStatementAsync(Guid accountId, int take, CancellationToken ct = default)
    {
        take = Math.Clamp(take, 1, 100);

        var query =
            from e in db.LedgerEntries
            where e.AccountId == accountId
            join t in db.Transfers on e.TransferId equals t.Id
            orderby e.CreatedAt descending
            select new StatementItem(t.Id, e.AmountCents, t.Description, e.CreatedAt);

        return await query.Take(take).ToListAsync(ct);
    }

    /// <summary>Crédito de boas-vindas vindo da conta do sistema. Idempotente por conta.</summary>
    public async Task CreditFromSystemAsync(Guid accountId, long cents, string description, CancellationToken ct = default)
    {
        await Gate.WaitAsync(ct);
        try
        {
            var key = $"system-credit:{accountId}";
            if (await db.Transfers.AnyAsync(t => t.FromAccountId == Account.SystemAccountId && t.IdempotencyKey == key, ct))
                return;

            AddTransfer(Account.SystemAccountId, accountId, cents, description, key);
            await db.SaveChangesAsync(ct);
        }
        finally
        {
            Gate.Release();
        }
    }

    public async Task<TransferOutcome> TransferAsync(
        Guid fromAccountId,
        string toEmail,
        decimal amount,
        string idempotencyKey,
        string? description,
        CancellationToken ct = default)
    {
        if (!Money.TryToCents(amount, out var cents))
            return Fail(TransferError.InvalidAmount);

        await Gate.WaitAsync(ct);
        try
        {
            var source = await db.Accounts.Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Id == fromAccountId && !a.IsSystem, ct);
            if (source?.User is null) return Fail(TransferError.AccountNotFound);
            if (!source.User.PhoneVerified) return Fail(TransferError.PhoneNotVerified);

            var email = toEmail.Trim().ToLowerInvariant();
            var target = await db.Accounts.FirstOrDefaultAsync(a => !a.IsSystem && a.User!.Email == email, ct);
            if (target is null) return Fail(TransferError.RecipientNotFound);
            if (target.Id == source.Id) return Fail(TransferError.SameAccount);

            // Repetir a mesma requisição (mesma chave) devolve o resultado original sem debitar de novo.
            var existing = await FindByKeyAsync(source.Id, idempotencyKey, ct);
            if (existing is not null)
                return ReplayOrConflict(existing, target.Id, cents);

            var balance = await GetBalanceCentsAsync(source.Id, ct);
            if (balance < cents) return Fail(TransferError.InsufficientFunds);

            var transfer = AddTransfer(source.Id, target.Id, cents, description?.Trim() ?? "", idempotencyKey);
            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                // Corrida entre instâncias: o índice único barrou a duplicata. Devolve a transferência vencedora.
                db.ChangeTracker.Clear();
                var winner = await FindByKeyAsync(source.Id, idempotencyKey, ct);
                if (winner is null) throw;
                return ReplayOrConflict(winner, target.Id, cents);
            }

            return new TransferOutcome(true, TransferError.None, transfer, false);
        }
        finally
        {
            Gate.Release();
        }
    }

    private Task<Transfer?> FindByKeyAsync(Guid fromAccountId, string key, CancellationToken ct) =>
        db.Transfers.FirstOrDefaultAsync(t => t.FromAccountId == fromAccountId && t.IdempotencyKey == key, ct);

    private static TransferOutcome ReplayOrConflict(Transfer existing, Guid toAccountId, long cents) =>
        existing.ToAccountId == toAccountId && existing.AmountCents == cents
            ? new TransferOutcome(true, TransferError.None, existing, true)
            : Fail(TransferError.IdempotencyKeyReused);

    private Transfer AddTransfer(Guid fromId, Guid toId, long cents, string description, string key)
    {
        var now = DateTime.UtcNow;
        var transfer = new Transfer
        {
            FromAccountId = fromId,
            ToAccountId = toId,
            AmountCents = cents,
            Description = description,
            IdempotencyKey = key,
            CreatedAt = now
        };

        db.Transfers.Add(transfer);
        db.LedgerEntries.Add(new LedgerEntry { TransferId = transfer.Id, AccountId = fromId, AmountCents = -cents, CreatedAt = now });
        db.LedgerEntries.Add(new LedgerEntry { TransferId = transfer.Id, AccountId = toId, AmountCents = cents, CreatedAt = now });
        return transfer;
    }

    private static TransferOutcome Fail(TransferError error) => new(false, error, null, false);
}
