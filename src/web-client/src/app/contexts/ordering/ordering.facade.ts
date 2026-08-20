import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { GraphqlLazyLoadEvent } from '../../core/graphql/graphql-query-builder.service';
import { OrderListItem, PaginatedResult } from './data-access/ordering.models';
import { OrderingService } from './data-access/ordering.api';

@Injectable({ providedIn: 'root' })
export class OrderingFacade {
  private readonly orderingService = inject(OrderingService);

  getOrders(event: GraphqlLazyLoadEvent): Observable<PaginatedResult<OrderListItem>> {
    return this.orderingService.getOrders(event);
  }
}
