﻿using LinqToDB.EntityFrameworkCore.Tests.Models.Northwind;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinqToDB.EntityFrameworkCore.Tests.SQLite.Models.Northwind.Mapping
{
	public class CustomersMap : IEntityTypeConfiguration<Customer>
	{
		public void Configure(EntityTypeBuilder<Customer> builder)
		{
			builder.HasKey(e => e.CustomerId);

			builder.HasIndex(e => e.City)
					.HasDatabaseName("City");

			builder.HasIndex(e => e.CompanyName)
					.HasDatabaseName("Customer_CompanyName");

			builder.HasIndex(e => e.PostalCode)
					.HasDatabaseName("Customer_Postal_ode");

			builder.HasIndex(e => e.Region)
					.HasDatabaseName("Customer_Region");

			builder.Property(e => e.CustomerId)
					.HasColumnName("CustomerID")
					.HasMaxLength(5)
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
			builder.Property(e => e.Phone).HasMaxLength(24);
			builder.Property(e => e.PostalCode).HasMaxLength(10);
			builder.Property(e => e.Region).HasMaxLength(15);
		}
	}
}
