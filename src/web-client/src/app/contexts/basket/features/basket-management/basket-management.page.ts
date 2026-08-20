import { CurrencyPipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { InputMaskDirective } from 'primeng/inputmask';
import { Table, TableLazyLoadEvent, TableModule } from 'primeng/table';
import { AuthService } from '../../../../core/auth/auth.service';
import { GraphqlLazyLoadEvent } from '../../../../core/graphql/graphql-query-builder.service';
import { IdentityFacade, Account, SaveAddress, SavePaymentMethod, SavedAddress, SavedPaymentMethod } from '../../../identity';
import { BasketListItem, CheckoutBasketRequest } from '../../data-access/basket.models';
import { BasketService } from '../../data-access/basket.api';
import { DataPageCardComponent } from '../../../../shared/ui/data-page-card.component';
import { TableCaptionComponent } from '../../../../shared/ui/table-caption.component';

@Component({
  selector: 'app-basket-management-page',
  imports: [ButtonModule, CurrencyPipe, DataPageCardComponent, DialogModule, FormsModule, InputMaskDirective, InputTextModule, TableCaptionComponent, TableModule],
  templateUrl: './basket-management.page.html',
  styleUrl: './basket-management.page.scss'
})
export class BasketManagementPage {
  private readonly basketService = inject(BasketService);
  private readonly identityFacade = inject(IdentityFacade);
  readonly auth = inject(AuthService);
  readonly pageSize = 10;
  readonly maxFilterRules = Number.MAX_SAFE_INTEGER;
  readonly tableFirst = signal(0);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly checkoutLoading = signal(false);
  readonly checkoutDialogVisible = signal(false);
  readonly errorMessage = signal('');
  readonly totalRecords = signal(0);
  readonly baskets = signal<BasketListItem[]>([]);
  readonly account = signal<Account | null>(null);
  selectedAddressId = '';
  selectedPaymentMethodId = '';
  newAddressVisible = false;
  newPaymentVisible = false;
  newAddress = this.emptyAddress();
  newPayment = this.emptyPayment();
  private lastLazyLoadEvent: GraphqlLazyLoadEvent = this.createDefaultLazyLoadEvent();

  onLazyLoad(event: TableLazyLoadEvent): void {
    const lazyLoadEvent = this.normalizeLazyLoadEvent(event);
    this.lastLazyLoadEvent = lazyLoadEvent;
    this.loadBaskets(lazyLoadEvent);
  }

  refresh(): void { this.loadBaskets(this.lastLazyLoadEvent); }
  clearFilters(table: Table): void { table.clear(); }

  removeItem(productId: string): void {
    const userName = this.auth.userName();
    if (!userName) { this.errorMessage.set('Your authenticated user could not be resolved.'); return; }
    this.saving.set(true);
    this.basketService.removeItem(userName, productId).subscribe({
      next: () => { this.saving.set(false); this.refresh(); },
      error: error => { this.saving.set(false); this.errorMessage.set(this.toMessage(error)); }
    });
  }

  openCheckout(): void {
    this.errorMessage.set('');
    this.checkoutDialogVisible.set(true);
    this.checkoutLoading.set(true);
    this.identityFacade.getAccount().subscribe({
      next: account => {
        const checkoutAccount: Account = {
          ...account,
          addresses: account.addresses ?? [],
          paymentMethods: account.paymentMethods ?? []
        };
        this.account.set(checkoutAccount);
        this.selectedAddressId = checkoutAccount.addresses.find(address => address.isDefaultShipping)?.id ?? checkoutAccount.addresses[0]?.id ?? '';
        this.selectedPaymentMethodId = checkoutAccount.paymentMethods.find(paymentMethod => paymentMethod.isDefault)?.id ?? checkoutAccount.paymentMethods[0]?.id ?? '';
        this.checkoutLoading.set(false);
      },
      error: error => {
        this.checkoutLoading.set(false);
        this.errorMessage.set(this.toMessage(error));
      }
    });
  }

  saveAddress(): void {
    const requiredAddressFields = [
      this.newAddress.label, this.newAddress.firstName, this.newAddress.lastName, this.newAddress.email,
      this.newAddress.phone, this.newAddress.addressLine1, this.newAddress.city, this.newAddress.state,
      this.newAddress.postalCode, this.newAddress.countryCode
    ];
    if (!requiredAddressFields.every(value => value.trim()) || !/^\S+@\S+\.\S+$/.test(this.newAddress.email) || !/^[A-Za-z]{2}$/.test(this.newAddress.countryCode) || !/^[A-Za-z0-9 -]+$/.test(this.newAddress.postalCode)) {
      this.errorMessage.set('Enter a valid email, postal code, and two-letter country code.');
      return;
    }
    this.newAddress.countryCode = this.newAddress.countryCode.toUpperCase();
    this.saving.set(true);
    this.identityFacade.addAddress(this.newAddress).subscribe({
      next: address => {
        this.updateAccountAddresses(address);
        this.selectedAddressId = address.id;
        this.newAddress = this.emptyAddress();
        this.newAddressVisible = false;
        this.saving.set(false);
      },
      error: error => { this.saving.set(false); this.errorMessage.set(this.toMessage(error)); }
    });
  }

  savePaymentMethod(): void {
    const cardNumber = this.newPayment.cardNumber.replace(/\s/g, '');
    if (!this.newPayment.label.trim() || !this.newPayment.cardholderName.trim() || !/^\d{12,19}$/.test(cardNumber) || !/^(0[1-9]|1[0-2])\/\d{2}$/.test(this.newPayment.expiration.trim())) {
      this.errorMessage.set('Enter a valid payment label, cardholder, card number, and MM/YY expiration.');
      return;
    }
    const paymentMethod: SavePaymentMethod = {
      label: this.newPayment.label.trim(),
      cardholderName: this.newPayment.cardholderName.trim(),
      brand: this.cardBrand(cardNumber),
      last4: cardNumber.slice(-4),
      expiration: this.newPayment.expiration.trim(),
      token: `mock_${crypto.randomUUID()}`,
      isDefault: !(this.account()?.paymentMethods?.length ?? 0)
    };
    this.newPayment.cardNumber = '';
    this.saving.set(true);
    this.identityFacade.addPaymentMethod(paymentMethod).subscribe({
      next: savedPaymentMethod => {
        this.updateAccountPaymentMethods(savedPaymentMethod);
        this.selectedPaymentMethodId = savedPaymentMethod.id;
        this.newPayment = this.emptyPayment();
        this.newPaymentVisible = false;
        this.saving.set(false);
      },
      error: error => { this.saving.set(false); this.errorMessage.set(this.toMessage(error)); }
    });
  }

  submitCheckout(): void {
    const userName = this.auth.userName();
    const customerId = this.auth.customerId();
    const address = this.selectedAddress();
    const paymentMethod = this.selectedPaymentMethod();
    if (!userName || !customerId || !address || !paymentMethod) {
      this.errorMessage.set('Select or create an address and payment method before checkout.');
      return;
    }
    const request: CheckoutBasketRequest = {
      basketCheckout: {
        userName,
        customerId,
        totalPrice: 0,
        address: this.toCheckoutAddress(address),
        payment: {
          token: paymentMethod.token,
          cardholderName: paymentMethod.cardholderName,
          brand: paymentMethod.brand,
          last4: paymentMethod.last4,
          expiration: paymentMethod.expiration
        }
      }
    };
    this.saving.set(true);
    this.basketService.checkout(request).subscribe({
      next: response => {
        this.saving.set(false);
        if (!response.isSuccess) { this.errorMessage.set('Checkout could not be completed.'); return; }
        this.checkoutDialogVisible.set(false);
        this.refresh();
      },
      error: error => { this.saving.set(false); this.errorMessage.set(this.toMessage(error)); }
    });
  }

  lineTotal(item: { quantity: number; price: number }): number { return item.quantity * item.price; }
  selectedAddress(): SavedAddress | undefined { return this.account()?.addresses.find(address => address.id === this.selectedAddressId); }
  selectedPaymentMethod(): SavedPaymentMethod | undefined { return this.account()?.paymentMethods.find(paymentMethod => paymentMethod.id === this.selectedPaymentMethodId); }

  private updateAccountAddresses(address: SavedAddress): void {
    const account = this.account();
    if (account) this.account.set({ ...account, addresses: [...account.addresses, address] });
  }

  private updateAccountPaymentMethods(paymentMethod: SavedPaymentMethod): void {
    const account = this.account();
    if (account) this.account.set({ ...account, paymentMethods: [...account.paymentMethods, paymentMethod] });
  }

  private toCheckoutAddress(address: SavedAddress) {
    return { firstName: address.firstName, lastName: address.lastName, emailAddress: address.email, phone: address.phone, addressLine1: address.addressLine1, addressLine2: address.addressLine2, city: address.city, state: address.state, postalCode: address.postalCode, countryCode: address.countryCode };
  }

  private loadBaskets(event: GraphqlLazyLoadEvent): void {
    if (!this.auth.userName()) return;
    const ownBasketEvent = { ...event, filters: { ...(event.filters ?? {}), userName: { value: this.auth.userName(), matchMode: 'equals' } } };
    this.loading.set(true); this.errorMessage.set('');
    this.basketService.getBaskets(ownBasketEvent).subscribe({
      next: page => { this.tableFirst.set(page.pageIndex * page.pageSize); this.totalRecords.set(page.count); this.baskets.set(page.data); this.loading.set(false); },
      error: error => { this.loading.set(false); this.errorMessage.set(this.toMessage(error)); }
    });
  }

  private emptyAddress(): SaveAddress {
    return { label: '', firstName: this.auth.profile()?.firstName ?? '', lastName: this.auth.profile()?.lastName ?? '', email: this.auth.profile()?.email ?? '', phone: '', addressLine1: '', addressLine2: '', city: '', state: '', postalCode: '', countryCode: '', isDefaultShipping: !this.account()?.addresses.length, isDefaultBilling: !this.account()?.addresses.length };
  }

  private emptyPayment() { return { label: '', cardholderName: '', cardNumber: '', expiration: '' }; }
  private cardBrand(cardNumber: string): string { return cardNumber.startsWith('4') ? 'Visa' : cardNumber.startsWith('5') ? 'Mastercard' : 'Card'; }
  private createDefaultLazyLoadEvent(): GraphqlLazyLoadEvent { return { first: 0, rows: this.pageSize, sortField: 'userName', sortOrder: 1, filters: {} }; }
  private normalizeLazyLoadEvent(event: TableLazyLoadEvent): GraphqlLazyLoadEvent { const sortField = Array.isArray(event.sortField) ? event.sortField[0] : event.sortField; return { first: typeof event.first === 'number' && event.first >= 0 ? event.first : 0, rows: typeof event.rows === 'number' && event.rows > 0 ? event.rows : this.pageSize, sortField: sortField ?? 'userName', sortOrder: event.sortOrder ?? 1, filters: event.filters ?? {} }; }
  private toMessage(error: unknown): string { return error instanceof Error ? error.message : 'Unable to update your cart.'; }
}
