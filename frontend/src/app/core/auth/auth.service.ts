import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, catchError, finalize, map, of, shareReplay, tap } from 'rxjs';
import { LoginRequest, RegisterRequest, TokenResponse, User } from './auth.models';

interface Session {
  accessToken: string;
  expiresAt: Date;
  user: User;
}

/**
 * Estado de autenticação baseado em Signals.
 *
 * Decisão de segurança: o access token fica SOMENTE em memória (não em localStorage),
 * reduzindo o impacto de um ataque XSS. Ao recarregar a página, a sessão é restaurada
 * chamando /api/auth/refresh, que usa o cookie HttpOnly com o refresh token.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  private readonly session = signal<Session | null>(null);
  private refreshInFlight$: Observable<string | null> | null = null;

  readonly user = computed(() => this.session()?.user ?? null);
  readonly isAuthenticated = computed(() => this.session() !== null);
  readonly isAdmin = computed(() => this.session()?.user.role === 'Admin');
  readonly accessToken = computed(() => this.session()?.accessToken ?? null);

  login(request: LoginRequest): Observable<User> {
    return this.http
      .post<TokenResponse>('/api/auth/login', request, { withCredentials: true })
      .pipe(map((response) => this.startSession(response)));
  }

  register(request: RegisterRequest): Observable<User> {
    return this.http.post<User>('/api/auth/register', request);
  }

  /**
   * Obtém um novo access token. Se várias requisições receberem 401 ao mesmo tempo,
   * todas reaproveitam a MESMA chamada de refresh (shareReplay), evitando invalidar a rotação.
   */
  refresh(): Observable<string | null> {
    this.refreshInFlight$ ??= this.http
      .post<TokenResponse>('/api/auth/refresh', null, { withCredentials: true })
      .pipe(
        map((response) => {
          this.startSession(response);
          return response.accessToken;
        }),
        catchError(() => {
          this.session.set(null);
          return of(null);
        }),
        finalize(() => (this.refreshInFlight$ = null)),
        shareReplay(1),
      );
    return this.refreshInFlight$;
  }

  /** Executado na inicialização da aplicação para restaurar a sessão (se o cookie existir). */
  restoreSession(): Observable<unknown> {
    return this.refresh();
  }

  logout(): void {
    this.http
      .post('/api/auth/logout', null, { withCredentials: true })
      .pipe(catchError(() => of(null)))
      .subscribe();
    this.endSession();
  }

  endSession(): void {
    this.session.set(null);
    void this.router.navigate(['/login']);
  }

  updateCurrentUser(user: User): void {
    const current = this.session();
    if (current && current.user.id === user.id) this.session.set({ ...current, user });
  }

  private startSession(response: TokenResponse): User {
    this.session.set({
      accessToken: response.accessToken,
      expiresAt: new Date(response.expiresAt),
      user: response.user,
    });
    return response.user;
  }
}

export const authEndpoints = ['/api/auth/login', '/api/auth/refresh', '/api/auth/register', '/api/auth/logout'];
