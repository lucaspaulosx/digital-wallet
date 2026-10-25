import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { OtpRequestResponse, RegisterRequest, TokenResponse } from './models';

export const TOKEN_STORAGE_KEY = 'carteira.token';

export interface JwtClaims {
  sub?: string;
  email?: string;
  role?: string;
  exp?: number;
}

/** Lê o payload do JWT só para exibir dados na tela. Quem valida o token de verdade é a API. */
export function decodeJwt(token: string): JwtClaims | null {
  try {
    const payload = token.split('.')[1];
    const json = atob(payload.replace(/-/g, '+').replace(/_/g, '/'));
    return JSON.parse(json) as JwtClaims;
  } catch {
    return null;
  }
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  private readonly _token = signal<string | null>(this.readStoredToken());

  readonly token = this._token.asReadonly();
  readonly claims = computed(() => {
    const token = this._token();
    return token ? decodeJwt(token) : null;
  });
  readonly isLoggedIn = computed(() => {
    const claims = this.claims();
    return !!claims && (!claims.exp || claims.exp * 1000 > Date.now());
  });
  readonly isAdmin = computed(() => this.claims()?.role === 'Admin');
  readonly email = computed(() => this.claims()?.email ?? '');

  register(body: RegisterRequest): Observable<void> {
    return this.http.post<void>('/api/auth/register', body);
  }

  login(email: string, password: string): Observable<TokenResponse> {
    return this.http
      .post<TokenResponse>('/api/auth/login', { email, password })
      .pipe(tap((response) => this.setToken(response.accessToken)));
  }

  requestOtp(): Observable<OtpRequestResponse> {
    return this.http.post<OtpRequestResponse>('/api/auth/otp/request', {});
  }

  verifyOtp(code: string): Observable<void> {
    return this.http.post<void>('/api/auth/otp/verify', { code });
  }

  logout(): void {
    this.setToken(null);
    void this.router.navigate(['/login']);
  }

  // sessionStorage: o token some ao fechar a aba. Em produção, o ideal é cookie HttpOnly (ver docs/decisions.md).
  private setToken(token: string | null): void {
    try {
      if (token) sessionStorage.setItem(TOKEN_STORAGE_KEY, token);
      else sessionStorage.removeItem(TOKEN_STORAGE_KEY);
    } catch {
      /* armazenamento indisponível: o token fica só em memória */
    }
    this._token.set(token);
  }

  private readStoredToken(): string | null {
    try {
      return sessionStorage.getItem(TOKEN_STORAGE_KEY);
    } catch {
      return null;
    }
  }
}
