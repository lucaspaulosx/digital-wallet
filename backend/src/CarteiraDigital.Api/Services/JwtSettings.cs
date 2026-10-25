namespace CarteiraDigital.Api.Services;

public class JwtSettings
{
    public string Issuer { get; set; } = "carteira-digital";
    public string Audience { get; set; } = "carteira-digital-web";

    /// <summary>Chave de assinatura (mínimo 32 caracteres). Em produção vem de variável de ambiente, nunca do repositório.</summary>
    public string Key { get; set; } = "";

    public int ExpiresMinutes { get; set; } = 60;
}
