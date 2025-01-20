using LinqToDB.EntityFrameworkCore.Tests.Models.Northwind;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinqToDB.EntityFrameworkCore.Tests.Pomelo.Models.Northwind.Mapping
{
	public class OrderMap : IEntityTypeConfiguration<Order>
	{
		public void Configure(EntityTypeBuilder<Order> builder)
		{
			
				builder.HasKey(e => e.OrderId);

				builder.HasIndex(e => e.CustomerId)
					.HasDatabaseName("CustomersOrders");

				builder.HasIndex(e => e.EmployeeId)
					.HasDatabaseName("EmployeesOrders");

				builder.HasIndex(e => e.OrderDate)
					.HasDatabaseName("OrderDate");

				builder.HasIndex(e => e.ShipPostalCode)
					.HasDatabaseName("ShipPostalCode");

				builder.HasIndex(e => e.ShipVia)
					.HasDatabaseName("ShippersOrders");

				builder.HasIndex(e => e.ShippedDate)
					.HasDatabaseName("ShippedDate");

				builder.Property(e => e.OrderId).HasColumnName("OrderID")
					.ValueGeneratedNever();

				builder.Property(e => e.CustomerId)
					.HasColumnName("CustomerID")
					.HasMaxLength(5);

				builder.Property(e => e.EmployeeId).HasColumnName("EmployeeID");

				builder.Property(e => e.Freight)
					.HasColumnType("decimal(13, 4)")
					.HasDefaultValue(0m);

				builder.Property(e => e.OrderDate).HasColumnType("datetime");

				builder.Property(e => e.RequiredDate).HasColumnType("datetime");

				builder.Property(e => e.ShipAddress).HasMaxLength(60);

				builder.Property(e => e.ShipCity).HasMaxLength(15);

				builder.Property(e => e.ShipCountry).HasMaxLength(15);

				builder.Property(e => e.ShipName).HasMaxLength(40);

				builder.Property(e => e.ShipPostalCode).HasMaxLength(10);

				builder.Property(e => e.ShipRegion).HasMaxLength(15);

				builder.Property(e => e.ShippedDate).HasColumnType("datetime");

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
