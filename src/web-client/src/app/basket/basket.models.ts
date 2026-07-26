export interface BasketListItem {
  id: string;
  userName: string;
  itemCount: number;
  totalPrice: number;
}

export interface CreateBasketItem {
  productId: string;
  quantity: number;
  color: string;
  price: number;
  productName: string;
}

export interface CreateBasketRequest {
  shoppingCart: {
    id: string;
    userName: string;
    items: CreateBasketItem[];
  };
}

export interface GraphqlResponse<T> {
  data?: T;
  errors?: Array<{ message: string }>;
}

export interface PaginatedResult<T> {
  pageIndex: number;
  pageSize: number;
  count: number;
  data: T[];
}
