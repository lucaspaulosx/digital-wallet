using CarteiraDigital.Core;

namespace CarteiraDigital.Api.Endpoints;

public static class CreditEndpoints
{
    public static void MapCreditEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/credit").WithTags("Crédito").RequireAuthorization();

        group.MapPost("/simulations", (SimulationRequest request) =>
        {
            try
            {
                var result = CreditSimulator.Simulate(request.Amount, request.Months, request.MonthlyRatePercent);
                return Results.Ok(result);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                return Support.Problem(400, "Simulação inválida", ex.Message);
            }
        });
    }
}
