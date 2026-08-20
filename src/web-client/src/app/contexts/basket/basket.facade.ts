import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { BasketCommandResponse } from './data-access/basket.models';
import { BasketService } from './data-access/basket.api';

@Injectable({ providedIn: 'root' })
export class BasketFacade {
  private readonly basketService = inject(BasketService);

  addItem(userName: string, productId: string, quantity: number, color: string): Observable<BasketCommandResponse> {
    return this.basketService.addItem(userName, productId, quantity, color);
  }
}
