namespace CarteiraDigital.Core;

/// <summary>
/// Dinheiro é guardado em centavos (long) para evitar erros de arredondamento de ponto flutuante.
/// A API expõe decimal; a conversão acontece só nas bordas.
/// </summary>
public static class Money
{
    /// <summary>Converte um valor em reais para centavos. Rejeita zero, negativos e mais de 2 casas decimais.</summary>
    public static bool TryToCents(decimal amount, out long cents)
    {
        cents = 0;
        if (amount <= 0) return false;

        var scaled = amount * 100m;
        if (scaled != decimal.Truncate(scaled)) return false;
        if (scaled > long.MaxValue / 2) return false;

        cents = (long)scaled;
        return true;
    }

    public static decimal FromCents(long cents) => cents / 100m;
}
