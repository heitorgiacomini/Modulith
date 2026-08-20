using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ordering.Data.Configurations;
public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(o => o.CustomerId);

        builder.HasIndex(e => e.OrderName)
               .IsUnique();

        builder.Property(e => e.OrderName)
               .IsRequired()
               .HasMaxLength(100);

        builder.HasMany(s => s.Items)
           .WithOne()
           .HasForeignKey(si => si.OrderId);

        builder.ComplexProperty(
           o => o.ShippingAddress, addressBuilder =>
           {
               addressBuilder.Property(a => a.FirstName)
                   .HasMaxLength(50)
                   .IsRequired();

               addressBuilder.Property(a => a.LastName)
                    .HasMaxLength(50)
                    .IsRequired();

               addressBuilder.Property(a => a.EmailAddress)
                   .HasMaxLength(50);

               addressBuilder.Property(a => a.Phone)
                   .HasMaxLength(30)
                   .IsRequired();

               addressBuilder.Property(a => a.AddressLine1)
                   .HasMaxLength(180)
                   .IsRequired();

               addressBuilder.Property(a => a.AddressLine2).HasMaxLength(180);

               addressBuilder.Property(a => a.City).HasMaxLength(80).IsRequired();

               addressBuilder.Property(a => a.State)
                   .HasMaxLength(50);

               addressBuilder.Property(a => a.PostalCode)
                   .HasMaxLength(20)
                   .IsRequired();

               addressBuilder.Property(a => a.CountryCode).HasMaxLength(2).IsRequired();
           });

        builder.ComplexProperty(
          o => o.BillingAddress, addressBuilder =>
          {
              addressBuilder.Property(a => a.FirstName)
                   .HasMaxLength(50)
                   .IsRequired();

              addressBuilder.Property(a => a.LastName)
                   .HasMaxLength(50)
                   .IsRequired();

              addressBuilder.Property(a => a.EmailAddress)
                  .HasMaxLength(50);

              addressBuilder.Property(a => a.Phone)
                  .HasMaxLength(30)
                  .IsRequired();

              addressBuilder.Property(a => a.AddressLine1)
                  .HasMaxLength(180)
                  .IsRequired();

              addressBuilder.Property(a => a.AddressLine2).HasMaxLength(180);

              addressBuilder.Property(a => a.City).HasMaxLength(80).IsRequired();

              addressBuilder.Property(a => a.State)
                  .HasMaxLength(50);

              addressBuilder.Property(a => a.PostalCode)
                  .HasMaxLength(20)
                  .IsRequired();

              addressBuilder.Property(a => a.CountryCode).HasMaxLength(2).IsRequired();
          });

        builder.ComplexProperty(
               o => o.Payment, paymentBuilder =>
               {
                   paymentBuilder.Property(p => p.Token).HasMaxLength(200).IsRequired();

                   paymentBuilder.Property(p => p.CardholderName)
                       .HasMaxLength(100)
                       .IsRequired();

                   paymentBuilder.Property(p => p.Brand).HasMaxLength(30).IsRequired();

                   paymentBuilder.Property(p => p.Last4).HasMaxLength(4).IsRequired();

                   paymentBuilder.Property(p => p.Expiration).HasMaxLength(10).IsRequired();
               });
    }
}
