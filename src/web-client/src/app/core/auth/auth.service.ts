import { Injectable, signal } from '@angular/core';
import Keycloak, { KeycloakProfile } from 'keycloak-js';
import KeycloakAuthorization from 'keycloak-js/authz';
import { environment } from '../../../environments/environment';
import { AuthorizationPermission } from './authorization-context';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly keycloak = new Keycloak(environment.keycloak);
  private readonly authorization = new KeycloakAuthorization(this.keycloak);
  private readonly permissionTokens = new Map<string, string>();
  private refreshPromise: Promise<boolean> | null = null;
  readonly authenticated = signal(false);
  readonly userName = signal<string | null>(null);
  readonly customerId = signal<string | null>(null);
  readonly profile = signal<KeycloakProfile | null>(null);

  async initialize(): Promise<void> {
    const authenticated = await this.keycloak.init({
      onLoad: 'check-sso',
      pkceMethod: 'S256',
      checkLoginIframe: false
    });
    this.updateIdentity(authenticated);

    this.keycloak.onAuthSuccess = () => this.updateIdentity(true);
    this.keycloak.onAuthLogout = () => {
      this.permissionTokens.clear();
      this.updateIdentity(false);
    };
    this.keycloak.onTokenExpired = () => {
      void this.refreshToken();
    };
  }

  login(): Promise<void> {
    return this.keycloak.login({ redirectUri: window.location.href });
  }

  logout(): Promise<void> {
    return this.keycloak.logout({ redirectUri: window.location.origin });
  }

  openAccountConsole(): Promise<void> {
    return this.keycloak.accountManagement();
  }

  async token(): Promise<string | null> {
    if (!this.keycloak.authenticated) {
      return null;
    }

    if (!await this.refreshToken()) {
      return null;
    }
    return this.keycloak.token ?? null;
  }

  async permissionToken(permission: AuthorizationPermission): Promise<string | null> {
    if (!this.keycloak.authenticated) {
      return null;
    }

    if (!await this.refreshToken()) {
      return null;
    }
    const cacheKey = `${permission.resource}#${[...permission.scopes].sort().join(',')}`;
    const cachedToken = this.permissionTokens.get(cacheKey);
    if (cachedToken && this.hasTokenLifetime(cachedToken, 30)) {
      return cachedToken;
    }

    const token = await new Promise<string>((resolve, reject) => {
      this.authorization.entitlement('ordering-api', {
        permissions: [{ id: permission.resource, scopes: permission.scopes }],
        metadata: { responseIncludeResourceName: true }
      }).then(
        resolve,
        () => reject(new Error('Access to the requested Ordering scope was denied.')),
        () => reject(new Error('Keycloak could not evaluate the Ordering permission.'))
      );
    });

    this.permissionTokens.set(cacheKey, token);
    return token;
  }

  private updateIdentity(authenticated: boolean): void {
    this.authenticated.set(authenticated);
    const profile = this.keycloak.tokenParsed as (KeycloakProfile & {
      preferred_username?: string;
      sub?: string;
    }) | undefined;
    this.userName.set(authenticated ? profile?.preferred_username ?? profile?.username ?? null : null);
    this.customerId.set(authenticated ? profile?.sub ?? null : null);
    this.profile.set(authenticated && profile ? profile : null);
  }

  private async refreshToken(): Promise<boolean> {
    if (!this.keycloak.authenticated) {
      return false;
    }

    this.refreshPromise ??= this.keycloak.updateToken(30)
      .then(() => true)
      .catch(() => {
        this.permissionTokens.clear();
        this.keycloak.clearToken();
        this.updateIdentity(false);
        void this.login().catch(() => undefined);
        return false;
      })
      .finally(() => {
        this.refreshPromise = null;
      });

    return this.refreshPromise;
  }

  private hasTokenLifetime(token: string, minimumValiditySeconds: number): boolean {
    try {
      const payload = JSON.parse(this.decodeBase64Url(token.split('.')[1])) as { exp?: number };
      return typeof payload.exp === 'number' && payload.exp > Date.now() / 1000 + minimumValiditySeconds;
    } catch {
      return false;
    }
  }

  private decodeBase64Url(value: string): string {
    const base64 = value.replace(/-/g, '+').replace(/_/g, '/').padEnd(Math.ceil(value.length / 4) * 4, '=');
    return decodeURIComponent(Array.from(atob(base64))
      .map(character => `%${character.charCodeAt(0).toString(16).padStart(2, '0')}`)
      .join(''));
  }
}
