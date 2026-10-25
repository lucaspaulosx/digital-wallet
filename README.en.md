# Carteira Digital (Digital Wallet)

[Versão em português](README.md)

A fintech-style fullstack wallet: **.NET 8 (Minimal API) + Angular 18**. A portfolio project to demonstrate financial business rules, basic security and testing — not a real financial product.

## Features

- Sign-up with a (fake) R$ 1,000.00 welcome bonus; JWT login
- Account activation by OTP (**simulated** SMS; the code is written to the API log)
- Balance and statement derived from a **double-entry ledger**
- Transfers with **idempotency** (`Idempotency-Key`)
- Credit simulation (Price table)
- Admin area with RBAC (`Admin` role only)

## Running

Requirements: .NET SDK 8, Node 20.

```bash
# API (http://localhost:5080)
cd backend/src/CarteiraDigital.Api
dotnet run

# Frontend (http://localhost:4200, /api proxied to 5080)
cd frontend
npm ci
npm start
```

Dev admin: `admin@carteira.local` / `Admin#12345` (only in `appsettings.Development.json`).

Docker: `cp .env.example .env` (set `JWT_KEY`) then `docker compose up --build` → http://localhost:8080.

## Tests

```bash
dotnet test backend/CarteiraDigital.sln
cd frontend && npx ng test --watch=false --browsers=ChromeHeadlessCI
```

## API

| Method | Route | Description |
|---|---|---|
| POST | /api/auth/register, /login | Sign-up and login |
| POST | /api/auth/otp/request, /otp/verify | Activation code |
| GET | /api/accounts/me, /me/transactions | Balance and statement |
| POST | /api/transfers | Transfer (`Idempotency-Key` header required) |
| POST | /api/credit/simulations | Credit simulation |
| GET | /api/admin/accounts | Admin only |

See `backend/api.http`, [docs/architecture.md](docs/architecture.md) and [docs/decisions.md](docs/decisions.md) (decisions and **known limitations**).

## Stack and experience

The frontend here is **Angular** (standalone components, signals, reactive forms). My previous production front-end experience is with **React** (at Lotus, in a corporate setting); I chose Angular here to show versatility. The .NET backend matches the stack I use professionally.

## Limitations

Educational scope: SQLite without migrations, simulated OTP, token in `sessionStorage`, no rate limiting, in-process idempotency gate (single instance). See `docs/decisions.md`.

## License

MIT
