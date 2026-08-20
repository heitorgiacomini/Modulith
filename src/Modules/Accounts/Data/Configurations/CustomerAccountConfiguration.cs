using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Accounts.Data.Configurations;

public sealed class CustomerAccountConfiguration : IEntityTypeConfiguration<CustomerAccount>
{
  public void Configure(EntityTypeBuilder<CustomerAccount> builder)
  {
    builder.HasKey(account => account.Id);
    builder.Property(account => account.Locale).HasMaxLength(10).IsRequired();
    builder.Property(account => account.Currency).HasMaxLength(3).IsRequired();
    builder.HasMany(account => account.Addresses)
      .WithOne()
      .HasForeignKey(address => address.CustomerAccountId)
      .OnDelete(DeleteBehavior.Cascade);
    builder.Navigation(account => account.Addresses).UsePropertyAccessMode(PropertyAccessMode.Field);
    builder.HasMany(account => account.PaymentMethods)
      .WithOne()
      .HasForeignKey(paymentMethod => paymentMethod.CustomerAccountId)
      .OnDelete(DeleteBehavior.Cascade);
    builder.Navigation(account => account.PaymentMethods).UsePropertyAccessMode(PropertyAccessMode.Field);
  }
}
