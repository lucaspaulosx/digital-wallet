using CarteiraDigital.Api.Data;
using CarteiraDigital.Api.Domain;
using CarteiraDigital.Api.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CarteiraDigital.Tests;

public sealed class LedgerServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly LedgerService _ledger;

    public LedgerServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();
        DbInitializer.EnsureSystemAccountAsync(_db).GetAwaiter().GetResult();

        _ledger = new LedgerService(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private async Task<Account> CreateCustomerAsync(string email, decimal balance, bool verified = true)
    {
        var user = new User { Name = email, Email = email, Phone = "11999999999", PhoneVerified = verified };
        var account = new Account { UserId = user.Id };
        _db.Users.Add(user);
        _db.Accounts.Add(account);
        await _db.SaveChangesAsync();

        if (balance > 0)
            await _ledger.CreditFromSystemAsync(account.Id, (long)(balance * 100), "Saldo inicial");

        return account;
    }

    [Fact]
    public async Task Transfer_moves_money_between_accounts()
    {
        var ana = await CreateCustomerAsync("ana@teste.com", 500m);
        var bia = await CreateCustomerAsync("bia@teste.com", 0m);

        var outcome = await _ledger.TransferAsync(ana.Id, "bia@teste.com", 120.50m, "k1", "Almoço");

        Assert.True(outcome.Success);
        Assert.False(outcome.Replayed);
        Assert.Equal(37_950, await _ledger.GetBalanceCentsAsync(ana.Id));
        Assert.Equal(12_050, await _ledger.GetBalanceCentsAsync(bia.Id));
    }

    [Fact]
    public async Task Ledger_entries_always_sum_to_zero()
    {
        var ana = await CreateCustomerAsync("ana@teste.com", 500m);
        await CreateCustomerAsync("bia@teste.com", 100m);

        await _ledger.TransferAsync(ana.Id, "bia@teste.com", 10m, "k1", null);
        await _ledger.TransferAsync(ana.Id, "bia@teste.com", 20m, "k2", null);

        Assert.Equal(0, await _db.LedgerEntries.SumAsync(e => e.AmountCents));
    }

    [Fact]
    public async Task Same_idempotency_key_does_not_debit_twice()
    {
        var ana = await CreateCustomerAsync("ana@teste.com", 500m);
        await CreateCustomerAsync("bia@teste.com", 0m);

        var first = await _ledger.TransferAsync(ana.Id, "bia@teste.com", 100m, "mesma-chave", null);
        var second = await _ledger.TransferAsync(ana.Id, "bia@teste.com", 100m, "mesma-chave", null);

        Assert.True(first.Success);
        Assert.True(second.Success);
        Assert.True(second.Replayed);
        Assert.Equal(first.Transfer!.Id, second.Transfer!.Id);
        Assert.Equal(40_000, await _ledger.GetBalanceCentsAsync(ana.Id));
    }

    [Fact]
    public async Task Reusing_a_key_with_different_data_is_a_conflict()
    {
        var ana = await CreateCustomerAsync("ana@teste.com", 500m);
        await CreateCustomerAsync("bia@teste.com", 0m);

        await _ledger.TransferAsync(ana.Id, "bia@teste.com", 100m, "chave", null);
        var conflict = await _ledger.TransferAsync(ana.Id, "bia@teste.com", 200m, "chave", null);

        Assert.False(conflict.Success);
        Assert.Equal(TransferError.IdempotencyKeyReused, conflict.Error);
    }

    [Fact]
    public async Task Insufficient_funds_are_rejected_and_nothing_is_written()
    {
        var ana = await CreateCustomerAsync("ana@teste.com", 50m);
        await CreateCustomerAsync("bia@teste.com", 0m);

        var outcome = await _ledger.TransferAsync(ana.Id, "bia@teste.com", 50.01m, "k1", null);

        Assert.False(outcome.Success);
        Assert.Equal(TransferError.InsufficientFunds, outcome.Error);
        Assert.Equal(5_000, await _ledger.GetBalanceCentsAsync(ana.Id));
    }

    [Fact]
    public async Task Unverified_sender_cannot_transfer()
    {
        var ana = await CreateCustomerAsync("ana@teste.com", 500m, verified: false);
        await CreateCustomerAsync("bia@teste.com", 0m);

        var outcome = await _ledger.TransferAsync(ana.Id, "bia@teste.com", 10m, "k1", null);

        Assert.Equal(TransferError.PhoneNotVerified, outcome.Error);
    }

    [Fact]
    public async Task Cannot_transfer_to_yourself()
    {
        var ana = await CreateCustomerAsync("ana@teste.com", 500m);

        var outcome = await _ledger.TransferAsync(ana.Id, "ana@teste.com", 10m, "k1", null);

        Assert.Equal(TransferError.SameAccount, outcome.Error);
    }

    [Fact]
    public async Task Unknown_recipient_is_reported()
    {
        var ana = await CreateCustomerAsync("ana@teste.com", 500m);

        var outcome = await _ledger.TransferAsync(ana.Id, "ninguem@teste.com", 10m, "k1", null);

        Assert.Equal(TransferError.RecipientNotFound, outcome.Error);
    }

    [Fact]
    public async Task Statement_lists_most_recent_first()
    {
        var ana = await CreateCustomerAsync("ana@teste.com", 500m);
        await CreateCustomerAsync("bia@teste.com", 0m);

        await _ledger.TransferAsync(ana.Id, "bia@teste.com", 10m, "k1", "primeira");
        await Task.Delay(20);
        await _ledger.TransferAsync(ana.Id, "bia@teste.com", 20m, "k2", "segunda");

        var statement = await _ledger.GetStatementAsync(ana.Id, 10);

        Assert.Equal(3, statement.Count);
        Assert.Equal("segunda", statement[0].Description);
        Assert.Equal(-2_000, statement[0].AmountCents);
    }
}
