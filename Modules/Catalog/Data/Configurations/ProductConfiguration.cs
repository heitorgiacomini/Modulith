namespace Catalog.Data.Configurations
{
	public class ProductConfiguration : IEntityTypeConfiguration<Product>
	{
		public void Configure(EntityTypeBuilder<Product> builder)
		{
			_ = builder.HasKey(x => x.Id);
			_ = builder.Property(x => x.Name).IsRequired().HasMaxLength(50);
			_ = builder.Property(x => x.Category).IsRequired();
			_ = builder.Property(x => x.Description).HasMaxLength(200);
			_ = builder.Property(x => x.ImageFile).HasMaxLength(100);
			_ = builder.Property(x => x.Price).IsRequired();

		}
	}
}
