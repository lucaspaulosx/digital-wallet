using CarteiraDigital.Api.Data;
using CarteiraDigital.Core;
using Microsoft.EntityFrameworkCore;

namespace CarteiraDigital.Api.Endpoints;

public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this WebApplication app)
    {
        // RBAC: só o papel Admin acessa. A checagem é feita no servidor; o front apenas esconde o que não se aplica.
        var group = app.MapGroup("/api/admin").WithTags("Administração").RequireAuthorization("AdminOnly");

        group.MapGet("/accounts", async (AppDbContext db, CancellationToken ct) =>
        {
            var balances = await db.LedgerEntries
                .GroupBy(e => e.AccountId)
                .Select(g => new { AccountId = g.Key, Balance = g.Sum(x => x.AmountCents) })
                .ToListAsync(ct);
            var byAccount = balances.ToDictionary(b => b.AccountId, b => b.Balance);

            var accounts = await db.Accounts.Include(a => a.User)
                .Where(a => !a.IsSystem)
                .ToListAsync(ct);

            var response = accounts
                .Where(a => a.User is not null)
                .Select(a => new AdminAccountResponse(
                    a.Id, a.User!.Name, a.User.Email,
                    Money.FromCents(byAccount.GetValueOrDefault(a.Id)), a.User.PhoneVerified))
                .OrderBy(a => a.Name)
                .ToList();

            return Results.Ok(response);
        });
    }
}
