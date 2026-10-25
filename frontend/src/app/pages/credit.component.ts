import { CurrencyPipe, DecimalPipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ApiService } from '../core/api.service';
import { errorMessage } from '../core/errors';
import { SimulationResult } from '../core/models';

@Component({
  selector: 'app-credit',
  standalone: true,
  imports: [ReactiveFormsModule, CurrencyPipe, DecimalPipe],
  template: `
    <section class="card narrow">
      <h1>Simular crédito</h1>
      <p class="muted">Parcelas fixas (Tabela Price). Simulação didática, sem IOF, CET ou tarifas.</p>
      <form [formGroup]="form" (ngSubmit)="submit()">
        <label>Valor desejado (R$)
          <input type="number" step="0.01" min="0.01" formControlName="amount" />
        </label>
        <label>Prazo (meses)
          <input type="number" step="1" min="1" max="120" formControlName="months" />
        </label>
        <label>Taxa mensal (%)
          <input type="number" step="0.01" min="0" max="100" formControlName="monthlyRatePercent" />
        </label>
        @if (error()) {
          <p class="error" role="alert">{{ error() }}</p>
        }
        <button type="submit" [disabled]="form.invalid || loading()">Simular</button>
      </form>
    </section>

    @if (result(); as r) {
      <section class="card narrow">
        <h2>Resultado</h2>
        <dl class="summary">
          <dt>Parcela</dt><dd>{{ r.installment | currency: 'BRL' }} × {{ r.months }}</dd>
          <dt>Total pago</dt><dd>{{ r.totalPaid | currency: 'BRL' }}</dd>
          <dt>Juros totais</dt><dd>{{ r.totalInterest | currency: 'BRL' }}</dd>
          <dt>Taxa mensal</dt><dd>{{ r.monthlyRatePercent | number: '1.2-2' }}%</dd>
        </dl>
      </section>
    }
  `,
})
export class CreditComponent {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(ApiService);

  readonly form = this.fb.nonNullable.group({
    amount: [10000, [Validators.required, Validators.min(0.01)]],
    months: [12, [Validators.required, Validators.min(1), Validators.max(120)]],
    monthlyRatePercent: [1.5, [Validators.required, Validators.min(0), Validators.max(100)]],
  });
  readonly result = signal<SimulationResult | null>(null);
  readonly error = signal<string | null>(null);
  readonly loading = signal(false);

  submit(): void {
    if (this.form.invalid || this.loading()) return;
    this.loading.set(true);
    this.error.set(null);
    this.api.simulate(this.form.getRawValue()).subscribe({
      next: (result) => {
        this.result.set(result);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(errorMessage(err));
        this.result.set(null);
        this.loading.set(false);
      },
    });
  }
}
