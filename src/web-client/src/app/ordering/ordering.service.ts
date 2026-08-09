import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  GraphqlCollectionVariables,
  GraphqlLazyLoadEvent,
  GraphqlQueryBuilderService
} from '../shared/graphql/graphql-query-builder.service';
import { CreateOrderRequest, GraphqlResponse, OrderListItem, PaginatedResult } from './ordering.models';

interface OrdersQueryResult {
  orders: {
    items: OrderListItem[];
    totalCount: number;
  };
}

@Injectable({
  providedIn: 'root'
})
export class OrderingService {
  private readonly httpClient = inject(HttpClient);
  private readonly graphqlQueryBuilder = inject(GraphqlQueryBuilderService);
  private readonly ordersQuery = this.graphqlQueryBuilder.buildQuery({
    operationName: 'Orders',
    variableDefinitions: {
      skip: 'Int',
      take: 'Int',
      where: 'OrderListItemFilterInput',
      order: '[OrderListItemSortInput!]'
    },
    rootField: 'orders',
    rootArguments: {
      skip: '$skip',
      take: '$take',
      where: '$where',
      order: '$order'
    },
    selection: [
      'totalCount',
      {
        name: 'items',
        fields: [
          'id',
          'customerId',
          'orderName',
          'itemCount',
          'totalPrice',
          {
            name: 'items',
            fields: ['productId', { name: 'product', fields: ['name'] }, 'quantity', 'price']
          }
        ]
      }
    ]
  });

  getOrders(event: GraphqlLazyLoadEvent): Observable<PaginatedResult<OrderListItem>> {
    const variables = this.graphqlQueryBuilder.buildCollectionVariables(event, 10);

    return this.graphql<OrdersQueryResult, GraphqlCollectionVariables>(this.ordersQuery, variables).pipe(
      map(response => ({
        pageIndex: Math.floor(variables.skip / variables.take),
        pageSize: variables.take,
        count: response.orders.totalCount,
        data: response.orders.items
      }))
    );
  }

  createOrder(request: CreateOrderRequest): Observable<void> {
    return this.httpClient.post<void>(`${environment.apiUrl}/orders`, request);
  }

  private graphql<T, TVariables extends object>(query: string, variables: TVariables): Observable<T> {
    return this.httpClient
      .post<GraphqlResponse<T>>(environment.graphqlUrl, { query, variables })
      .pipe(map(response => this.unwrapResponse(response)));
  }

  private unwrapResponse<T>(response: GraphqlResponse<T>): T {
    if (response.errors?.length) {
      throw new Error(response.errors.map(error => error.message).join(', '));
    }

    if (!response.data) {
      throw new Error('GraphQL response did not include data.');
    }

    return response.data;
  }
}


