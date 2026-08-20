namespace Accounts.Accounts.Models;

public sealed class SavedAddress : Entity<Guid>
{
  public Guid CustomerAccountId { get; private set; }
  public string Label { get; private set; } = default!;
  public string FirstName { get; private set; } = default!;
  public string LastName { get; private set; } = default!;
  public string Email { get; private set; } = default!;
  public string Phone { get; private set; } = default!;
  public string AddressLine1 { get; private set; } = default!;
  public string? AddressLine2 { get; private set; }
  public string City { get; private set; } = default!;
  public string State { get; private set; } = default!;
  public string PostalCode { get; private set; } = default!;
  public string CountryCode { get; private set; } = default!;
  public bool IsDefaultShipping { get; private set; }
  public bool IsDefaultBilling { get; private set; }

  internal static SavedAddress Create(Guid id, Guid accountId, AddressData address)
  {
    var savedAddress = new SavedAddress { Id = id, CustomerAccountId = accountId };
    savedAddress.Update(address);
    return savedAddress;
  }

  internal void Update(AddressData address)
  {
    Label = address.Label;
    FirstName = address.FirstName;
    LastName = address.LastName;
    Email = address.Email;
    Phone = address.Phone;
    AddressLine1 = address.AddressLine1;
    AddressLine2 = address.AddressLine2;
    City = address.City;
    State = address.State;
    PostalCode = address.PostalCode;
    CountryCode = address.CountryCode.ToUpperInvariant();
    IsDefaultShipping = address.IsDefaultShipping;
    IsDefaultBilling = address.IsDefaultBilling;
  }

  internal void ClearDefaultShipping() => IsDefaultShipping = false;
  internal void ClearDefaultBilling() => IsDefaultBilling = false;
}
