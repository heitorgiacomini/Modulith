export interface OrderListItem {
  id: string;
  customerId: string;
  orderName: string;
  itemCount: number;
  totalPrice: number;
  items: OrderItemListItem[];
}

export interface OrderItemListItem {
  productId: string;
  product: {
    name: string;
  };
  quantity: number;
  price: number;
}

export interface CreateOrderRequest {
  order: {
    id: string;
    customerId: string;
    orderName: string;
    shippingAddress: Address;
    billingAddress: Address;
    payment: Payment;
    items: OrderItem[];
  };
}

export interface Address {
  firstName: string;
  lastName: string;
  emailAddress: string;
  addressLine: string;
  country: string;
  state: string;
  zipCode: string;
}

export interface Payment {
  cardName: string;
  cardNumber: string;
  expiration: string;
  cvv: string;
  paymentMethod: number;
}

export interface OrderItem {
  orderId: string;
  productId: string;
  quantity: number;
  price: number;
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
