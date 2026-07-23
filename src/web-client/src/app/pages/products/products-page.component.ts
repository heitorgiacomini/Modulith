import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { InputTextModule } from 'primeng/inputtext';
import { MessageModule } from 'primeng/message';
import { PaginatorModule } from 'primeng/paginator';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { SelectModule } from 'primeng/select';
import { TagModule } from 'primeng/tag';
import { CatalogService, ProductSort } from '../../catalog/catalog.service';
import { ProductDto } from '../../catalog/catalog.models';

@Component({
  selector: 'app-products-page',
  imports: [
    CommonModule,
    FormsModule,
    ButtonModule,
    CardModule,
    InputTextModule,
    MessageModule,
    PaginatorModule,
    ProgressSpinnerModule,
    SelectModule,
    TagModule
  ],
  templateUrl: './products-page.component.html',
  styleUrl: './products-page.component.scss'
})
export class ProductsPageComponent implements OnInit {
  private readonly catalogService = inject(CatalogService);

  readonly pageSize = 6;
  readonly pageIndex = signal(0);
  readonly loading = signal(false);
  readonly errorMessage = signal('');
  readonly totalRecords = signal(0);
  readonly products = signal<ProductDto[]>([]);
  readonly selectedProduct = signal<ProductDto | null>(null);
  readonly sortOptions: Array<{ label: string; value: ProductSort }> = [
    { label: 'Name: A to Z', value: 'NAME_ASC' },
    { label: 'Name: Z to A', value: 'NAME_DESC' },
    { label: 'Price: low to high', value: 'PRICE_ASC' },
    { label: 'Price: high to low', value: 'PRICE_DESC' }
  ];

  searchTerm = '';
  sortOrder: ProductSort = 'NAME_ASC';

  ngOnInit(): void {
    this.loadProducts();
  }

  loadProducts(pageIndex = 0): void {
    this.loading.set(true);
    this.errorMessage.set('');

    this.catalogService.getProducts(pageIndex, this.pageSize, {
      search: this.searchTerm,
      sort: this.sortOrder
    }).subscribe({
      next: page => {
        this.pageIndex.set(page.pageIndex);
        this.totalRecords.set(page.count);
        this.products.set(page.data);
        this.loading.set(false);

        if (page.data.length > 0) {
          this.selectProduct(page.data[0]);
        } else {
          this.selectedProduct.set(null);
        }
      },
      error: error => {
        this.loading.set(false);
        this.errorMessage.set(this.toMessage(error));
      }
    });
  }

  onPageChange(event: { page?: number }): void {
    this.loadProducts(event.page ?? 0);
  }

  applyFilters(): void {
    this.loadProducts();
  }

  onSortChange(sort: ProductSort): void {
    this.sortOrder = sort;
    this.applyFilters();
  }

  clearFilters(): void {
    this.searchTerm = '';
    this.sortOrder = 'NAME_ASC';
    this.applyFilters();
  }

  hasActiveFilters(): boolean {
    return this.searchTerm.trim().length > 0 || this.sortOrder !== 'NAME_ASC';
  }

  selectProduct(product: ProductDto): void {
    this.catalogService.getProduct(product.id).subscribe({
      next: item => {
        this.selectedProduct.set(item);
      },
      error: error => {
        this.errorMessage.set(this.toMessage(error));
      }
    });
  }

  imageUrl(product: ProductDto): string {
    return product.imageFile.startsWith('http')
      ? product.imageFile
      : `https://placehold.co/900x600/2563eb/ffffff?text=${encodeURIComponent(product.name)}`;
  }

  trackByProduct(_: number, product: ProductDto): string {
    return product.id;
  }

  private toMessage(error: unknown): string {
    if (error instanceof Error) {
      return error.message;
    }

    return 'Unable to load catalog data.';
  }
}
