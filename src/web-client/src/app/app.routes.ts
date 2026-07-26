import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'products'
  },
  {
    path: 'products',
    loadComponent: () =>
      import('./pages/products/products-page.component').then(m => m.ProductsPageComponent)
  },
  {
    path: 'baskets',
    loadComponent: () =>
      import('./pages/baskets/baskets-page.component').then(m => m.BasketsPageComponent)
  },
  {
    path: 'orders',
    loadComponent: () =>
      import('./pages/orders/orders-page.component').then(m => m.OrdersPageComponent)
  },
  {
    path: '**',
    redirectTo: 'products'
  }
];
