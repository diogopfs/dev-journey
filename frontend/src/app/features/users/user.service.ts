import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { User } from '../../core/auth/auth.models';
import { ChangePasswordRequest, CreateUserRequest, PagedResult, UpdateUserRequest } from './user.models';

/** Camada de acesso à API de usuários: os componentes não conhecem URLs nem HttpClient. */
@Injectable({ providedIn: 'root' })
export class UserService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/users';

  list(search: string, page: number, pageSize: number): Observable<PagedResult<User>> {
    const params = new HttpParams().set('search', search).set('page', page).set('pageSize', pageSize);
    return this.http.get<PagedResult<User>>(this.baseUrl, { params });
  }

  getById(id: string): Observable<User> {
    return this.http.get<User>(`${this.baseUrl}/${id}`);
  }

  create(request: CreateUserRequest): Observable<User> {
    return this.http.post<User>(this.baseUrl, request);
  }

  update(id: string, request: UpdateUserRequest): Observable<User> {
    return this.http.put<User>(`${this.baseUrl}/${id}`, request);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  changePassword(request: ChangePasswordRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/me/password`, request);
  }
}
