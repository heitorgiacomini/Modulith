import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { MessageModule } from 'primeng/message';
import { TagModule } from 'primeng/tag';
import { Table, TableLazyLoadEvent, TableModule } from 'primeng/table';
import { CatalogService } from '../../catalog/catalog.service';
import { ProductDto } from '../../catalog/catalog.models';
import { GraphqlLazyLoadEvent } from '../../shared/graphql/graphql-query-builder.service';

@Component({
  selector: 'app-products-page',
  imports: [
    CommonModule,
    ButtonModule,
    CardModule,
    MessageModule,
    TagModule,
    TableModule
  ],
  templateUrl: './products-page.component.html',
  styleUrl: './products-page.component.scss'
})
export class ProductsPageComponent implements OnInit {
  private readonly catalogService = inject(CatalogService);

  readonly pageSize = 10;
  readonly maxFilterRules = Number.MAX_SAFE_INTEGER;
  readonly tableFirst = signal(0);
  readonly loading = signal(false);
  readonly errorMessage = signal('');
  readonly totalRecords = signal(0);
  readonly products = signal<ProductDto[]>([]);
  readonly selectedProduct = signal<ProductDto | null>(null);

  private lastLazyLoadEvent: GraphqlLazyLoadEvent = this.createDefaultLazyLoadEvent();

  ngOnInit(): void {
    this.loadProducts(this.lastLazyLoadEvent);
  }

  onLazyLoad(event: TableLazyLoadEvent): void {
    const lazyLoadEvent = this.normalizeLazyLoadEvent(event);
    this.lastLazyLoadEvent = lazyLoadEvent;
    this.loadProducts(lazyLoadEvent);
  }

  refresh(): void {
    this.loadProducts(this.lastLazyLoadEvent);
  }

  clearFilters(table: Table): void {
    table.clear();
  }

  loadProducts(event: GraphqlLazyLoadEvent): void {
    this.loading.set(true);
    this.errorMessage.set('');

    this.catalogService.getProducts(event).subscribe({
      next: page => {
        this.tableFirst.set(page.pageIndex * page.pageSize);
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

  private createDefaultLazyLoadEvent(): GraphqlLazyLoadEvent {
    return {
      first: 0,
      rows: this.pageSize,
      sortField: 'name',
      sortOrder: 1,
      filters: {}
    };
  }

  private normalizeLazyLoadEvent(event: TableLazyLoadEvent): GraphqlLazyLoadEvent {
    const sortField = Array.isArray(event.sortField) ? event.sortField[0] : event.sortField;

    return {
      first: typeof event.first === 'number' && event.first >= 0 ? event.first : 0,
      rows: typeof event.rows === 'number' && event.rows > 0 ? event.rows : this.pageSize,
      sortField: sortField ?? 'name',
      sortOrder: event.sortOrder ?? 1,
      filters: event.filters ?? {}
    };
  }

  private toMessage(error: unknown): string {
    if (error instanceof Error) {
      return error.message;
    }

    return 'Unable to load catalog data.';
  }
}
