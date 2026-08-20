import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { Account, AccountPreferences, SaveAddress, SavePaymentMethod, SavedAddress, SavedPaymentMethod } from './account.models';

@Injectable({ providedIn: 'root' })
export class AccountService {
  private readonly httpClient = inject(HttpClient);
  private readonly accountUrl = `${environment.apiUrl}/account/me`;

  getAccount(): Observable<Account> {
    return this.httpClient.get<Account>(this.accountUrl);
  }

  updatePreferences(preferences: AccountPreferences): Observable<Account> {
    return this.httpClient.put<Account>(`${this.accountUrl}/preferences`, preferences);
  }

  addAddress(address: SaveAddress): Observable<SavedAddress> {
    return this.httpClient.post<SavedAddress>(`${this.accountUrl}/addresses`, address);
  }

  updateAddress(id: string, address: SaveAddress): Observable<SavedAddress> {
    return this.httpClient.put<SavedAddress>(`${this.accountUrl}/addresses/${id}`, address);
  }

  deleteAddress(id: string): Observable<void> {
    return this.httpClient.delete<void>(`${this.accountUrl}/addresses/${id}`);
  }

  addPaymentMethod(paymentMethod: SavePaymentMethod): Observable<SavedPaymentMethod> {
    return this.httpClient.post<SavedPaymentMethod>(`${this.accountUrl}/payment-methods`, paymentMethod);
  }

  deletePaymentMethod(id: string): Observable<void> {
    return this.httpClient.delete<void>(`${this.accountUrl}/payment-methods/${id}`);
  }
}
