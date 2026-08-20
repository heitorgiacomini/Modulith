import { HttpContextToken } from '@angular/common/http';

export interface AuthorizationPermission {
  resource: string;
  scopes: string[];
}

export const AUTHORIZATION_PERMISSION = new HttpContextToken<AuthorizationPermission | null>(() => null);