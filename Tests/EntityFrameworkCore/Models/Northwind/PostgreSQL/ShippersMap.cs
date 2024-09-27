using LinqToDB.EntityFrameworkCore.Tests.Models.Northwind;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinqToDB.EntityFrameworkCore.Tests.PostgreSQL.Models.Northwind.Mapping
{
	public class ShippersMap : BaseEntityMap<Shipper>
	{
		public override void Configure(EntityTypeBuilder<Shipper> builder)
		{
			base.Configure(builder);

			builder.HasKey(e => e.ShipperId);

			builder.Property(e => e.ShipperId).HasColumnName("ShipperID")
				.ValueGeneratedNever();

			builder.Property(e => e.CompanyName)
				.IsRequired()
				.HasMaxLength(40);

			builder.Property(e => e.Phone).HasMaxLength(24); 
		}
	}
}
