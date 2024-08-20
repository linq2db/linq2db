using LinqToDB.EntityFrameworkCore.Tests.Models.Northwind;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinqToDB.EntityFrameworkCore.Tests.PostgreSQL.Models.Northwind.Mapping
{
	public class OrderDetailsMap : BaseEntityMap<OrderDetail>
	{
		public override void Configure(EntityTypeBuilder<OrderDetail> builder)
		{
			base.Configure(builder);

			builder.HasKey(e => new { e.OrderId, e.ProductId });

			 builder.ToTable("Order Details");

			builder.HasIndex(e => e.OrderId)
				.HasDatabaseName("IX_OrderDetail_OrdersOrder_Details");

			builder.HasIndex(e => e.ProductId)
				.HasDatabaseName("IX_OrderDetail_ProductsOrder_Details");

			builder.Property(e => e.OrderId).HasColumnName("OrderID");

			builder.Property(e => e.ProductId).HasColumnName("ProductID");

			builder.Property(e => e.Quantity).HasDefaultValueSql("((1))");

			builder.Property(e => e.UnitPrice).HasColumnType("money");

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
