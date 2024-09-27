using LinqToDB.EntityFrameworkCore.Tests.Models.Northwind;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinqToDB.EntityFrameworkCore.Tests.PostgreSQL.Models.Northwind.Mapping
{
	public class CustomerCustomerDemoMap : BaseEntityMap<CustomerCustomerDemo>
	{
		public override void Configure(EntityTypeBuilder<CustomerCustomerDemo> builder)
		{
			base.Configure(builder);

			builder.HasKey(e => new { e.CustomerId, e.CustomerTypeId })
				.IsClustered(false);

			builder.Property(e => e.CustomerId)
				.HasColumnName("CustomerID")
				.HasMaxLength(5);

			builder.Property(e => e.CustomerTypeId)
				.HasColumnName("CustomerTypeID")
				.HasMaxLength(10);

			builder.HasOne(d => d.Customer)
				.WithMany(p => p.CustomerCustomerDemo)
				.HasForeignKey(d => d.CustomerId)
				.OnDelete(DeleteBehavior.ClientSetNull)
				.HasConstraintName("FK_CustomerCustomerDemo_Customers");

			builder.HasOne(d => d.CustomerType)
				.WithMany(p => p.CustomerCustomerDemo)
				.HasForeignKey(d => d.CustomerTypeId)
				.OnDelete(DeleteBehavior.ClientSetNull)
				.HasConstraintName("FK_CustomerCustomerDemo");
		}
	}
}
