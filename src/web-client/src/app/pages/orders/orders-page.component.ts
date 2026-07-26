import { Component, inject, OnInit, signal } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { DialogModule } from 'primeng/dialog';
import { InputNumberModule } from 'primeng/inputnumber';
import { InputTextModule } from 'primeng/inputtext';
import { MessageModule } from 'primeng/message';
import { Table, TableLazyLoadEvent, TableModule } from 'primeng/table';
import { CreateOrderRequest, OrderListItem } from '../../ordering/ordering.models';
import { OrderingService } from '../../ordering/ordering.service';
import { GraphqlLazyLoadEvent } from '../../shared/graphql/graphql-query-builder.service';

@Component({
  selector: 'app-orders-page',
  imports: [ButtonModule, CardModule, CurrencyPipe, DialogModule, FormsModule, InputNumberModule, InputTextModule, MessageModule, TableModule],
  templateUrl: './orders-page.component.html',
  styleUrl: './orders-page.component.scss'
})
export class OrdersPageComponent implements OnInit {
  private readonly orderingService = inject(OrderingService);
  readonly pageSize = 10;
  readonly maxFilterRules = Number.MAX_SAFE_INTEGER;
  readonly tableFirst = signal(0);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly createDialogVisible = signal(false);
  readonly errorMessage = signal('');
  readonly totalRecords = signal(0);
  readonly orders = signal<OrderListItem[]>([]);
  newOrder = this.emptyOrder();
  private lastLazyLoadEvent: GraphqlLazyLoadEvent = this.createDefaultLazyLoadEvent();

  ngOnInit(): void { this.loadOrders(this.lastLazyLoadEvent); }
  onLazyLoad(event: TableLazyLoadEvent): void { const lazyLoadEvent = this.normalizeLazyLoadEvent(event); this.lastLazyLoadEvent = lazyLoadEvent; this.loadOrders(lazyLoadEvent); }
  refresh(): void { this.loadOrders(this.lastLazyLoadEvent); }
  clearFilters(table: Table): void { table.clear(); }
  openCreateDialog(): void { this.newOrder = this.emptyOrder(); this.createDialogVisible.set(true); }

  createOrder(): void {
    if (!this.newOrder.customerId.trim() || !this.newOrder.orderName.trim() || !this.newOrder.productId.trim() || !this.newOrder.firstName.trim() || !this.newOrder.lastName.trim() || !this.newOrder.email.trim() || this.newOrder.quantity <= 0 || this.newOrder.price <= 0) {
      this.errorMessage.set('Customer, order, product, address fields, and a positive item quantity and price are required.'); return;
    }
    const address = { firstName: this.newOrder.firstName.trim(), lastName: this.newOrder.lastName.trim(), emailAddress: this.newOrder.email.trim(), addressLine: this.newOrder.addressLine.trim(), country: this.newOrder.country.trim(), state: this.newOrder.state.trim(), zipCode: this.newOrder.zipCode.trim() };
    const request: CreateOrderRequest = { order: { id: crypto.randomUUID(), customerId: this.newOrder.customerId.trim(), orderName: this.newOrder.orderName.trim(), shippingAddress: address, billingAddress: address, payment: { cardName: this.newOrder.cardName.trim(), cardNumber: this.newOrder.cardNumber.trim(), expiration: this.newOrder.expiration.trim(), cvv: this.newOrder.cvv.trim(), paymentMethod: 1 }, items: [{ orderId: crypto.randomUUID(), productId: this.newOrder.productId.trim(), quantity: this.newOrder.quantity, price: this.newOrder.price }] } };
    this.saving.set(true); this.errorMessage.set('');
    this.orderingService.createOrder(request).subscribe({ next: () => { this.saving.set(false); this.createDialogVisible.set(false); this.refresh(); }, error: error => { this.saving.set(false); this.errorMessage.set(this.toMessage(error)); } });
  }

  private loadOrders(event: GraphqlLazyLoadEvent): void { this.loading.set(true); this.errorMessage.set(''); this.orderingService.getOrders(event).subscribe({ next: page => { this.tableFirst.set(page.pageIndex * page.pageSize); this.totalRecords.set(page.count); this.orders.set(page.data); this.loading.set(false); }, error: error => { this.loading.set(false); this.errorMessage.set(this.toMessage(error)); } }); }
  private emptyOrder() { return { customerId: '', orderName: '', productId: '', quantity: 1, price: 0, firstName: '', lastName: '', email: '', addressLine: '', country: '', state: '', zipCode: '', cardName: '', cardNumber: '', expiration: '', cvv: '' }; }
  private createDefaultLazyLoadEvent(): GraphqlLazyLoadEvent { return { first: 0, rows: this.pageSize, sortField: 'orderName', sortOrder: 1, filters: {} }; }
  private normalizeLazyLoadEvent(event: TableLazyLoadEvent): GraphqlLazyLoadEvent { const sortField = Array.isArray(event.sortField) ? event.sortField[0] : event.sortField; return { first: typeof event.first === 'number' && event.first >= 0 ? event.first : 0, rows: typeof event.rows === 'number' && event.rows > 0 ? event.rows : this.pageSize, sortField: sortField ?? 'orderName', sortOrder: event.sortOrder ?? 1, filters: event.filters ?? {} }; }
  private toMessage(error: unknown): string { return error instanceof Error ? error.message : 'Unable to load orders.'; }
}

