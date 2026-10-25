import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from './core/auth.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <header class="topbar">
      <a class="brand" routerLink="/">Carteira Digital</a>
      @if (auth.isLoggedIn()) {
        <nav>
          <a routerLink="/" routerLinkActive="active" [routerLinkActiveOptions]="{ exact: true }">Painel</a>
          <a routerLink="/transferir" routerLinkActive="active">Transferir</a>
          <a routerLink="/credito" routerLinkActive="active">Crédito</a>
          @if (auth.isAdmin()) {
            <a routerLink="/admin" routerLinkActive="active">Admin</a>
          }
          <button type="button" class="link" (click)="auth.logout()">Sair</button>
        </nav>
      }
    </header>
    <main class="container">
      <router-outlet />
    </main>
    <footer class="footer">Projeto de demonstração: os valores e o SMS são simulados.</footer>
  `,
})
export class AppComponent {
  protected readonly auth = inject(AuthService);
}
