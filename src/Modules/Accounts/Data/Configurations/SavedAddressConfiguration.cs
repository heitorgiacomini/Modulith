using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Accounts.Data.Configurations;

public sealed class SavedAddressConfiguration : IEntityTypeConfiguration<SavedAddress>
{
  public void Configure(EntityTypeBuilder<SavedAddress> builder)
  {
    builder.HasKey(address => address.Id);
    builder.Property(address => address.Label).HasMaxLength(40).IsRequired();
    builder.Property(address => address.FirstName).HasMaxLength(50).IsRequired();
    builder.Property(address => address.LastName).HasMaxLength(50).IsRequired();
    builder.Property(address => address.Email).HasMaxLength(100).IsRequired();
    builder.Property(address => address.Phone).HasMaxLength(30).IsRequired();
    builder.Property(address => address.AddressLine1).HasMaxLength(180).IsRequired();
    builder.Property(address => address.AddressLine2).HasMaxLength(180);
    builder.Property(address => address.City).HasMaxLength(80).IsRequired();
    builder.Property(address => address.State).HasMaxLength(80).IsRequired();
    builder.Property(address => address.PostalCode).HasMaxLength(20).IsRequired();
    builder.Property(address => address.CountryCode).HasMaxLength(2).IsRequired();
    builder.HasIndex(address => new { address.CustomerAccountId, address.Label });
  }
}
