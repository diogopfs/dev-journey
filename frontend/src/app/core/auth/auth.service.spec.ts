import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { TokenResponse } from './auth.models';
import { AuthService } from './auth.service';

const tokenResponse: TokenResponse = {
  accessToken: 'jwt-token',
  expiresAt: new Date(Date.now() + 15 * 60_000).toISOString(),
  user: {
    id: '1',
    name: 'Admin',
    email: 'admin@devjourney.local',
    role: 'Admin',
    isActive: true,
    createdAt: new Date().toISOString(),
    updatedAt: null,
  },
};

describe('AuthService', () => {
  let service: AuthService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    service = TestBed.inject(AuthService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('login deve iniciar a sessão e expor o usuário via signals', () => {
    service.login({ email: 'admin@devjourney.local', password: 'Admin@123' }).subscribe();

    const req = http.expectOne('/api/auth/login');
    expect(req.request.withCredentials).toBe(true);
    req.flush(tokenResponse);

    expect(service.isAuthenticated()).toBe(true);
    expect(service.isAdmin()).toBe(true);
    expect(service.accessToken()).toBe('jwt-token');
  });

  it('refresh com falha deve limpar a sessão e retornar null', () => {
    let result: string | null | undefined;
    service.refresh().subscribe((token) => (result = token));

    http.expectOne('/api/auth/refresh').flush(null, { status: 401, statusText: 'Unauthorized' });

    expect(result).toBeNull();
    expect(service.isAuthenticated()).toBe(false);
  });

  it('chamadas simultâneas de refresh devem reaproveitar a mesma requisição', () => {
    service.refresh().subscribe();
    service.refresh().subscribe();

    http.expectOne('/api/auth/refresh').flush(tokenResponse);
    expect(service.accessToken()).toBe('jwt-token');
  });
});
