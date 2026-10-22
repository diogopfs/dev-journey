import { Routes } from '@angular/router';
import { adminGuard, authGuard, guestGuard } from './core/auth/auth.guards';
import { Shell } from './layout/shell/shell';

/**
 * Rotas com lazy loading (loadComponent): o código de cada tela só é baixado quando acessado.
 */
export const routes: Routes = [
  {
    path: 'login',
    canActivate: [guestGuard],
    loadComponent: () => import('./features/auth/login/login').then((m) => m.Login),
  },
  {
    path: 'register',
    canActivate: [guestGuard],
    loadComponent: () => import('./features/auth/register/register').then((m) => m.Register),
  },
  {
    path: '',
    component: Shell,
    canActivate: [authGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'users' },
      {
        path: 'users',
        loadComponent: () => import('./features/users/user-list/user-list').then((m) => m.UserList),
      },
      {
        path: 'users/new',
        canActivate: [adminGuard],
        loadComponent: () => import('./features/users/user-form/user-form').then((m) => m.UserForm),
      },
      {
        path: 'users/:id/edit',
        loadComponent: () => import('./features/users/user-form/user-form').then((m) => m.UserForm),
      },
      {
        path: 'account/password',
        loadComponent: () =>
          import('./features/users/change-password/change-password').then((m) => m.ChangePassword),
      },
      {
        path: 'audit',
        canActivate: [adminGuard],
        loadComponent: () => import('./features/audit/audit-list/audit-list').then((m) => m.AuditList),
      },
    ],
  },
  { path: '**', redirectTo: '' },
];
