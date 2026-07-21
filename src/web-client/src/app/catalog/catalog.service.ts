import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { GraphqlResponse, PaginatedResult, ProductDto } from './catalog.models';

interface ProductsQueryResult {
  products: PaginatedResult<ProductDto>;
}

interface ProductQueryResult {
  product: ProductDto | null;
}

const PRODUCTS_QUERY = `
  query Products($pageIndex: Int!, $pageSize: Int!) {
    products(pageIndex: $pageIndex, pageSize: $pageSize) {
      pageIndex
      pageSize
      count
      data {
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

  getProducts(pageIndex = 0, pageSize = 6): Observable<PaginatedResult<ProductDto>> {
    return this.graphql<ProductsQueryResult>(PRODUCTS_QUERY, { pageIndex, pageSize }).pipe(
      map(response => response.products)
    );
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
