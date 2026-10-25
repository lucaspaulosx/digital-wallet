# Carteira Digital

[English version](README.en.md)

Carteira digital (estilo fintech) fullstack: **.NET 8 (Minimal API) + Angular 18**. Projeto de portfólio feito para demonstrar regras de negócio financeiras, segurança básica e testes — não é um produto financeiro real.

## O que faz

- Cadastro com bônus de boas-vindas (R$ 1.000,00 fictício) e login com JWT
- Ativação da conta por código OTP (SMS **simulado**; o código aparece no log da API)
- Saldo e extrato calculados a partir de um **razão de partidas dobradas**
- Transferências entre contas com **idempotência** (`Idempotency-Key`)
- Simulação de crédito (tabela Price)
- Área admin com RBAC (somente perfil `Admin`)

## Como rodar

Requisitos: .NET SDK 8, Node 20.

```bash
# API (http://localhost:5080)
cd backend/src/CarteiraDigital.Api
dotnet run

# Front (http://localhost:4200, proxy /api -> 5080)
cd frontend
npm ci
npm start
```

Admin de desenvolvimento: `admin@carteira.local` / `Admin#12345` (apenas em `appsettings.Development.json`).

Docker: `cp .env.example .env` (defina `JWT_KEY`) e `docker compose up --build` → http://localhost:8080.

## Testes

```bash
dotnet test backend/CarteiraDigital.sln      # unitários + e2e (WebApplicationFactory)
cd frontend && npx ng test --watch=false --browsers=ChromeHeadlessCI
```

## API

| Método | Rota | Descrição |
|---|---|---|
| POST | /api/auth/register, /login | Cadastro e login |
| POST | /api/auth/otp/request, /otp/verify | Código de ativação |
| GET | /api/accounts/me, /me/transactions | Saldo e extrato |
| POST | /api/transfers | Transferência (header `Idempotency-Key` obrigatório) |
| POST | /api/credit/simulations | Simulação de crédito |
| GET | /api/admin/accounts | Somente Admin |

Veja `backend/api.http`, [docs/architecture.md](docs/architecture.md) e [docs/decisions.md](docs/decisions.md) (decisões e **limitações conhecidas**).

## Stack e experiência

Aqui o front é **Angular** (standalone components, signals, reactive forms). Minha experiência anterior de front-end em produção é com **React** (na Lotus, em ambiente corporativo); escolhi Angular neste projeto para demonstrar versatilidade. O backend em .NET segue a stack que uso profissionalmente.

## Limitações

Projeto didático: SQLite sem migrations, OTP simulado, token em `sessionStorage`, sem rate limiting e trava de idempotência em memória (1 instância). Detalhes em `docs/decisions.md`.

## Licença

MIT
