using System.Security.Claims;
using CarteiraDigital.Api.Data;
using CarteiraDigital.Api.Services;
using CarteiraDigital.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarteiraDigital.Api.Endpoints;

public static class TransferEndpoints
{
    public static void MapTransferEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/transfers").WithTags("Transferências").RequireAuthorization();

        // O cabeçalho Idempotency-Key é obrigatório: se o cliente repetir a requisição (timeout, duplo clique),
        // a transferência não é executada duas vezes.
        group.MapPost("/", async (
            TransferRequest request,
            [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
            ClaimsPrincipal principal,
            AppDbContext db,
            LedgerService ledger,
            HttpResponse response,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Length > 100)
                return Support.Problem(400, "Requisição inválida", "Envie o cabeçalho Idempotency-Key (até 100 caracteres).");
            if (string.IsNullOrWhiteSpace(request.ToEmail))
                return Support.Problem(400, "Requisição inválida", "Informe o e-mail do destinatário.");

            var userId = principal.GetUserId();
            var account = await db.Accounts.FirstOrDefaultAsync(a => a.UserId == userId && !a.IsSystem, ct);
            if (account is null) return Support.Problem(404, "Não encontrado", "Este usuário não possui conta.");

            var outcome = await ledger.TransferAsync(
                account.Id, request.ToEmail, request.Amount, idempotencyKey.Trim(), request.Description, ct);

            if (!outcome.Success)
            {
                return outcome.Error switch
                {
                    TransferError.InvalidAmount => Support.Problem(400, "Valor inválido", "Informe um valor maior que zero, com no máximo 2 casas decimais."),
                    TransferError.PhoneNotVerified => Support.Problem(403, "Conta não ativada", "Valide seu telefone para transferir."),
                    TransferError.RecipientNotFound => Support.Problem(404, "Destinatário não encontrado", "Não existe conta para este e-mail."),
                    TransferError.SameAccount => Support.Problem(400, "Requisição inválida", "Não é possível transferir para a própria conta."),
                    TransferError.InsufficientFunds => Support.Problem(422, "Saldo insuficiente", "O saldo não cobre o valor da transferência."),
                    TransferError.IdempotencyKeyReused => Support.Problem(409, "Conflito", "Esta Idempotency-Key já foi usada com outros dados."),
                    _ => Support.Problem(400, "Requisição inválida", "Não foi possível concluir a transferência.")
                };
            }

            var transfer = outcome.Transfer!;
            if (outcome.Replayed) response.Headers["Idempotent-Replayed"] = "true";

            var body = new TransferResponse(
                transfer.Id, Money.FromCents(transfer.AmountCents), request.ToEmail.Trim().ToLowerInvariant(),
                transfer.Description, transfer.CreatedAt);

            return outcome.Replayed ? Results.Ok(body) : Results.Created($"/api/transfers/{transfer.Id}", body);
        });
    }
}
