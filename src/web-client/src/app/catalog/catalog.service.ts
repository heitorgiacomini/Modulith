import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { GraphqlResponse, PaginatedResult, ProductDto } from './catalog.models';

export type ProductSort = 'NAME_ASC' | 'NAME_DESC' | 'PRICE_ASC' | 'PRICE_DESC';

export interface ProductQueryOptions {
  search?: string;
  sort?: ProductSort;
}

interface ProductsQueryResult {
  products: {
    items: ProductDto[];
    totalCount: number;
  };
}

interface ProductQueryResult {
  product: ProductDto | null;
}

const PRODUCTS_QUERY = `
  query Products(
    $skip: Int
    $take: Int
    $where: ProductFilterInput
    $order: [ProductSortInput!]
  ) {
    products(skip: $skip, take: $take, where: $where, order: $order) {
      totalCount
      items {
        id
        name
        category
        description
        imageFile
        price
      }
    }
  }
`;

const PRODUCT_QUERY = `
  query Product($id: UUID!) {
    product(id: $id) {
      id
      name
      category
      description
      imageFile
      price
    }
  }
`;

@Injectable({
  providedIn: 'root'
})
export class CatalogService {
  private readonly httpClient = inject(HttpClient);

  getProducts(
    pageIndex = 0,
    pageSize = 6,
    options: ProductQueryOptions = {}
  ): Observable<PaginatedResult<ProductDto>> {
    const search = options.search?.trim();

    return this.graphql<ProductsQueryResult>(PRODUCTS_QUERY, {
      skip: pageIndex * pageSize,
      take: pageSize,
      where: search ? { name: { contains: search } } : null,
      order: [this.createSortOrder(options.sort)]
    }).pipe(
      map(response => ({
        pageIndex,
        pageSize,
        count: response.products.totalCount,
        data: response.products.items
      }))
    );
  }

  private createSortOrder(sort: ProductSort | undefined): Record<string, string> {
    switch (sort) {
      case 'NAME_DESC':
        return { name: 'DESC' };
      case 'PRICE_ASC':
        return { price: 'ASC' };
      case 'PRICE_DESC':
        return { price: 'DESC' };
      case 'NAME_ASC':
      default:
        return { name: 'ASC' };
    }
  }

  getProduct(id: string): Observable<ProductDto> {
    return this.graphql<ProductQueryResult>(PRODUCT_QUERY, { id }).pipe(
      map(response => {
        if (!response.product) {
          throw new Error('Product not found.');
        }

        return response.product;
      })
    );
  }

  private graphql<T>(query: string, variables: Record<string, unknown>): Observable<T> {
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
