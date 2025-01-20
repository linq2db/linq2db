using LinqToDB.EntityFrameworkCore.Tests.Models.Northwind;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinqToDB.EntityFrameworkCore.Tests.Pomelo.Models.Northwind.Mapping
{
	public class CategoriesMap : IEntityTypeConfiguration<Category>
	{
		public void Configure(EntityTypeBuilder<Category> builder)
		{

			builder.HasKey(e => e.CategoryId);

			builder.HasIndex(e => e.CategoryName)
				.HasDatabaseName("CategoryName");

			builder.Property(e => e.CategoryId).HasColumnName("CategoryID")
				.ValueGeneratedNever();

			builder.Property(e => e.CategoryName)
				.IsRequired()
				.HasMaxLength(15);

			builder.Property(e => e.Description).HasColumnType("text");

			builder.Property(e => e.Picture).HasColumnType("blob");
		}
	}
}
