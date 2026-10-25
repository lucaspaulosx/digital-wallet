import { CurrencyPipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { ApiService } from '../core/api.service';
import { errorMessage } from '../core/errors';
import { AdminAccount } from '../core/models';

@Component({
  selector: 'app-admin',
  standalone: true,
  imports: [CurrencyPipe],
  template: `
    <section class="card">
      <h1>Contas</h1>
      @if (error()) {
        <p class="error" role="alert">{{ error() }}</p>
      }
      <table>
        <thead>
          <tr><th>Nome</th><th>E-mail</th><th>Ativada</th><th class="num">Saldo</th></tr>
        </thead>
        <tbody>
          @for (a of accounts(); track a.accountId) {
            <tr>
              <td>{{ a.name }}</td>
              <td>{{ a.email }}</td>
              <td>{{ a.phoneVerified ? 'Sim' : 'Não' }}</td>
              <td class="num">{{ a.balance | currency: 'BRL' }}</td>
            </tr>
          }
        </tbody>
      </table>
    </section>
  `,
})
export class AdminComponent {
  private readonly api = inject(ApiService);

  readonly accounts = signal<AdminAccount[]>([]);
  readonly error = signal<string | null>(null);

  constructor() {
    this.api.adminAccounts().subscribe({
      next: (accounts) => this.accounts.set(accounts),
      error: (err) => this.error.set(errorMessage(err)),
    });
  }
}
