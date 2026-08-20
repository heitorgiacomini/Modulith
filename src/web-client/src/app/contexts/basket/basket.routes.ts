import { Routes } from '@angular/router';

export const BASKET_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./features/basket-management/basket-management.page').then(m => m.BasketManagementPage)
  }
];
