using CarteiraDigital.Core;

namespace CarteiraDigital.Tests;

public class CreditSimulatorTests
{
    [Fact]
    public void Price_table_matches_known_installment()
    {
        // R$ 10.000,00 em 12 meses a 1% ao mês => parcela de R$ 888,49 (Tabela Price).
        var result = CreditSimulator.Simulate(10_000m, 12, 1m);

        Assert.Equal(888.49m, result.Installment);
        Assert.Equal(10_661.88m, result.TotalPaid);
        Assert.Equal(661.88m, result.TotalInterest);
    }

    [Fact]
    public void Zero_rate_splits_principal_evenly()
    {
        var result = CreditSimulator.Simulate(1_200m, 12, 0m);

        Assert.Equal(100m, result.Installment);
        Assert.Equal(0m, result.TotalInterest);
    }

    [Theory]
    [InlineData(0, 12, 1)]
    [InlineData(100, 0, 1)]
    [InlineData(100, 121, 1)]
    [InlineData(100, 12, -1)]
    [InlineData(100, 12, 101)]
    public void Invalid_inputs_are_rejected(int amount, int months, int rate)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CreditSimulator.Simulate(amount, months, rate));
    }
}

public class MoneyTests
{
    [Fact]
    public void Converts_reais_to_cents()
    {
        Assert.True(Money.TryToCents(10.50m, out var cents));
        Assert.Equal(1050, cents);
        Assert.Equal(10.50m, Money.FromCents(1050));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Rejects_zero_and_negative(int amount)
    {
        Assert.False(Money.TryToCents(amount, out _));
    }

    [Fact]
    public void Rejects_more_than_two_decimal_places()
    {
        Assert.False(Money.TryToCents(10.505m, out _));
    }
}

public class OtpCodeTests
{
    [Fact]
    public void Generated_code_has_six_digits()
    {
        var code = OtpCode.Generate();

        Assert.Equal(6, code.Length);
        Assert.All(code, c => Assert.True(char.IsDigit(c)));
    }

    [Fact]
    public void Verify_accepts_only_the_original_code()
    {
        var salt = OtpCode.NewSalt();
        var hash = OtpCode.Hash("123456", salt);

        Assert.True(OtpCode.Verify("123456", salt, hash));
        Assert.False(OtpCode.Verify("654321", salt, hash));
    }
}

public class PasswordPolicyTests
{
    [Theory]
    [InlineData("Senha1234", true)]
    [InlineData("curta1", false)]
    [InlineData("somenteletras", false)]
    [InlineData("12345678", false)]
    [InlineData("", false)]
    public void Validates_minimum_rules(string password, bool expected)
    {
        Assert.Equal(expected, PasswordPolicy.IsValid(password));
    }
}
