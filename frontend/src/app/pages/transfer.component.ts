import { CurrencyPipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ApiService } from '../core/api.service';
import { errorMessage } from '../core/errors';
import { newIdempotencyKey } from '../core/idempotency';
import { TransferResponse } from '../core/models';

@Component({
  selector: 'app-transfer',
  standalone: true,
  imports: [ReactiveFormsModule, CurrencyPipe],
  template: `
    <section class="card narrow">
      <h1>Transferir</h1>
      <form [formGroup]="form" (ngSubmit)="submit()">
        <label>E-mail do destinatário
          <input type="email" formControlName="toEmail" />
        </label>
        <label>Valor (R$)
          <input type="number" step="0.01" min="0.01" formControlName="amount" />
        </label>
        <label>Descrição (opcional)
          <input type="text" maxlength="140" formControlName="description" />
        </label>
        @if (error()) {
          <p class="error" role="alert">{{ error() }}</p>
        }
        @if (done(); as result) {
          <p class="success" role="status">
            Transferência de {{ result.amount | currency: 'BRL' }} para {{ result.toEmail }} concluída.
          </p>
        }
        <button type="submit" [disabled]="form.invalid || loading()">
          {{ loading() ? 'Enviando…' : 'Transferir' }}
        </button>
      </form>
    </section>
  `,
})
export class TransferComponent {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(ApiService);

  readonly form = this.fb.group({
    toEmail: ['', [Validators.required, Validators.email]],
    amount: [null as number | null, [Validators.required, Validators.min(0.01)]],
    description: [''],
  });
  readonly error = signal<string | null>(null);
  readonly loading = signal(false);
  readonly done = signal<TransferResponse | null>(null);

  // A chave só muda quando o usuário altera o formulário ou depois de um sucesso.
  // Assim, clicar de novo ou repetir após um erro de rede não gera uma segunda transferência.
  private idempotencyKey = newIdempotencyKey();

  constructor() {
    this.form.valueChanges.subscribe(() => {
      this.idempotencyKey = newIdempotencyKey();
    });
  }

  submit(): void {
    if (this.form.invalid || this.loading()) return;
    this.loading.set(true);
    this.error.set(null);
    this.done.set(null);

    const value = this.form.getRawValue();
    this.api
      .transfer(
        { toEmail: value.toEmail ?? '', amount: value.amount ?? 0, description: value.description ?? '' },
        this.idempotencyKey,
      )
      .subscribe({
        next: (result) => {
          this.done.set(result);
          this.loading.set(false);
          this.form.reset({ toEmail: '', amount: null, description: '' });
          this.idempotencyKey = newIdempotencyKey();
        },
        error: (err) => {
          this.error.set(errorMessage(err));
          this.loading.set(false);
        },
      });
  }
}
