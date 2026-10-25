import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { AuthService, TOKEN_STORAGE_KEY, decodeJwt } from './auth.service';

/** Monta um JWT sem assinatura, só para testar a leitura do payload no front. */
function fakeJwt(payload: object): string {
  const encode = (value: object) => btoa(JSON.stringify(value)).replace(/=/g, '');
  return `${encode({ alg: 'none' })}.${encode(payload)}.assinatura`;
}

describe('decodeJwt', () => {
  it('reads the payload', () => {
    const claims = decodeJwt(fakeJwt({ email: 'ana@teste.com', role: 'Customer' }));
    expect(claims?.email).toBe('ana@teste.com');
    expect(claims?.role).toBe('Customer');
  });

  it('returns null for malformed tokens', () => {
    expect(decodeJwt('nao-e-um-jwt')).toBeNull();
  });
});

describe('AuthService', () => {
  let http: HttpTestingController;

  const create = () => {
    TestBed.configureTestingModule({
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
    return TestBed.inject(AuthService);
  };

  beforeEach(() => sessionStorage.clear());

  it('starts logged out without a stored token', () => {
    const auth = create();
    expect(auth.isLoggedIn()).toBeFalse();
    expect(auth.isAdmin()).toBeFalse();
  });

  it('restores the session from sessionStorage', () => {
    const exp = Math.floor(Date.now() / 1000) + 3600;
    sessionStorage.setItem(TOKEN_STORAGE_KEY, fakeJwt({ email: 'adm@teste.com', role: 'Admin', exp }));

    const auth = create();

    expect(auth.isLoggedIn()).toBeTrue();
    expect(auth.isAdmin()).toBeTrue();
    expect(auth.email()).toBe('adm@teste.com');
  });

  it('treats an expired token as logged out', () => {
    const exp = Math.floor(Date.now() / 1000) - 60;
    sessionStorage.setItem(TOKEN_STORAGE_KEY, fakeJwt({ role: 'Customer', exp }));

    expect(create().isLoggedIn()).toBeFalse();
  });

  it('stores the token after login', () => {
    const auth = create();
    const exp = Math.floor(Date.now() / 1000) + 3600;
    const token = fakeJwt({ email: 'ana@teste.com', role: 'Customer', exp });

    auth.login('ana@teste.com', 'Senha#1234').subscribe();
    http.expectOne('/api/auth/login').flush({ accessToken: token, expiresAt: new Date().toISOString() });

    expect(auth.isLoggedIn()).toBeTrue();
    expect(sessionStorage.getItem(TOKEN_STORAGE_KEY)).toBe(token);
  });

  it('clears the session on logout', () => {
    const exp = Math.floor(Date.now() / 1000) + 3600;
    sessionStorage.setItem(TOKEN_STORAGE_KEY, fakeJwt({ role: 'Customer', exp }));
    const auth = create();
    const navigate = spyOn(TestBed.inject(Router), 'navigate').and.resolveTo(true);

    auth.logout();

    expect(auth.isLoggedIn()).toBeFalse();
    expect(sessionStorage.getItem(TOKEN_STORAGE_KEY)).toBeNull();
    expect(navigate).toHaveBeenCalledWith(['/login']);
  });
});
