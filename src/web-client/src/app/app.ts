import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { ToolbarModule } from 'primeng/toolbar';
import { AuthService } from './core/auth/auth.service';

@Component({
  selector: 'app-root',
  imports: [RouterLink, RouterLinkActive, RouterOutlet, ButtonModule, ToolbarModule],
  templateUrl: './core/layout/app-shell.html',
  styleUrl: './core/layout/app-shell.scss'
})
export class App {
  readonly auth = inject(AuthService);
}
