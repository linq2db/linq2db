using LinqToDB.EntityFrameworkCore.Tests.Models.Northwind;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinqToDB.EntityFrameworkCore.Tests.PostgreSQL.Models.Northwind.Mapping
{
	public class OrderMap : BaseEntityMap<Order>
	{
		public override void Configure(EntityTypeBuilder<Order> builder)
		{
			
			base.Configure(builder);

				builder.HasKey(e => e.OrderId);

				builder.HasIndex(e => e.CustomerId)
					.HasDatabaseName("IX_Order_CustomersOrders");

				builder.HasIndex(e => e.EmployeeId)
					.HasDatabaseName("IX_Order_EmployeesOrders");

				builder.HasIndex(e => e.OrderDate)
					.HasDatabaseName("IX_Order_OrderDate");

				builder.HasIndex(e => e.ShipPostalCode)
					.HasDatabaseName("IX_Order_ShipPostalCode");

				builder.HasIndex(e => e.ShipVia)
					.HasDatabaseName("IX_Order_ShippersOrders");

				builder.HasIndex(e => e.ShippedDate)
					.HasDatabaseName("IX_Order_ShippedDate");

				builder.Property(e => e.OrderId).HasColumnName("OrderID")
					.ValueGeneratedNever();

				builder.Property(e => e.CustomerId)
					.HasColumnName("CustomerID")
					.HasMaxLength(5);

				builder.Property(e => e.EmployeeId).HasColumnName("EmployeeID");

				builder.Property(e => e.Freight)
					.HasColumnType("money")
					.HasDefaultValueSql("((0))");

				builder.Property(e => e.OrderDate).HasColumnType("timestamp");

				builder.Property(e => e.RequiredDate).HasColumnType("timestamp");

				builder.Property(e => e.ShipAddress).HasMaxLength(60);

				builder.Property(e => e.ShipCity).HasMaxLength(15);

				builder.Property(e => e.ShipCountry).HasMaxLength(15);

				builder.Property(e => e.ShipName).HasMaxLength(40);

				builder.Property(e => e.ShipPostalCode).HasMaxLength(10);

				builder.Property(e => e.ShipRegion).HasMaxLength(15);

				builder.Property(e => e.ShippedDate).HasColumnType("timestamp");

				builder.HasOne(d => d.Customer!)
					.WithMany(p => p.Orders)
					.HasForeignKey(d => d.CustomerId)
					.HasConstraintName("FK_Orders_Customers");

				builder.HasOne(d => d.Employee!)
					.WithMany(p => p.Orders)
					.HasForeignKey(d => d.EmployeeId)
					.HasConstraintName("FK_Orders_Employees");

				builder.HasOne(d => d.ShipViaNavigation!)
					.WithMany(p => p.Orders)
					.HasForeignKey(d => d.ShipVia)
					.HasConstraintName("FK_Orders_Shippers");
			}
	}
}
