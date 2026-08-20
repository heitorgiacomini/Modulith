import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { DialogModule } from 'primeng/dialog';
import { InputNumberModule } from 'primeng/inputnumber';
import { InputTextModule } from 'primeng/inputtext';
import { MessageModule } from 'primeng/message';
import { TagModule } from 'primeng/tag';
import { Table, TableLazyLoadEvent, TableModule } from 'primeng/table';
import { CatalogService } from '../../data-access/catalog.api';
import { CreateProductRequest, ProductDto } from '../../data-access/catalog.models';
import { BasketFacade } from '../../../basket';
import { AuthService } from '../../../../core/auth/auth.service';
import { GraphqlLazyLoadEvent } from '../../../../core/graphql/graphql-query-builder.service';
import { DataPageCardComponent } from '../../../../shared/ui/data-page-card.component';
import { TableCaptionComponent } from '../../../../shared/ui/table-caption.component';
import { ProductDraft } from './product-create/product-draft';
import { DEFAULT_PRODUCT_PAGE_SIZE } from './product-list/product-list.constants';

@Component({
  selector: 'app-product-management-page',
  imports: [CommonModule, FormsModule, ButtonModule, CardModule, DataPageCardComponent, DialogModule, InputNumberModule, InputTextModule, MessageModule, TableCaptionComponent, TagModule, TableModule],
  templateUrl: './product-management.page.html',
  styleUrl: './product-management.page.scss'
})
export class ProductManagementPage {
  private readonly catalogService = inject(CatalogService);
  private readonly basketFacade = inject(BasketFacade);
  readonly auth = inject(AuthService);

  readonly pageSize = DEFAULT_PRODUCT_PAGE_SIZE;
  readonly maxFilterRules = Number.MAX_SAFE_INTEGER;
  readonly tableFirst = signal(0);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly createDialogVisible = signal(false);
  readonly addToCartDialogVisible = signal(false);
  readonly errorMessage = signal('');
  readonly totalRecords = signal(0);
  readonly products = signal<ProductDto[]>([]);
  readonly selectedProduct = signal<ProductDto | null>(null);
  newProduct: ProductDraft = this.emptyProduct();
  cartItem = { quantity: 1, color: '' };

  private lastLazyLoadEvent: GraphqlLazyLoadEvent = this.createDefaultLazyLoadEvent();

  onLazyLoad(event: TableLazyLoadEvent): void {
    const lazyLoadEvent = this.normalizeLazyLoadEvent(event);
    this.lastLazyLoadEvent = lazyLoadEvent;
    this.loadProducts(lazyLoadEvent);
  }

  refresh(): void { this.loadProducts(this.lastLazyLoadEvent); }
  clearFilters(table: Table): void { table.clear(); }

  openCreateDialog(): void {
    this.newProduct = this.emptyProduct();
    this.createDialogVisible.set(true);
  }

  createProduct(): void {
    const categories = this.newProduct.categories.split(',').map(category => category.trim()).filter(Boolean);
    if (!this.newProduct.name.trim() || categories.length === 0 || !this.newProduct.imageFile.trim() || this.newProduct.price <= 0) {
      this.errorMessage.set('Name, at least one category, image URL, and a positive price are required.');
      return;
    }

    const request: CreateProductRequest = {
      product: {
        name: this.newProduct.name.trim(),
        category: categories,
        description: this.newProduct.description.trim(),
        imageFile: this.newProduct.imageFile.trim(),
        price: this.newProduct.price
      }
    };
    this.saving.set(true);
    this.errorMessage.set('');
    this.catalogService.createProduct(request).subscribe({
      next: () => { this.saving.set(false); this.createDialogVisible.set(false); this.refresh(); },
      error: error => { this.saving.set(false); this.errorMessage.set(this.toMessage(error)); }
    });
  }

  loadProducts(event: GraphqlLazyLoadEvent): void {
    this.loading.set(true); this.errorMessage.set('');
    this.catalogService.getProducts(event).subscribe({
      next: page => {
        this.tableFirst.set(page.pageIndex * page.pageSize); this.totalRecords.set(page.count); this.products.set(page.data); this.loading.set(false);
        if (page.data.length > 0) { this.selectProduct(page.data[0]); } else { this.selectedProduct.set(null); }
      },
      error: error => { this.loading.set(false); this.errorMessage.set(this.toMessage(error)); }
    });
  }

  selectProduct(product: ProductDto): void {
    this.selectedProduct.set(product);
  }

  openAddToCartDialog(): void {
    if (!this.auth.authenticated()) {
      void this.auth.login();
      return;
    }
    this.cartItem = { quantity: 1, color: '' };
    this.addToCartDialogVisible.set(true);
  }

  addToCart(): void {
    const product = this.selectedProduct();
    const userName = this.auth.userName();
    if (!product || !userName || this.cartItem.quantity < 1) {
      this.errorMessage.set('Sign in and select a valid quantity before adding to the cart.');
      return;
    }

    this.saving.set(true);
    this.basketFacade.addItem(userName, product.id, this.cartItem.quantity, this.cartItem.color.trim()).subscribe({
      next: () => {
        this.saving.set(false);
        this.addToCartDialogVisible.set(false);
      },
      error: error => {
        this.saving.set(false);
        this.errorMessage.set(this.toMessage(error));
      }
    });
  }

  imageUrl(product: ProductDto): string { return product.imageFile.startsWith('http') ? product.imageFile : `https://placehold.co/900x600/2563eb/ffffff?text=${encodeURIComponent(product.name)}`; }
  private emptyProduct(): ProductDraft { return { name: '', categories: '', description: '', imageFile: '', price: 0 }; }
  private createDefaultLazyLoadEvent(): GraphqlLazyLoadEvent { return { first: 0, rows: this.pageSize, sortField: 'name', sortOrder: 1, filters: {} }; }
  private normalizeLazyLoadEvent(event: TableLazyLoadEvent): GraphqlLazyLoadEvent { const sortField = Array.isArray(event.sortField) ? event.sortField[0] : event.sortField; return { first: typeof event.first === 'number' && event.first >= 0 ? event.first : 0, rows: typeof event.rows === 'number' && event.rows > 0 ? event.rows : this.pageSize, sortField: sortField ?? 'name', sortOrder: event.sortOrder ?? 1, filters: event.filters ?? {} }; }
  private toMessage(error: unknown): string { return error instanceof Error ? error.message : 'Unable to load catalog data.'; }
}
