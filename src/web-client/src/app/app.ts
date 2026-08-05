import { Component, inject } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { ToolbarModule } from 'primeng/toolbar';
import { AuthService } from './auth/auth.service';

@Component({
  selector: 'app-root',
  imports: [RouterLink, RouterOutlet, ButtonModule, ToolbarModule],
  templateUrl: './app-shell.html',
  styleUrl: './app.scss'
})
export class App {
  readonly auth = inject(AuthService);
}
