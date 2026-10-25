namespace CarteiraDigital.Api;

public record RegisterRequest(string? Name, string? Email, string? Password, string? Phone);
public record LoginRequest(string? Email, string? Password);
public record TokenResponse(string AccessToken, DateTime ExpiresAt);

public record OtpRequestResponse(DateTime ExpiresAt, string? DevCode);
public record VerifyOtpRequest(string? Code);

public record AccountResponse(Guid AccountId, string Name, string Email, decimal Balance, bool PhoneVerified);
public record StatementItemResponse(Guid TransferId, decimal Amount, string Description, DateTime CreatedAt);

public record TransferRequest(string? ToEmail, decimal Amount, string? Description);
public record TransferResponse(Guid Id, decimal Amount, string ToEmail, string Description, DateTime CreatedAt);

public record SimulationRequest(decimal Amount, int Months, decimal MonthlyRatePercent);

public record AdminAccountResponse(Guid AccountId, string Name, string Email, decimal Balance, bool PhoneVerified);
