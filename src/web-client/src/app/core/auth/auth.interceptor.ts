import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { from, switchMap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';
import { AUTHORIZATION_PERMISSION } from './authorization-context';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const isApplicationApi = request.url.startsWith(environment.apiUrl) || request.url.startsWith(environment.graphqlUrl);
  if (!isApplicationApi) {
    return next(request);
  }

  const auth = inject(AuthService);
  const permission = request.context.get(AUTHORIZATION_PERMISSION);
  const tokenRequest = permission ? auth.permissionToken(permission) : auth.token();

  return from(tokenRequest).pipe(
    switchMap(token => next(token
      ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
      : request))
  );
};
