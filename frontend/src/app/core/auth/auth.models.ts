export type UserRole = 'User' | 'Admin';

export interface User {
  id: string;
  name: string;
  email: string;
  role: UserRole;
  isActive: boolean;
  createdAt: string;
  updatedAt: string | null;
}

/** Resposta de /api/auth/login e /api/auth/refresh. O refresh token vem em cookie HttpOnly (invisível ao JS). */
export interface TokenResponse {
  accessToken: string;
  expiresAt: string;
  user: User;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  name: string;
  email: string;
  password: string;
}
