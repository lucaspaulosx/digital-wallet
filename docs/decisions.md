# Decisões e limitações / Decisions and limitations

| # | Decisão / Decision | Motivo / Why | Limitação / Limitation |
|---|---|---|---|
| 1 | Dinheiro em centavos (`long`) / money in cents | Evita erro de ponto flutuante | Sem multi-moeda |
| 2 | Razão com partidas dobradas / double-entry ledger | Saldo auditável; soma dos lançamentos = 0 | Saldo calculado por SUM (sem cache) |
| 3 | `Idempotency-Key` em transferências | Retentativas seguras | Trava em memória (`SemaphoreSlim`): só 1 instância. Em produção: lock no banco/Redis |
| 4 | SQLite + `EnsureCreated` | Rodar sem infraestrutura | Sem migrations; produção usaria PostgreSQL + migrations |
| 5 | JWT em `sessionStorage` | Simples para o demo | Exposto a XSS; produção: cookie HttpOnly + CSRF |
| 6 | OTP por SMS simulado | Mostrar fluxo de verificação (hash+salt, expiração, tentativas, cooldown) | Código vai para o log, não há provedor real |
| 7 | Sem rate limiting | Escopo | Produção: limitar login/OTP por IP e conta |
| 8 | Registro revela e-mail existente | Simplicidade | Enumeração de usuários |
| 9 | Credenciais admin só em `appsettings.Development.json` | Demo local | Nunca usar em produção; `Jwt:Key` exigida (≥32) fora do dev |

Projeto de portfólio, **não** é um produto financeiro real. / Portfolio project, **not** a real financial product.
