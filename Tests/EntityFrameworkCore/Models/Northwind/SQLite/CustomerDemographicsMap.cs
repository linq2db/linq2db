using LinqToDB.EntityFrameworkCore.Tests.Models.Northwind;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinqToDB.EntityFrameworkCore.Tests.SQLite.Models.Northwind.Mapping
{
	public class CustomerDemographicsMap : IEntityTypeConfiguration<CustomerDemographics>
	{
		public void Configure(EntityTypeBuilder<CustomerDemographics> builder)
		{
			builder.HasKey(e => e.CustomerTypeId);

			builder.Property(e => e.CustomerTypeId)
				.HasColumnName("CustomerTypeID")
				.HasMaxLength(10)
				.ValueGeneratedNever();

			builder.Property(e => e.CustomerDesc).HasColumnType("text");
		}
	}
}
