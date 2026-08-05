import { Routes } from '@angular/router';
import { authGuard } from './auth/auth.guard';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'products'
  },
  {
    path: 'products',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./pages/products/products-page.component').then(m => m.ProductsPageComponent)
  },
  {
    path: 'baskets',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./pages/baskets/baskets-page.component').then(m => m.BasketsPageComponent)
  },
  {
    path: 'orders',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./pages/orders/orders-page.component').then(m => m.OrdersPageComponent)
  },
  {
    path: '**',
    redirectTo: 'products'
  }
];
