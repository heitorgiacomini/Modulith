import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  GraphqlCollectionVariables,
  GraphqlLazyLoadEvent,
  GraphqlQueryBuilderService,
  GraphqlSelectionNode
} from '../shared/graphql/graphql-query-builder.service';
import { GraphqlResponse, PaginatedResult, ProductDto } from './catalog.models';

interface ProductsQueryResult {
  products: {
    items: ProductDto[];
    totalCount: number;
  };
}

interface ProductQueryResult {
  product: ProductDto | null;
}

const PRODUCT_FIELDS: Array<string | GraphqlSelectionNode> = [
  'id',
  'name',
  'category',
  'description',
  'imageFile',
  'price'
];

@Injectable({
  providedIn: 'root'
})
export class CatalogService {
  private readonly httpClient = inject(HttpClient);
  private readonly graphqlQueryBuilder = inject(GraphqlQueryBuilderService);
  private readonly productsQuery = this.graphqlQueryBuilder.buildQuery({
    operationName: 'Products',
    variableDefinitions: {
      skip: 'Int',
      take: 'Int',
      where: 'ProductFilterInput',
      order: '[ProductSortInput!]'
    },
    rootField: 'products',
    rootArguments: {
      skip: '$skip',
      take: '$take',
      where: '$where',
      order: '$order'
    },
    selection: ['totalCount', { name: 'items', fields: PRODUCT_FIELDS }]
  });
  private readonly productQuery = this.graphqlQueryBuilder.buildQuery({
    operationName: 'Product',
    variableDefinitions: {
      id: 'UUID!'
    },
    rootField: 'product',
    rootArguments: {
      id: '$id'
    },
    selection: PRODUCT_FIELDS
  });

  getProducts(event: GraphqlLazyLoadEvent): Observable<PaginatedResult<ProductDto>> {
    const queryVariables = this.graphqlQueryBuilder.buildCollectionVariables(event, 6);
    const pageSize = typeof queryVariables.take === 'number' ? queryVariables.take : 6;
    const first = typeof queryVariables.skip === 'number' ? queryVariables.skip : 0;

    return this.graphql<ProductsQueryResult, GraphqlCollectionVariables>(
      this.productsQuery,
      queryVariables
    ).pipe(
      map(response => ({
        pageIndex: Math.floor(first / pageSize),
        pageSize,
        count: response.products.totalCount,
        data: response.products.items
      }))
    );
  }

  getProduct(id: string): Observable<ProductDto> {
    return this.graphql<ProductQueryResult>(this.productQuery, { id }).pipe(
      map(response => {
        if (!response.product) {
          throw new Error('Product not found.');
        }

        return response.product;
      })
    );
  }

  private graphql<T, TVariables extends object = Record<string, unknown>>(
    query: string,
    variables: TVariables
  ): Observable<T> {
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
