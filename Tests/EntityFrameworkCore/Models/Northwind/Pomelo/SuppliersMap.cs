using LinqToDB.EntityFrameworkCore.Tests.Models.Northwind;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinqToDB.EntityFrameworkCore.Tests.Pomelo.Models.Northwind.Mapping
{
	public class SuppliersMap : IEntityTypeConfiguration<Supplier>
	{
		public void Configure(EntityTypeBuilder<Supplier> builder)
		{

			builder.HasKey(e => e.SupplierId);

			builder.HasIndex(e => e.CompanyName)
				.HasDatabaseName("CompanyName");

			builder.HasIndex(e => e.PostalCode)
				.HasDatabaseName("PostalCode");

			builder.Property(e => e.SupplierId).HasColumnName("SupplierID")
				.ValueGeneratedNever();

			builder.Property(e => e.Address).HasMaxLength(60);

			builder.Property(e => e.City).HasMaxLength(15);

			builder.Property(e => e.CompanyName)
				.IsRequired()
				.HasMaxLength(40);

			builder.Property(e => e.ContactName).HasMaxLength(30);

			builder.Property(e => e.ContactTitle).HasMaxLength(30);

			builder.Property(e => e.Country).HasMaxLength(15);

			builder.Property(e => e.Fax).HasMaxLength(24);

			builder.Property(e => e.HomePage).HasColumnType("text");

			builder.Property(e => e.Phone).HasMaxLength(24);

			builder.Property(e => e.PostalCode).HasMaxLength(10);

			builder.Property(e => e.Region).HasMaxLength(15);
		}
	}
}
