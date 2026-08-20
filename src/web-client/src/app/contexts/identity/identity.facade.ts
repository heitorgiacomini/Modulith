import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { AccountService } from './data-access/account.api';
import { Account, SaveAddress, SavePaymentMethod, SavedAddress, SavedPaymentMethod } from './data-access/account.models';

@Injectable({ providedIn: 'root' })
export class IdentityFacade {
  private readonly accountService = inject(AccountService);

  getAccount(): Observable<Account> { return this.accountService.getAccount(); }
  addAddress(address: SaveAddress): Observable<SavedAddress> { return this.accountService.addAddress(address); }
  addPaymentMethod(paymentMethod: SavePaymentMethod): Observable<SavedPaymentMethod> {
    return this.accountService.addPaymentMethod(paymentMethod);
  }
}
