import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { MessageModule } from 'primeng/message';
import { PaginatorModule } from 'primeng/paginator';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { TagModule } from 'primeng/tag';
import { CatalogService } from '../../catalog/catalog.service';
import { ProductDto } from '../../catalog/catalog.models';

@Component({
  selector: 'app-products-page',
  imports: [
    CommonModule,
    ButtonModule,
    CardModule,
    MessageModule,
    PaginatorModule,
    ProgressSpinnerModule,
    TagModule
  ],
  templateUrl: './products-page.component.html',
  styleUrl: './products-page.component.scss'
})
export class ProductsPageComponent implements OnInit {
  private readonly catalogService = inject(CatalogService);

  readonly pageSize = 6;
  pageIndex = 0;
  loading = false;
  errorMessage = '';
  totalRecords = 0;
  products: ProductDto[] = [];
  selectedProduct: ProductDto | null = null;

  ngOnInit(): void {
    queueMicrotask(() => this.loadProducts());
  }

  loadProducts(pageIndex = 0): void {
    this.loading = true;
    this.errorMessage = '';

    this.catalogService.getProducts(pageIndex, this.pageSize).subscribe({
      next: page => {
        this.pageIndex = page.pageIndex;
        this.totalRecords = page.count;
        this.products = page.data;
        this.loading = false;

        if (this.products.length > 0) {
          this.selectProduct(this.products[0]);
        } else {
          this.selectedProduct = null;
        }
      },
      error: error => {
        this.loading = false;
        this.errorMessage = this.toMessage(error);
      }
    });
  }

  onPageChange(event: { page?: number }): void {
    this.loadProducts(event.page ?? 0);
  }

  selectProduct(product: ProductDto): void {
    this.catalogService.getProduct(product.id).subscribe({
      next: item => {
        this.selectedProduct = item;
      },
      error: error => {
        this.errorMessage = this.toMessage(error);
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
