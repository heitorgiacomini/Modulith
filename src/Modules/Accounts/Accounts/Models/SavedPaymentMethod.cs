namespace Accounts.Accounts.Models;

public sealed class SavedPaymentMethod : Entity<Guid>
{
  public Guid CustomerAccountId { get; private set; }
  public string Label { get; private set; } = default!;
  public string CardholderName { get; private set; } = default!;
  public string Brand { get; private set; } = default!;
  public string Last4 { get; private set; } = default!;
  public string Expiration { get; private set; } = default!;
  public string Token { get; private set; } = default!;
  public bool IsDefault { get; private set; }

  internal static SavedPaymentMethod Create(Guid id, Guid accountId, PaymentMethodData paymentMethod)
  {
    var savedPaymentMethod = new SavedPaymentMethod { Id = id, CustomerAccountId = accountId };
    savedPaymentMethod.Update(paymentMethod);
    return savedPaymentMethod;
  }

  internal void Update(PaymentMethodData paymentMethod)
  {
    Label = paymentMethod.Label;
    CardholderName = paymentMethod.CardholderName;
    Brand = paymentMethod.Brand;
    Last4 = paymentMethod.Last4;
    Expiration = paymentMethod.Expiration;
    Token = paymentMethod.Token;
    IsDefault = paymentMethod.IsDefault;
  }

  internal void ClearDefault() => IsDefault = false;
  internal void SetDefault() => IsDefault = true;
}

public sealed record PaymentMethodData(
  string Label,
  string CardholderName,
  string Brand,
  string Last4,
  string Expiration,
  string Token,
  bool IsDefault);
