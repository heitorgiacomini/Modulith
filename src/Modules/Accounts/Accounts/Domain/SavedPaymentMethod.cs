namespace Accounts.Accounts.Domain;

using Shared.Data.Auditing;

public sealed class SavedPaymentMethod : FullAuditedEntity<Guid>, IMultiTenant
{
  public Guid CustomerAccountId { get; private set; }
  public Guid? TenantId { get; set; }
  public string Label { get; private set; } = default!;
  [AuditSensitive] public string CardholderName { get; private set; } = default!;
  [AuditSensitive] public string Brand { get; private set; } = default!;
  [AuditSensitive] public string Last4 { get; private set; } = default!;
  [AuditSensitive] public string Expiration { get; private set; } = default!;
  [AuditSensitive] public string Token { get; private set; } = default!;
  public bool IsDefault { get; private set; }

  private SavedPaymentMethod()
  {
  }

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
