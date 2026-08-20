import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'products'
  },
  {
    path: 'products',
    canActivate: [authGuard],
    loadChildren: () => import('./contexts/catalog/catalog.routes').then(m => m.CATALOG_ROUTES)
  },
  {
    path: 'baskets',
    canActivate: [authGuard],
    loadChildren: () => import('./contexts/basket/basket.routes').then(m => m.BASKET_ROUTES)
  },
  {
    path: 'orders',
    canActivate: [authGuard],
    loadChildren: () => import('./contexts/ordering/ordering.routes').then(m => m.ORDERING_ROUTES)
  },
  {
    path: 'account',
    canActivate: [authGuard],
    loadChildren: () => import('./contexts/identity/identity.routes').then(m => m.IDENTITY_ROUTES)
  },
  {
    path: '**',
    redirectTo: 'products'
  }
];
