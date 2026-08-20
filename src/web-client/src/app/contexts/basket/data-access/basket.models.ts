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
    address: CheckoutAddress;
    payment: CheckoutPayment;
  };
}

export interface CheckoutAddress {
  firstName: string;
  lastName: string;
  emailAddress: string;
  phone: string;
  addressLine1: string;
  addressLine2: string | null;
  city: string;
  state: string;
  postalCode: string;
  countryCode: string;
}

export interface CheckoutPayment {
  token: string;
  cardholderName: string;
  brand: string;
  last4: string;
  expiration: string;
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
