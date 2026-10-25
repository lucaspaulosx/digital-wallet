import { Routes } from '@angular/router';
import { adminGuard, authGuard } from './core/guards';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    canActivate: [authGuard],
    loadComponent: () => import('./pages/dashboard.component').then((m) => m.DashboardComponent),
  },
  {
    path: 'transferir',
    canActivate: [authGuard],
    loadComponent: () => import('./pages/transfer.component').then((m) => m.TransferComponent),
  },
  {
    path: 'credito',
    canActivate: [authGuard],
    loadComponent: () => import('./pages/credit.component').then((m) => m.CreditComponent),
  },
  {
    path: 'ativar',
    canActivate: [authGuard],
    loadComponent: () => import('./pages/activate.component').then((m) => m.ActivateComponent),
  },
  {
    path: 'admin',
    canActivate: [authGuard, adminGuard],
    loadComponent: () => import('./pages/admin.component').then((m) => m.AdminComponent),
  },
  {
    path: 'login',
    loadComponent: () => import('./pages/login.component').then((m) => m.LoginComponent),
  },
  {
    path: 'cadastro',
    loadComponent: () => import('./pages/register.component').then((m) => m.RegisterComponent),
  },
  { path: '**', redirectTo: '' },
];
