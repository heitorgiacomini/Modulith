export interface ProductDto {
  id: string;
  name: string;
  category: string[];
  description: string;
  imageFile: string;
  price: number;
}

export interface PaginatedResult<T> {
  pageIndex: number;
  pageSize: number;
  count: number;
  data: T[];
}

export interface GraphqlResponse<T> {
  data?: T;
  errors?: Array<{ message: string }>;
}
