import { CurrencyPipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { TooltipModule } from 'primeng/tooltip';
import { AuthService } from '../../../../core/auth/auth.service';
import { OrderListItem, OrderingFacade } from '../../../ordering';
import { Account, AccountPreferences, SaveAddress, SavedAddress } from '../../data-access/account.models';
import { AccountService } from '../../data-access/account.api';

type AccountSection = 'overview' | 'personal' | 'addresses' | 'payments' | 'orders' | 'preferences' | 'security';

@Component({
  selector: 'app-account-profile-page',
  imports: [ButtonModule, CurrencyPipe, DialogModule, InputTextModule, ReactiveFormsModule, TooltipModule],
  templateUrl: './account-profile.page.html',
  styleUrl: './account-profile.page.scss'
})
export class AccountProfilePage {
  private readonly accountService = inject(AccountService);
  private readonly orderingFacade = inject(OrderingFacade);
  private readonly formBuilder = inject(FormBuilder);
  readonly auth = inject(AuthService);
  readonly activeSection = signal<AccountSection>('overview');
  readonly account = signal<Account | null>(null);
  readonly orders = signal<OrderListItem[]>([]);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly errorMessage = signal('');
  readonly addressDialogVisible = signal(false);
  readonly editingAddressId = signal<string | null>(null);
  readonly sections: { id: AccountSection; label: string; icon: string }[] = [
    { id: 'overview', label: 'Overview', icon: 'pi pi-home' },
    { id: 'personal', label: 'Personal information', icon: 'pi pi-id-card' },
    { id: 'addresses', label: 'Addresses', icon: 'pi pi-map-marker' },
    { id: 'payments', label: 'Payments', icon: 'pi pi-credit-card' },
    { id: 'orders', label: 'Orders', icon: 'pi pi-receipt' },
    { id: 'preferences', label: 'Preferences', icon: 'pi pi-sliders-h' },
    { id: 'security', label: 'Security', icon: 'pi pi-shield' }
  ];

  readonly addressForm = this.formBuilder.nonNullable.group({
    label: ['', [Validators.required, Validators.maxLength(40)]],
    firstName: ['', Validators.required],
    lastName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    phone: ['', Validators.required],
    addressLine1: ['', Validators.required],
    addressLine2: [''],
    city: ['', Validators.required],
    state: ['', Validators.required],
    postalCode: ['', Validators.required],
    countryCode: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(2)]],
    isDefaultShipping: [false],
    isDefaultBilling: [false]
  });

  readonly preferencesForm = this.formBuilder.nonNullable.group({
    locale: ['en-US', Validators.required],
    currency: ['USD', Validators.required],
    orderStatusNotifications: [true],
    marketingEmails: [false]
  });

  constructor() {
    this.loadAccount();
    this.loadOrders();
  }

  selectSection(section: AccountSection): void {
    this.activeSection.set(section);
  }

  openAddressDialog(address?: SavedAddress): void {
    this.editingAddressId.set(address?.id ?? null);
    this.addressForm.reset({
      label: address?.label ?? '',
      firstName: address?.firstName ?? this.auth.profile()?.firstName ?? '',
      lastName: address?.lastName ?? this.auth.profile()?.lastName ?? '',
      email: address?.email ?? this.auth.profile()?.email ?? '',
      phone: address?.phone ?? '',
      addressLine1: address?.addressLine1 ?? '',
      addressLine2: address?.addressLine2 ?? '',
      city: address?.city ?? '',
      state: address?.state ?? '',
      postalCode: address?.postalCode ?? '',
      countryCode: address?.countryCode ?? '',
      isDefaultShipping: address?.isDefaultShipping ?? false,
      isDefaultBilling: address?.isDefaultBilling ?? false
    });
    this.addressDialogVisible.set(true);
  }

  closeAddressDialog(): void {
    this.addressDialogVisible.set(false);
    this.errorMessage.set('');
  }

  saveAddress(): void {
    if (this.addressForm.invalid) {
      this.addressForm.markAllAsTouched();
      return;
    }

    const value = this.addressForm.getRawValue();
    const address: SaveAddress = {
      ...value,
      addressLine2: value.addressLine2 || null,
      countryCode: value.countryCode.toUpperCase()
    };
    const addressId = this.editingAddressId();
    const request = addressId
      ? this.accountService.updateAddress(addressId, address)
      : this.accountService.addAddress(address);

    this.saving.set(true);
    this.errorMessage.set('');
    request.subscribe({
      next: () => {
        this.saving.set(false);
        this.addressDialogVisible.set(false);
        this.loadAccount();
      },
      error: error => this.handleError(error, 'Unable to save the address.')
    });
  }

  deleteAddress(address: SavedAddress): void {
    if (!window.confirm(`Delete ${address.label}?`)) {
      return;
    }

    this.saving.set(true);
    this.accountService.deleteAddress(address.id).subscribe({
      next: () => {
        this.saving.set(false);
        this.loadAccount();
      },
      error: error => this.handleError(error, 'Unable to delete the address.')
    });
  }

  savePreferences(): void {
    if (this.preferencesForm.invalid) {
      return;
    }

    this.saving.set(true);
    const preferences: AccountPreferences = this.preferencesForm.getRawValue();
    this.accountService.updatePreferences(preferences).subscribe({
      next: account => {
        this.account.set(account);
        this.saving.set(false);
      },
      error: error => this.handleError(error, 'Unable to save preferences.')
    });
  }

  defaultShipping(): SavedAddress | undefined {
    return this.account()?.addresses.find(address => address.isDefaultShipping);
  }

  defaultBilling(): SavedAddress | undefined {
    return this.account()?.addresses.find(address => address.isDefaultBilling);
  }

  private loadAccount(): void {
    this.loading.set(true);
    this.errorMessage.set('');
    this.accountService.getAccount().subscribe({
      next: account => {
        this.account.set(account);
        this.preferencesForm.setValue(account.preferences);
        this.loading.set(false);
      },
      error: error => this.handleError(error, 'Unable to load your account.')
    });
  }

  private loadOrders(): void {
    this.orderingFacade.getOrders({
      first: 0,
      rows: 5,
      sortField: 'orderName',
      sortOrder: -1,
      filters: {}
    }).subscribe({
      next: page => this.orders.set(page.data),
      error: () => this.orders.set([])
    });
  }

  private handleError(error: unknown, fallback: string): void {
    this.saving.set(false);
    this.loading.set(false);
    this.errorMessage.set(error instanceof Error ? error.message : fallback);
  }
}
