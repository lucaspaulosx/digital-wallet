import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ApiService } from '../core/api.service';
import { errorMessage } from '../core/errors';
import { Account, StatementItem } from '../core/models';

interface QuickAction {
  label: string;
  icon: string;
  link: string;
  /** Exibe a ação só enquanto o telefone não foi verificado. */
  onlyUnverified?: boolean;
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CurrencyPipe, DatePipe, RouterLink],
  template: `
    @if (error()) {
      <p class="error" role="alert">{{ error() }}</p>
    }

    @if (account(); as acc) {
      <section class="card">
        <p class="muted">Olá, {{ acc.name }}</p>
        <p class="label">Saldo</p>
        <p class="balance">{{ acc.balance | currency: 'BRL' }}</p>

        @if (!acc.phoneVerified) {
          <p class="hint">
            Sua conta ainda não foi ativada. <a routerLink="/ativar">Confirme seu telefone</a> para transferir.
          </p>
        }
      </section>

      <section class="card" aria-labelledby="acoes-titulo">
        <h2 id="acoes-titulo">O que você quer fazer?</h2>
        <div class="actions">
          @for (action of actions; track action.link) {
            @if (!action.onlyUnverified || !acc.phoneVerified) {
              <a class="action" [routerLink]="action.link">
                <span class="action-icon" aria-hidden="true">{{ action.icon }}</span>
                <span>{{ action.label }}</span>
              </a>
            }
          }
        </div>
      </section>

      <section class="card">
        <h2>Extrato</h2>
        @if (statement().length === 0) {
          <p class="muted">Nenhuma movimentação ainda.</p>
        }
        <ul class="statement">
          @for (item of statement(); track item.transferId) {
            <li>
              <div>
                <strong>{{ item.description || 'Transferência' }}</strong>
                <small class="muted">{{ item.createdAt | date: 'dd/MM/yyyy HH:mm' }}</small>
              </div>
              <span [class.credit]="item.amount > 0" [class.debit]="item.amount < 0">
                {{ item.amount | currency: 'BRL' }}
              </span>
            </li>
          }
        </ul>
      </section>
    } @else if (!error()) {
      <p class="muted">Carregando…</p>
    }
  `,
})
export class DashboardComponent {
  private readonly api = inject(ApiService);

  /** Para adicionar uma ação (Saque, Pix, ...), inclua uma linha aqui. */
  readonly actions: QuickAction[] = [
    { label: 'Transferir', icon: '⇄', link: '/transferir' },
    { label: 'Simular crédito', icon: '％', link: '/credito' },
    { label: 'Ativar conta', icon: '✔', link: '/ativar', onlyUnverified: true },
  ];

  readonly account = signal<Account | null>(null);
  readonly statement = signal<StatementItem[]>([]);
  readonly error = signal<string | null>(null);

  constructor() {
    this.api.getAccount().subscribe({
      next: (account) => this.account.set(account),
      error: (err) => this.error.set(errorMessage(err)),
    });
    this.api.getStatement().subscribe({
      next: (items) => this.statement.set(items),
      error: (err) => this.error.set(errorMessage(err)),
    });
  }
}
