namespace CarteiraDigital.Core;

public static class PasswordPolicy
{
    public const int MinLength = 8;

    /// <summary>Mínimo de 8 caracteres, com pelo menos uma letra e um número.</summary>
    public static bool IsValid(string? password) =>
        !string.IsNullOrEmpty(password)
        && password.Length >= MinLength
        && password.Any(char.IsLetter)
        && password.Any(char.IsDigit);
}
