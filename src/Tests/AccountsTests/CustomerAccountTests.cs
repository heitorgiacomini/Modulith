using Accounts.Accounts.Models;
using Xunit;

namespace AccountsTests;

public sealed class CustomerAccountTests
{
  [Fact]
  public void AddAddress_ReassignsShippingDefault()
  {
    CustomerAccount account = CustomerAccount.Create(Guid.NewGuid());
    SavedAddress first = account.AddAddress(CreateAddress("Home", isDefaultShipping: true));

    SavedAddress second = account.AddAddress(CreateAddress("Work", isDefaultShipping: true));

    Assert.False(first.IsDefaultShipping);
    Assert.True(second.IsDefaultShipping);
  }

  [Fact]
  public void UpdateAddress_ReturnsFalseForUnknownAddress()
  {
    CustomerAccount account = CustomerAccount.Create(Guid.NewGuid());

    bool updated = account.UpdateAddress(Guid.NewGuid(), CreateAddress("Unknown"));

    Assert.False(updated);
    Assert.Empty(account.Addresses);
  }

  [Fact]
  public void UpdatePreferences_ReplacesCommercePreferences()
  {
    CustomerAccount account = CustomerAccount.Create(Guid.NewGuid());

    account.UpdatePreferences("pt-BR", "BRL", false, true);

    Assert.Equal("pt-BR", account.Locale);
    Assert.Equal("BRL", account.Currency);
    Assert.False(account.OrderStatusNotifications);
    Assert.True(account.MarketingEmails);
  }

  private static AddressData CreateAddress(string label, bool isDefaultShipping = false) => new(
    label,
    "Ada",
    "Lovelace",
    "ada@example.test",
    "+1 555 0100",
    "1 Computing Lane",
    null,
    "London",
    "London",
    "SW1A 1AA",
    "GB",
    isDefaultShipping,
    false);
}
