import { CurrencyPipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { ButtonModule } from 'primeng/button';
import { Table, TableLazyLoadEvent, TableModule } from 'primeng/table';
import { AuthService } from '../../auth/auth.service';
import { OrderListItem } from '../../ordering/ordering.models';
import { OrderingService } from '../../ordering/ordering.service';
import { GraphqlLazyLoadEvent } from '../../shared/graphql/graphql-query-builder.service';
import { DataPageCardComponent } from '../../shared/ui/data-page-card.component';
import { TableCaptionComponent } from '../../shared/ui/table-caption.component';

@Component({
  selector: 'app-orders-page',
  imports: [ButtonModule, CurrencyPipe, DataPageCardComponent, TableCaptionComponent, TableModule],
  templateUrl: './orders-page.component.html',
  styleUrl: './orders-page.component.scss'
})
export class OrdersPageComponent {
  private readonly orderingService = inject(OrderingService);
  readonly auth = inject(AuthService);
  readonly pageSize = 10;
  readonly tableFirst = signal(0);
  readonly loading = signal(false);
  readonly errorMessage = signal('');
  readonly totalRecords = signal(0);
  readonly orders = signal<OrderListItem[]>([]);
  private lastLazyLoadEvent: GraphqlLazyLoadEvent = this.defaultEvent();

  onLazyLoad(event: TableLazyLoadEvent): void {
    const sortField = Array.isArray(event.sortField) ? event.sortField[0] : event.sortField;
    this.lastLazyLoadEvent = {
      first: event.first ?? 0, rows: event.rows ?? this.pageSize,
      sortField: sortField ?? 'orderName', sortOrder: event.sortOrder ?? 1,
      filters: event.filters ?? {}
    };

    this.loadOrders(this.lastLazyLoadEvent);
  }

  refresh(): void { this.loadOrders(this.lastLazyLoadEvent); }
  clearFilters(table: Table): void { table.clear(); this.lastLazyLoadEvent = this.defaultEvent(); this.loadOrders(this.lastLazyLoadEvent); }
  lineTotal(item: { quantity: number; price: number }): number { return item.quantity * item.price; }

  private loadOrders(event: GraphqlLazyLoadEvent): void {
    const customerId = this.auth.customerId();
    if (!customerId) return;
    const ownOrdersEvent = {
      ...event,
      filters: { ...(event.filters ?? {}), customerId: { value: customerId, matchMode: 'equals' } }
    };
    this.loading.set(true);
    this.errorMessage.set('');
    this.orderingService.getOrders(ownOrdersEvent).subscribe({
      next: page => {
        this.tableFirst.set(page.pageIndex * page.pageSize);
        this.totalRecords.set(page.count);
        this.orders.set(page.data);
        this.loading.set(false);
      },
      error: error => {
        this.loading.set(false);
        this.errorMessage.set(error instanceof Error ? error.message : 'Unable to load orders.');
      }
    });
  }

  private defaultEvent(): GraphqlLazyLoadEvent {
    return { first: 0, rows: this.pageSize, sortField: 'orderName', sortOrder: 1, filters: {} };
  }
}
