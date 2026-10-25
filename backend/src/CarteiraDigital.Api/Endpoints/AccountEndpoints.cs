using System.Security.Claims;
using CarteiraDigital.Api.Data;
using CarteiraDigital.Api.Services;
using CarteiraDigital.Core;
using Microsoft.EntityFrameworkCore;

namespace CarteiraDigital.Api.Endpoints;

public static class AccountEndpoints
{
    public static void MapAccountEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/accounts").WithTags("Contas").RequireAuthorization();

        group.MapGet("/me", async (ClaimsPrincipal principal, AppDbContext db, LedgerService ledger, CancellationToken ct) =>
        {
            var userId = principal.GetUserId();
            var account = await db.Accounts.Include(a => a.User)
                .FirstOrDefaultAsync(a => a.UserId == userId && !a.IsSystem, ct);
            if (account?.User is null) return Support.Problem(404, "Não encontrado", "Este usuário não possui conta.");

            var balance = await ledger.GetBalanceCentsAsync(account.Id, ct);
            return Results.Ok(new AccountResponse(
                account.Id, account.User.Name, account.User.Email, Money.FromCents(balance), account.User.PhoneVerified));
        });

        group.MapGet("/me/transactions", async (ClaimsPrincipal principal, AppDbContext db, LedgerService ledger, int? take, CancellationToken ct) =>
        {
            var userId = principal.GetUserId();
            var account = await db.Accounts.FirstOrDefaultAsync(a => a.UserId == userId && !a.IsSystem, ct);
            if (account is null) return Support.Problem(404, "Não encontrado", "Este usuário não possui conta.");

            var items = await ledger.GetStatementAsync(account.Id, take ?? 20, ct);
            return Results.Ok(items.Select(i =>
                new StatementItemResponse(i.TransferId, Money.FromCents(i.AmountCents), i.Description, i.CreatedAt)));
        });
    }
}
