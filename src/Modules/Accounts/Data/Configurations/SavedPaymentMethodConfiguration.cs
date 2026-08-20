using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Accounts.Data.Configurations;

public sealed class SavedPaymentMethodConfiguration : IEntityTypeConfiguration<SavedPaymentMethod>
{
  public void Configure(EntityTypeBuilder<SavedPaymentMethod> builder)
  {
    builder.HasKey(paymentMethod => paymentMethod.Id);
    builder.Property(paymentMethod => paymentMethod.Label).HasMaxLength(40).IsRequired();
    builder.Property(paymentMethod => paymentMethod.CardholderName).HasMaxLength(100).IsRequired();
    builder.Property(paymentMethod => paymentMethod.Brand).HasMaxLength(30).IsRequired();
    builder.Property(paymentMethod => paymentMethod.Last4).HasMaxLength(4).IsRequired();
    builder.Property(paymentMethod => paymentMethod.Expiration).HasMaxLength(10).IsRequired();
    builder.Property(paymentMethod => paymentMethod.Token).HasMaxLength(200).IsRequired();
    builder.HasIndex(paymentMethod => new { paymentMethod.CustomerAccountId, paymentMethod.Label });
  }
}
