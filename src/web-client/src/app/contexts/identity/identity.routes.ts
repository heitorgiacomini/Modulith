import { Routes } from '@angular/router';

export const IDENTITY_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./features/account-profile/account-profile.page').then(m => m.AccountProfilePage)
  }
];
