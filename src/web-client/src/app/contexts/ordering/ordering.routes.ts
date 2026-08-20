import { Routes } from '@angular/router';

export const ORDERING_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./features/order-history/order-history.page').then(m => m.OrderHistoryPage)
  }
];
