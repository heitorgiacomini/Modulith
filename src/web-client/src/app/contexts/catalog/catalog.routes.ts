import { Routes } from '@angular/router';

export const CATALOG_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./features/product-management/product-management.page').then(m => m.ProductManagementPage)
  }
];
