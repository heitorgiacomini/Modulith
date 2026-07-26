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
import { BasketListItem, CreateBasketRequest } from '../../basket/basket.models';
import { BasketService } from '../../basket/basket.service';
import { GraphqlLazyLoadEvent } from '../../shared/graphql/graphql-query-builder.service';

@Component({
  selector: 'app-baskets-page',
  imports: [ButtonModule, CardModule, CurrencyPipe, DialogModule, FormsModule, InputNumberModule, InputTextModule, MessageModule, TableModule],
  templateUrl: './baskets-page.component.html',
  styleUrl: './baskets-page.component.scss'
})
export class BasketsPageComponent implements OnInit {
  private readonly basketService = inject(BasketService);
  readonly pageSize = 10;
  readonly maxFilterRules = Number.MAX_SAFE_INTEGER;
  readonly tableFirst = signal(0);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly createDialogVisible = signal(false);
  readonly errorMessage = signal('');
  readonly totalRecords = signal(0);
  readonly baskets = signal<BasketListItem[]>([]);
  newBasketItem = this.emptyBasketItem();
  private lastLazyLoadEvent: GraphqlLazyLoadEvent = this.createDefaultLazyLoadEvent();

  ngOnInit(): void { this.loadBaskets(this.lastLazyLoadEvent); }
  onLazyLoad(event: TableLazyLoadEvent): void { const lazyLoadEvent = this.normalizeLazyLoadEvent(event); this.lastLazyLoadEvent = lazyLoadEvent; this.loadBaskets(lazyLoadEvent); }
  refresh(): void { this.loadBaskets(this.lastLazyLoadEvent); }
  clearFilters(table: Table): void { table.clear(); }
  openCreateDialog(): void { this.newBasketItem = this.emptyBasketItem(); this.createDialogVisible.set(true); }

  createBasket(): void {
    if (!this.newBasketItem.productId.trim() || !this.newBasketItem.productName.trim() || this.newBasketItem.quantity <= 0 || this.newBasketItem.price <= 0) {
      this.errorMessage.set('Product ID, name, positive quantity, and positive price are required.'); return;
    }
    const request: CreateBasketRequest = { shoppingCart: { id: crypto.randomUUID(), userName: '', items: [{ productId: this.newBasketItem.productId.trim(), productName: this.newBasketItem.productName.trim(), color: this.newBasketItem.color.trim(), quantity: this.newBasketItem.quantity, price: this.newBasketItem.price }] } };
    this.saving.set(true); this.errorMessage.set('');
    this.basketService.createBasket(request).subscribe({
      next: () => { this.saving.set(false); this.createDialogVisible.set(false); this.refresh(); },
      error: error => { this.saving.set(false); this.errorMessage.set(this.toMessage(error)); }
    });
  }

  private loadBaskets(event: GraphqlLazyLoadEvent): void { this.loading.set(true); this.errorMessage.set(''); this.basketService.getBaskets(event).subscribe({ next: page => { this.tableFirst.set(page.pageIndex * page.pageSize); this.totalRecords.set(page.count); this.baskets.set(page.data); this.loading.set(false); }, error: error => { this.loading.set(false); this.errorMessage.set(this.toMessage(error)); } }); }
  private emptyBasketItem() { return { productId: '', productName: '', color: '', quantity: 1, price: 0 }; }
  private createDefaultLazyLoadEvent(): GraphqlLazyLoadEvent { return { first: 0, rows: this.pageSize, sortField: 'userName', sortOrder: 1, filters: {} }; }
  private normalizeLazyLoadEvent(event: TableLazyLoadEvent): GraphqlLazyLoadEvent { const sortField = Array.isArray(event.sortField) ? event.sortField[0] : event.sortField; return { first: typeof event.first === 'number' && event.first >= 0 ? event.first : 0, rows: typeof event.rows === 'number' && event.rows > 0 ? event.rows : this.pageSize, sortField: sortField ?? 'userName', sortOrder: event.sortOrder ?? 1, filters: event.filters ?? {} }; }
  private toMessage(error: unknown): string { return error instanceof Error ? error.message : 'Unable to load baskets. Sign in through Keycloak before creating a basket.'; }
}
