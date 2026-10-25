namespace CarteiraDigital.Core;

public sealed record CreditSimulationResult(
    decimal Principal,
    int Months,
    decimal MonthlyRatePercent,
    decimal Installment,
    decimal TotalPaid,
    decimal TotalInterest);

/// <summary>
/// Simulação de crédito pela Tabela Price (parcelas fixas):
/// PMT = P * i / (1 - (1 + i)^-n). Com taxa zero, PMT = P / n.
/// É uma simulação didática: não calcula IOF, CET nem tarifas.
/// </summary>
public static class CreditSimulator
{
    public const int MaxMonths = 120;

    public static CreditSimulationResult Simulate(decimal principal, int months, decimal monthlyRatePercent)
    {
        if (principal <= 0)
            throw new ArgumentOutOfRangeException(nameof(principal), "O valor deve ser maior que zero.");
        if (months < 1 || months > MaxMonths)
            throw new ArgumentOutOfRangeException(nameof(months), $"O prazo deve estar entre 1 e {MaxMonths} meses.");
        if (monthlyRatePercent < 0 || monthlyRatePercent > 100)
            throw new ArgumentOutOfRangeException(nameof(monthlyRatePercent), "A taxa mensal deve estar entre 0 e 100.");

        principal = Math.Round(principal, 2, MidpointRounding.AwayFromZero);

        decimal installment;
        if (monthlyRatePercent == 0)
        {
            installment = principal / months;
        }
        else
        {
            var i = (double)(monthlyRatePercent / 100m);
            var factor = i / (1 - Math.Pow(1 + i, -months));
            installment = (decimal)((double)principal * factor);
        }

        installment = Math.Round(installment, 2, MidpointRounding.AwayFromZero);
        var total = installment * months;
        var interest = Math.Max(0m, total - principal);

        return new CreditSimulationResult(principal, months, monthlyRatePercent, installment, total, interest);
    }
}
