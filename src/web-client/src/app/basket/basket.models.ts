export interface BasketListItem {
  id: string;
  userName: string;
  itemCount: number;
  totalPrice: number;
  items: BasketItemListItem[];
}

export interface BasketItemListItem {
  productId: string;
  productName: string;
  color: string;
  quantity: number;
  price: number;
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

export interface AddBasketItemRequest {
  userName: string;
  shoppingCartItem: {
    id: string;
    shoppingCartId: string;
    productId: string;
    quantity: number;
    color: string;
    price: number;
    productName: string;
  };
}

export interface CheckoutBasketRequest {
  basketCheckout: {
    userName: string;
    customerId: string;
    totalPrice: number;
    firstName: string;
    lastName: string;
    emailAddress: string;
    addressLine: string;
    country: string;
    state: string;
    zipCode: string;
    cardName: string;
    cardNumber: string;
    expiration: string;
    cvv: string;
    paymentMethod: number;
  };
}

export interface BasketCommandResponse {
  id?: string;
  isSuccess?: boolean;
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
