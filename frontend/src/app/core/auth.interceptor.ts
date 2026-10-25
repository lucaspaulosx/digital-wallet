import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { AuthService } from './auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const token = auth.token();

  const request =
    token && req.url.startsWith('/api/')
      ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
      : req;

  return next(request).pipe(
    catchError((error: HttpErrorResponse) => {
      // Token expirado ou inválido: volta para o login. O próprio login com senha errada também devolve 401, e não deve deslogar.
      if (error.status === 401 && token && !req.url.endsWith('/api/auth/login')) auth.logout();
      return throwError(() => error);
    }),
  );
};
