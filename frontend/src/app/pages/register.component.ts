import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../core/auth.service';
import { errorMessage } from '../core/errors';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <section class="card narrow">
      <h1>Criar conta</h1>
      <form [formGroup]="form" (ngSubmit)="submit()">
        <label>Nome
          <input type="text" formControlName="name" autocomplete="name" />
        </label>
        <label>E-mail
          <input type="email" formControlName="email" autocomplete="email" />
        </label>
        <label>Telefone com DDD
          <input type="tel" formControlName="phone" autocomplete="tel" placeholder="(11) 99999-8888" />
        </label>
        <label>Senha
          <input type="password" formControlName="password" autocomplete="new-password" />
          <small class="muted">Mínimo de 8 caracteres, com letras e números.</small>
        </label>
        @if (error()) {
          <p class="error" role="alert">{{ error() }}</p>
        }
        <button type="submit" [disabled]="form.invalid || loading()">
          {{ loading() ? 'Criando…' : 'Criar conta' }}
        </button>
      </form>
      <p class="muted">Já tem conta? <a routerLink="/login">Entrar</a></p>
    </section>
  `,
})
export class RegisterComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.minLength(2)]],
    email: ['', [Validators.required, Validators.email]],
    phone: ['', [Validators.required, Validators.minLength(10)]],
    password: ['', [Validators.required, Validators.minLength(8)]],
  });
  readonly error = signal<string | null>(null);
  readonly loading = signal(false);

  submit(): void {
    if (this.form.invalid || this.loading()) return;
    this.loading.set(true);
    this.error.set(null);

    const value = this.form.getRawValue();
    this.auth.register(value).subscribe({
      // Depois de cadastrar, entra direto e segue para a ativação por código.
      next: () =>
        this.auth.login(value.email, value.password).subscribe({
          next: () => void this.router.navigate(['/ativar']),
          error: (err) => this.fail(err),
        }),
      error: (err) => this.fail(err),
    });
  }

  private fail(err: unknown): void {
    this.error.set(errorMessage(err));
    this.loading.set(false);
  }
}
