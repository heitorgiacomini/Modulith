import { Injectable, signal } from '@angular/core';
import Keycloak, { KeycloakProfile } from 'keycloak-js';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly keycloak = new Keycloak(environment.keycloak);
  readonly authenticated = signal(false);
  readonly userName = signal<string | null>(null);
  readonly customerId = signal<string | null>(null);

  async initialize(): Promise<void> {
    const authenticated = await this.keycloak.init({
      onLoad: 'check-sso',
      pkceMethod: 'S256',
      checkLoginIframe: false
    });
    this.updateIdentity(authenticated);

    this.keycloak.onAuthSuccess = () => this.updateIdentity(true);
    this.keycloak.onAuthLogout = () => this.updateIdentity(false);
    this.keycloak.onTokenExpired = () => {
      void this.keycloak.updateToken(30).catch(() => this.login());
    };
  }

  login(): Promise<void> {
    return this.keycloak.login({ redirectUri: window.location.href });
  }

  logout(): Promise<void> {
    return this.keycloak.logout({ redirectUri: window.location.origin });
  }

  async token(): Promise<string | null> {
    if (!this.keycloak.authenticated) {
      return null;
    }

    await this.keycloak.updateToken(30);
    return this.keycloak.token ?? null;
  }

  private updateIdentity(authenticated: boolean): void {
    this.authenticated.set(authenticated);
    const profile = this.keycloak.tokenParsed as (KeycloakProfile & {
      preferred_username?: string;
      sub?: string;
    }) | undefined;
    this.userName.set(authenticated ? profile?.preferred_username ?? profile?.username ?? null : null);
    this.customerId.set(authenticated ? profile?.sub ?? null : null);
  }
}
