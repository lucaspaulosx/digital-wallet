using CarteiraDigital.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace CarteiraDigital.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Transfer> Transfers => Set<Transfer>();
    public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();
    public DbSet<OtpChallenge> OtpChallenges => Set<OtpChallenge>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().Property(u => u.Email).HasMaxLength(256);
        modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();

        modelBuilder.Entity<Account>().HasIndex(a => a.UserId);

        // Garante idempotência também no banco: a mesma chave não gera duas transferências da mesma conta.
        modelBuilder.Entity<Transfer>().Property(t => t.IdempotencyKey).HasMaxLength(100);
        modelBuilder.Entity<Transfer>()
            .HasIndex(t => new { t.FromAccountId, t.IdempotencyKey })
            .IsUnique();

        modelBuilder.Entity<LedgerEntry>().HasIndex(e => e.AccountId);
        modelBuilder.Entity<OtpChallenge>().HasIndex(c => c.UserId);
    }
}
