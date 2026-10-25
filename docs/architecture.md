# Arquitetura / Architecture

```mermaid
flowchart LR
  U[Browser] --> W[Angular 18 SPA]
  W -- /api, JWT --> A[.NET 8 Minimal API]
  A --> S[Services: Auth, Otp, Ledger, Credit]
  S --> D[(SQLite via EF Core)]
  S --> C[Core: Money, CreditSimulator, OtpCode, PasswordPolicy]
```

- **Core**: regras puras, sem dependências / pure rules, no dependencies.
- **Api**: endpoints finos + serviços; EF Core.
- **Ledger**: toda movimentação gera lançamentos em partidas dobradas (débito/crédito); o saldo é a soma dos lançamentos. / Every movement creates double-entry rows; balance is their sum.

```mermaid
sequenceDiagram
  participant C as Client
  participant API
  participant DB
  C->>API: POST /api/transfers (Idempotency-Key)
  API->>DB: key já existe? / key exists?
  alt nova / new
    API->>DB: Transfer + 2 LedgerEntry (transação)
    API-->>C: 201
  else repetida, mesmo corpo / replay
    API-->>C: 200 + Idempotent-Replayed
  else mesma chave, corpo diferente / same key, different body
    API-->>C: 409
  end
```
