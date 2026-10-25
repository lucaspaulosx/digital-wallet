using CarteiraDigital.Api.Services;

namespace CarteiraDigital.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Autenticação");

        group.MapPost("/register", async (RegisterRequest request, AuthService auth, CancellationToken ct) =>
        {
            var result = await auth.RegisterAsync(request, ct);
            return result.Success
                ? Results.StatusCode(StatusCodes.Status201Created)
                : Support.Problem(400, "Cadastro inválido", result.Error!);
        });

        group.MapPost("/login", async (LoginRequest request, AuthService auth, CancellationToken ct) =>
        {
            var token = await auth.LoginAsync(request, ct);
            return token is null
                ? Support.Problem(401, "Não autorizado", "Credenciais inválidas.")
                : Results.Ok(token);
        });

        // Etapa de ativação: o cliente pede um código por SMS e o confirma para liberar transferências.
        group.MapPost("/otp/request", async (System.Security.Claims.ClaimsPrincipal user, OtpService otp, IHostEnvironment env, CancellationToken ct) =>
        {
            var result = await otp.RequestAsync(user.GetUserId(), ct);
            if (result.Cooldown)
                return Support.Problem(429, "Aguarde", "Aguarde alguns segundos antes de pedir um novo código.");
            if (!result.Sent)
                return Support.Problem(404, "Não encontrado", "Usuário não encontrado.");

            // O código só é devolvido em Development, para a demonstração funcionar sem um provedor de SMS.
            return Results.Ok(new OtpRequestResponse(result.ExpiresAt, env.IsDevelopment() ? result.Code : null));
        }).RequireAuthorization();

        group.MapPost("/otp/verify", async (VerifyOtpRequest request, System.Security.Claims.ClaimsPrincipal user, OtpService otp, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Code))
                return Support.Problem(400, "Código inválido", "Informe o código recebido.");

            var result = await otp.VerifyAsync(user.GetUserId(), request.Code.Trim(), ct);
            return result switch
            {
                OtpVerifyResult.Ok => Results.NoContent(),
                OtpVerifyResult.NoChallenge => Support.Problem(400, "Código inválido", "Peça um novo código."),
                OtpVerifyResult.Expired => Support.Problem(400, "Código expirado", "Peça um novo código."),
                OtpVerifyResult.TooManyAttempts => Support.Problem(429, "Tentativas esgotadas", "Peça um novo código."),
                _ => Support.Problem(400, "Código inválido", "O código informado está incorreto.")
            };
        }).RequireAuthorization();
    }
}
