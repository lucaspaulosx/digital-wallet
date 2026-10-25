namespace CarteiraDigital.Api.Domain;

public static class Roles
{
    public const string Customer = "Customer";
    public const string Admin = "Admin";
}

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = Roles.Customer;
    public string Phone { get; set; } = "";
    public bool PhoneVerified { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Account
{
    /// <summary>Conta técnica que origina o dinheiro de boas-vindas. Seu saldo é sempre negativo.</summary>
    public static readonly Guid SystemAccountId = new("00000000-0000-0000-0000-000000000001");

    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public bool IsSystem { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Uma transferência gera sempre dois lançamentos (débito e crédito) que somam zero.</summary>
public class Transfer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid FromAccountId { get; set; }
    public Guid ToAccountId { get; set; }
    public long AmountCents { get; set; }
    public string Description { get; set; } = "";
    public string IdempotencyKey { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Lançamento do razão. Débito é negativo, crédito é positivo. O saldo é a soma dos lançamentos.</summary>
public class LedgerEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TransferId { get; set; }
    public Guid AccountId { get; set; }
    public long AmountCents { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class OtpChallenge
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Salt { get; set; } = "";
    public string CodeHash { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public int Attempts { get; set; }
    public bool Used { get; set; }
}
