import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  GraphqlCollectionVariables,
  GraphqlLazyLoadEvent,
  GraphqlQueryBuilderService
} from '../shared/graphql/graphql-query-builder.service';
import {
  AddBasketItemRequest,
  BasketCommandResponse,
  BasketListItem,
  CheckoutBasketRequest,
  CreateBasketRequest,
  GraphqlResponse,
  PaginatedResult
} from './basket.models';

interface BasketsQueryResult {
  baskets: {
    items: BasketListItem[];
    totalCount: number;
  };
}

@Injectable({
  providedIn: 'root'
})
export class BasketService {
  private readonly httpClient = inject(HttpClient);
  private readonly graphqlQueryBuilder = inject(GraphqlQueryBuilderService);
  private readonly basketsQuery = this.graphqlQueryBuilder.buildQuery({
    operationName: 'Baskets',
    variableDefinitions: {
      skip: 'Int',
      take: 'Int',
      where: 'BasketListItemFilterInput',
      order: '[BasketListItemSortInput!]'
    },
    rootField: 'baskets',
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
          'userName',
          'itemCount',
          'totalPrice',
          { name: 'items', fields: ['productId', 'productName', 'color', 'quantity', 'price'] }
        ]
      }
    ]
  });

  getBaskets(event: GraphqlLazyLoadEvent): Observable<PaginatedResult<BasketListItem>> {
    const variables = this.graphqlQueryBuilder.buildCollectionVariables(event, 10);

    return this.graphql<BasketsQueryResult, GraphqlCollectionVariables>(this.basketsQuery, variables).pipe(
      map(response => ({
        pageIndex: Math.floor(variables.skip / variables.take),
        pageSize: variables.take,
        count: response.baskets.totalCount,
        data: response.baskets.items
      }))
    );
  }

  createBasket(request: CreateBasketRequest): Observable<void> {
    return this.httpClient.post<void>(`${environment.apiUrl}/basket`, request);
  }

  addItem(userName: string, productId: string, quantity: number, color: string): Observable<BasketCommandResponse> {
    const request: AddBasketItemRequest = {
      userName,
      shoppingCartItem: {
        id: crypto.randomUUID(),
        shoppingCartId: crypto.randomUUID(),
        productId,
        quantity,
        color,
        price: 0,
        productName: ''
      }
    };
    return this.httpClient.post<BasketCommandResponse>(
      `${environment.apiUrl}/basket/${encodeURIComponent(userName)}/items`,
      request
    );
  }

  removeItem(userName: string, productId: string): Observable<BasketCommandResponse> {
    return this.httpClient.delete<BasketCommandResponse>(
      `${environment.apiUrl}/basket/${encodeURIComponent(userName)}/items/${productId}`
    );
  }

  checkout(request: CheckoutBasketRequest): Observable<BasketCommandResponse> {
    return this.httpClient.post<BasketCommandResponse>(`${environment.apiUrl}/basket/checkout`, request);
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

