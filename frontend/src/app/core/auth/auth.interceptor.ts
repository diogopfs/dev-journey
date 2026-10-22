import { HttpErrorResponse, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService, authEndpoints } from './auth.service';

const withToken = (req: HttpRequest<unknown>, token: string | null) =>
  token ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : req;

/**
 * Interceptor funcional:
 * 1. Anexa o header "Authorization: Bearer <token>" nas chamadas à API;
 * 2. Se a API responder 401 (token expirado), tenta o refresh UMA vez e repete a requisição;
 * 3. Se o refresh falhar, encerra a sessão e leva o usuário ao login.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);

  const isApi = req.url.startsWith('/api/');
  const isAuthEndpoint = authEndpoints.some((url) => req.url.startsWith(url));
  if (!isApi || isAuthEndpoint) return next(req);

  return next(withToken(req, auth.accessToken())).pipe(
    catchError((error: unknown) => {
      if (!(error instanceof HttpErrorResponse) || error.status !== 401) return throwError(() => error);

      return auth.refresh().pipe(
        switchMap((newToken) => {
          if (!newToken) {
            auth.endSession();
            return throwError(() => error);
          }
          return next(withToken(req, newToken));
        }),
      );
    }),
  );
};
