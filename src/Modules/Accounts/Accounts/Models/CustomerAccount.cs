namespace Accounts.Accounts.Models;

public sealed class CustomerAccount : Aggregate<Guid>
{
  private readonly List<SavedAddress> _addresses = [];
  private readonly List<SavedPaymentMethod> _paymentMethods = [];

  public IReadOnlyCollection<SavedAddress> Addresses => _addresses.AsReadOnly();
  public IReadOnlyCollection<SavedPaymentMethod> PaymentMethods => _paymentMethods.AsReadOnly();
  public string Locale { get; private set; } = "en-US";
  public string Currency { get; private set; } = "USD";
  public bool OrderStatusNotifications { get; private set; } = true;
  public bool MarketingEmails { get; private set; }

  public static CustomerAccount Create(Guid customerId) => new() { Id = customerId };

  public SavedAddress AddAddress(AddressData address)
  {
    var savedAddress = SavedAddress.Create(Guid.NewGuid(), Id, address);
    if (address.IsDefaultShipping)
    {
      ClearDefaultShipping();
    }

    if (address.IsDefaultBilling)
    {
      ClearDefaultBilling();
    }

    _addresses.Add(savedAddress);
    return savedAddress;
  }

  public bool UpdateAddress(Guid addressId, AddressData address)
  {
    SavedAddress? savedAddress = _addresses.SingleOrDefault(item => item.Id == addressId);
    if (savedAddress is null)
    {
      return false;
    }

    if (address.IsDefaultShipping)
    {
      ClearDefaultShipping();
    }

    if (address.IsDefaultBilling)
    {
      ClearDefaultBilling();
    }

    savedAddress.Update(address);
    return true;
  }

  public bool RemoveAddress(Guid addressId)
  {
    SavedAddress? address = _addresses.SingleOrDefault(item => item.Id == addressId);
    return address is not null && _addresses.Remove(address);
  }

  public SavedPaymentMethod AddPaymentMethod(PaymentMethodData paymentMethod)
  {
    var savedPaymentMethod = SavedPaymentMethod.Create(Guid.NewGuid(), Id, paymentMethod);
    if (paymentMethod.IsDefault || _paymentMethods.Count == 0)
    {
      ClearDefaultPaymentMethod();
      savedPaymentMethod.SetDefault();
    }

    _paymentMethods.Add(savedPaymentMethod);
    return savedPaymentMethod;
  }

  public bool UpdatePaymentMethod(Guid paymentMethodId, PaymentMethodData paymentMethod)
  {
    SavedPaymentMethod? savedPaymentMethod = _paymentMethods.SingleOrDefault(item => item.Id == paymentMethodId);
    if (savedPaymentMethod is null)
    {
      return false;
    }

    if (paymentMethod.IsDefault)
    {
      ClearDefaultPaymentMethod();
    }

    savedPaymentMethod.Update(paymentMethod);
    return true;
  }

  public bool RemovePaymentMethod(Guid paymentMethodId)
  {
    SavedPaymentMethod? paymentMethod = _paymentMethods.SingleOrDefault(item => item.Id == paymentMethodId);
    return paymentMethod is not null && _paymentMethods.Remove(paymentMethod);
  }

  public bool SetDefaultPaymentMethod(Guid paymentMethodId)
  {
    SavedPaymentMethod? paymentMethod = _paymentMethods.SingleOrDefault(item => item.Id == paymentMethodId);
    if (paymentMethod is null)
    {
      return false;
    }

    ClearDefaultPaymentMethod();
    paymentMethod.SetDefault();
    return true;
  }

  public void UpdatePreferences(
    string locale,
    string currency,
    bool orderStatusNotifications,
    bool marketingEmails)
  {
    Locale = locale;
    Currency = currency;
    OrderStatusNotifications = orderStatusNotifications;
    MarketingEmails = marketingEmails;
  }

  private void ClearDefaultShipping()
  {
    foreach (SavedAddress address in _addresses.Where(item => item.IsDefaultShipping))
    {
      address.ClearDefaultShipping();
    }
  }

  private void ClearDefaultBilling()
  {
    foreach (SavedAddress address in _addresses.Where(item => item.IsDefaultBilling))
    {
      address.ClearDefaultBilling();
    }
  }

  private void ClearDefaultPaymentMethod()
  {
    foreach (SavedPaymentMethod paymentMethod in _paymentMethods.Where(item => item.IsDefault))
    {
      paymentMethod.ClearDefault();
    }
  }
}

public sealed record AddressData(
  string Label,
  string FirstName,
  string LastName,
  string Email,
  string Phone,
  string AddressLine1,
  string? AddressLine2,
  string City,
  string State,
  string PostalCode,
  string CountryCode,
  bool IsDefaultShipping,
  bool IsDefaultBilling);
