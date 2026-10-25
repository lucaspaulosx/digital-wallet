import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../core/auth.service';
import { errorMessage } from '../core/errors';

@Component({
  selector: 'app-activate',
  standalone: true,
  imports: [ReactiveFormsModule],
  template: `
    <section class="card narrow">
      <h1>Ative sua conta</h1>
      <p class="muted">Enviamos um código de 6 dígitos por SMS. Confirme-o para liberar transferências.</p>

      <button type="button" class="secondary" (click)="requestCode()" [disabled]="sending()">
        {{ sent() ? 'Enviar novo código' : 'Enviar código por SMS' }}
      </button>

      @if (devCode()) {
        <p class="hint">Modo de desenvolvimento: seu código é <strong>{{ devCode() }}</strong>.</p>
      }

      <form [formGroup]="form" (ngSubmit)="verify()">
        <label>Código
          <input type="text" inputmode="numeric" maxlength="6" formControlName="code" autocomplete="one-time-code" />
        </label>
        @if (error()) {
          <p class="error" role="alert">{{ error() }}</p>
        }
        <button type="submit" [disabled]="form.invalid || verifying()">
          {{ verifying() ? 'Verificando…' : 'Confirmar' }}
        </button>
      </form>
    </section>
  `,
})
export class ActivateComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly form = this.fb.nonNullable.group({
    code: ['', [Validators.required, Validators.pattern(/^\d{6}$/)]],
  });
  readonly sent = signal(false);
  readonly sending = signal(false);
  readonly verifying = signal(false);
  readonly devCode = signal<string | null>(null);
  readonly error = signal<string | null>(null);

  requestCode(): void {
    this.sending.set(true);
    this.error.set(null);
    this.auth.requestOtp().subscribe({
      next: (response) => {
        this.sent.set(true);
        this.devCode.set(response.devCode);
        this.sending.set(false);
      },
      error: (err) => {
        this.error.set(errorMessage(err));
        this.sending.set(false);
      },
    });
  }

  verify(): void {
    if (this.form.invalid || this.verifying()) return;
    this.verifying.set(true);
    this.error.set(null);
    this.auth.verifyOtp(this.form.getRawValue().code).subscribe({
      next: () => void this.router.navigate(['/']),
      error: (err) => {
        this.error.set(errorMessage(err));
        this.verifying.set(false);
      },
    });
  }
}
