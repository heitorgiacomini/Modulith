import { CurrencyPipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { Table, TableLazyLoadEvent, TableModule } from 'primeng/table';
import { AuthService } from '../../auth/auth.service';
import { BasketListItem, CheckoutBasketRequest } from '../../basket/basket.models';
import { BasketService } from '../../basket/basket.service';
import { GraphqlLazyLoadEvent } from '../../shared/graphql/graphql-query-builder.service';
import { DataPageCardComponent } from '../../shared/ui/data-page-card.component';
import { TableCaptionComponent } from '../../shared/ui/table-caption.component';

@Component({
  selector: 'app-baskets-page',
  imports: [
    ButtonModule, CurrencyPipe, DataPageCardComponent, DialogModule, FormsModule,
    InputTextModule, TableCaptionComponent, TableModule
  ],
  templateUrl: './baskets-page.component.html',
  styleUrl: './baskets-page.component.scss'
})
export class BasketsPageComponent {
  private readonly basketService = inject(BasketService);
  readonly auth = inject(AuthService);

  readonly pageSize = 10;
  readonly maxFilterRules = Number.MAX_SAFE_INTEGER;
  readonly tableFirst = signal(0);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly checkoutDialogVisible = signal(false);
  readonly errorMessage = signal('');
  readonly totalRecords = signal(0);
  readonly baskets = signal<BasketListItem[]>([]);
  checkout = this.emptyCheckout();
  private lastLazyLoadEvent: GraphqlLazyLoadEvent = this.createDefaultLazyLoadEvent();

  onLazyLoad(event: TableLazyLoadEvent): void {
    const lazyLoadEvent = this.normalizeLazyLoadEvent(event);
    this.lastLazyLoadEvent = lazyLoadEvent;
    this.loadBaskets(lazyLoadEvent);
  }

  refresh(): void {
    this.loadBaskets(this.lastLazyLoadEvent);
  }

  clearFilters(table: Table): void {
    table.clear();
  }

  removeItem(productId: string): void {
    const userName = this.auth.userName();
    if (!userName) {
      this.errorMessage.set('Your authenticated user could not be resolved.');
      return;
    }
    this.saving.set(true);
    this.basketService.removeItem(userName, productId).subscribe({
      next: () => { this.saving.set(false); this.refresh(); },
      error: error => { this.saving.set(false); this.errorMessage.set(this.toMessage(error)); }
    });
  }

  openCheckout(): void {
    this.checkout = this.emptyCheckout();
    this.checkoutDialogVisible.set(true);
  }

  submitCheckout(): void {
    const userName = this.auth.userName();
    const customerId = this.auth.customerId();
    if (!userName || !customerId || !Object.values(this.checkout).every(value => String(value).trim())) {
      this.errorMessage.set('Complete every checkout field.');
      return;
    }

    const request: CheckoutBasketRequest = {
      basketCheckout: {
        userName,
        customerId,
        totalPrice: 0,
        ...this.checkout,
        paymentMethod: 1
      }
    };
    this.saving.set(true);
    this.errorMessage.set('');
    this.basketService.checkout(request).subscribe({
      next: response => {
        this.saving.set(false);
        if (!response.isSuccess) {
          this.errorMessage.set('Checkout could not be completed.');
          return;
        }
        this.checkoutDialogVisible.set(false);
        this.refresh();
      },
      error: error => { this.saving.set(false); this.errorMessage.set(this.toMessage(error)); }
    });
  }

  lineTotal(item: { quantity: number; price: number }): number {
    return item.quantity * item.price;
  }

  private loadBaskets(event: GraphqlLazyLoadEvent): void {
    if (!this.auth.userName()) {
      return;
    }
    const ownBasketEvent = {
      ...event,
      filters: {
        ...(event.filters ?? {}),
        userName: { value: this.auth.userName(), matchMode: 'equals' }
      }
    };
    this.loading.set(true);
    this.errorMessage.set('');
    this.basketService.getBaskets(ownBasketEvent).subscribe({
      next: page => {
        this.tableFirst.set(page.pageIndex * page.pageSize);
        this.totalRecords.set(page.count);
        this.baskets.set(page.data);
        this.loading.set(false);
      },
      error: error => { this.loading.set(false); this.errorMessage.set(this.toMessage(error)); }
    });
  }

  private emptyCheckout() {
    return {
      firstName: '', lastName: '', emailAddress: '', addressLine: '',
      country: '', state: '', zipCode: '', cardName: '', cardNumber: '',
      expiration: '', cvv: ''
    };
  }

  private createDefaultLazyLoadEvent(): GraphqlLazyLoadEvent {
    return { first: 0, rows: this.pageSize, sortField: 'userName', sortOrder: 1, filters: {} };
  }

  private normalizeLazyLoadEvent(event: TableLazyLoadEvent): GraphqlLazyLoadEvent {
    const sortField = Array.isArray(event.sortField) ? event.sortField[0] : event.sortField;
    return {
      first: typeof event.first === 'number' && event.first >= 0 ? event.first : 0,
      rows: typeof event.rows === 'number' && event.rows > 0 ? event.rows : this.pageSize,
      sortField: sortField ?? 'userName',
      sortOrder: event.sortOrder ?? 1,
      filters: event.filters ?? {}
    };
  }

  private toMessage(error: unknown): string {
    return error instanceof Error ? error.message : 'Unable to update your cart.';
  }
}
