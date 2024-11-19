using LinqToDB.EntityFrameworkCore.Tests.Models.Northwind;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinqToDB.EntityFrameworkCore.Tests.SQLite.Models.Northwind.Mapping
{
	public class OrderDetailsMap : IEntityTypeConfiguration<OrderDetail>
	{
		public void Configure(EntityTypeBuilder<OrderDetail> builder)
		{
			builder.HasKey(e => new { e.OrderId, e.ProductId });

			builder.ToTable("Order Details");

			builder.HasIndex(e => e.OrderId)
				.HasDatabaseName("OrdersOrder_Details");

			builder.HasIndex(e => e.ProductId)
				.HasDatabaseName("ProductsOrder_Details");

			builder.Property(e => e.OrderId).HasColumnName("OrderID");

			builder.Property(e => e.ProductId).HasColumnName("ProductID");

			builder.Property(e => e.Quantity).HasDefaultValue(1);

			builder.Property(e => e.UnitPrice).HasColumnType("decimal(13, 4)");

			builder.HasOne(d => d.Order)
				.WithMany(p => p.OrderDetails)
				.HasForeignKey(d => d.OrderId)
				.OnDelete(DeleteBehavior.ClientSetNull)
				.HasConstraintName("FK_Order_Details_Orders");

			builder.HasOne(d => d.Product)
				.WithMany(p => p.OrderDetails)
				.HasForeignKey(d => d.ProductId)
				.OnDelete(DeleteBehavior.ClientSetNull)
				.HasConstraintName("FK_Order_Details_Products");
		}
	}
}
