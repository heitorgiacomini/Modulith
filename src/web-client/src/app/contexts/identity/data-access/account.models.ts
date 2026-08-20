export interface Account {
  preferences: AccountPreferences;
  addresses: SavedAddress[];
  paymentMethods: SavedPaymentMethod[];
}

export interface AccountPreferences {
  locale: string;
  currency: string;
  orderStatusNotifications: boolean;
  marketingEmails: boolean;
}

export interface SavedAddress {
  id: string;
  label: string;
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
  addressLine1: string;
  addressLine2: string | null;
  city: string;
  state: string;
  postalCode: string;
  countryCode: string;
  isDefaultShipping: boolean;
  isDefaultBilling: boolean;
}

export type SaveAddress = Omit<SavedAddress, 'id'>;

export interface SavedPaymentMethod {
  id: string;
  label: string;
  cardholderName: string;
  brand: string;
  last4: string;
  expiration: string;
  token: string;
  isDefault: boolean;
}

export type SavePaymentMethod = Omit<SavedPaymentMethod, 'id'>;
