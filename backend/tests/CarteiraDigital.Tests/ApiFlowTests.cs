using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CarteiraDigital.Api;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;

namespace CarteiraDigital.Tests;

/// <summary>Testes de ponta a ponta: sobem a API de verdade, com um banco SQLite temporário.</summary>
public sealed class ApiFlowTests : IDisposable
{
    private const string Password = "Senha#1234";

    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"carteira-test-{Guid.NewGuid():N}.db");
    private readonly WebApplicationFactory<Program> _factory;

    public ApiFlowTests()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseSetting("ConnectionStrings:Default", $"Data Source={_dbPath}"));
    }

    public void Dispose()
    {
        _factory.Dispose();
        SqliteConnection.ClearAllPools();
        try { File.Delete(_dbPath); } catch (IOException) { /* arquivo ainda em uso: o SO limpa a pasta temporária depois */ }
    }

    private async Task<HttpClient> RegisterAndLoginAsync(string email, bool verifyPhone)
    {
        var client = _factory.CreateClient();

        var register = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("Pessoa Teste", email, Password, "(11) 99999-8888"));
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);

        await LoginAsync(client, email, Password);

        if (verifyPhone)
        {
            var otp = await client.PostAsync("/api/auth/otp/request", null);
            otp.EnsureSuccessStatusCode();
            var body = await otp.Content.ReadFromJsonAsync<OtpRequestResponse>();
            Assert.False(string.IsNullOrEmpty(body!.DevCode)); // só em Development

            var verify = await client.PostAsJsonAsync("/api/auth/otp/verify", new VerifyOtpRequest(body.DevCode));
            Assert.Equal(HttpStatusCode.NoContent, verify.StatusCode);
        }

        return client;
    }

    private static async Task LoginAsync(HttpClient client, string email, string password)
    {
        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        login.EnsureSuccessStatusCode();
        var token = await login.Content.ReadFromJsonAsync<TokenResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!.AccessToken);
    }

    private static Task<HttpResponseMessage> TransferAsync(HttpClient client, string toEmail, decimal amount, string? key)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/transfers")
        {
            Content = JsonContent.Create(new TransferRequest(toEmail, amount, "Teste"))
        };
        if (key is not null) request.Headers.Add("Idempotency-Key", key);
        return client.SendAsync(request);
    }

    [Fact]
    public async Task Full_flow_register_verify_transfer_and_replay()
    {
        var ana = await RegisterAndLoginAsync("ana@teste.com", verifyPhone: true);
        await RegisterAndLoginAsync("bia@teste.com", verifyPhone: false);

        var first = await TransferAsync(ana, "bia@teste.com", 100m, "chave-1");
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var replay = await TransferAsync(ana, "bia@teste.com", 100m, "chave-1");
        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        Assert.True(replay.Headers.Contains("Idempotent-Replayed"));

        var account = await ana.GetFromJsonAsync<AccountResponse>("/api/accounts/me");
        Assert.Equal(900m, account!.Balance); // R$ 1.000,00 de boas-vindas menos uma única transferência de R$ 100,00
    }

    [Fact]
    public async Task Transfer_requires_idempotency_key()
    {
        var ana = await RegisterAndLoginAsync("ana@teste.com", verifyPhone: true);
        await RegisterAndLoginAsync("bia@teste.com", verifyPhone: false);

        var response = await TransferAsync(ana, "bia@teste.com", 10m, key: null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Unverified_customer_gets_403_on_transfer()
    {
        var ana = await RegisterAndLoginAsync("ana@teste.com", verifyPhone: false);
        await RegisterAndLoginAsync("bia@teste.com", verifyPhone: false);

        var response = await TransferAsync(ana, "bia@teste.com", 10m, "chave-1");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Endpoints_require_authentication()
    {
        var anonymous = _factory.CreateClient();

        var response = await anonymous.GetAsync("/api/accounts/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Admin_route_is_restricted_by_role()
    {
        var customer = await RegisterAndLoginAsync("ana@teste.com", verifyPhone: false);
        Assert.Equal(HttpStatusCode.Forbidden, (await customer.GetAsync("/api/admin/accounts")).StatusCode);

        var admin = _factory.CreateClient();
        await LoginAsync(admin, "admin@carteira.local", "Admin#12345"); // criado só em Development
        var response = await admin.GetAsync("/api/admin/accounts");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Credit_simulation_returns_installment()
    {
        var ana = await RegisterAndLoginAsync("ana@teste.com", verifyPhone: false);

        var response = await ana.PostAsJsonAsync("/api/credit/simulations", new SimulationRequest(10_000m, 12, 1m));

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Equal(888.49m, body.GetProperty("installment").GetDecimal());
    }
}
