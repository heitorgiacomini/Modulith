import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
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
  readonly pageIndex = signal(0);
  readonly loading = signal(false);
  readonly errorMessage = signal('');
  readonly totalRecords = signal(0);
  readonly products = signal<ProductDto[]>([]);
  readonly selectedProduct = signal<ProductDto | null>(null);

  ngOnInit(): void {
    this.loadProducts();
  }

  loadProducts(pageIndex = 0): void {
    this.loading.set(true);
    this.errorMessage.set('');

    this.catalogService.getProducts(pageIndex, this.pageSize).subscribe({
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
