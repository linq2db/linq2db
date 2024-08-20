using LinqToDB.EntityFrameworkCore.Tests.Models.Northwind;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinqToDB.EntityFrameworkCore.Tests.PostgreSQL.Models.Northwind.Mapping
{
	public class CategoriesMap : BaseEntityMap<Category>
	{
		public override void Configure(EntityTypeBuilder<Category> builder)
		{
			base.Configure(builder);

			builder.HasKey(e => e.CategoryId);

			builder.HasIndex(e => e.CategoryName)
				.HasDatabaseName("IX_Category_CategoryName");

			builder.Property(e => e.CategoryId).HasColumnName("CategoryID")
				.ValueGeneratedNever();

			builder.Property(e => e.CategoryName)
				.IsRequired()
				.HasMaxLength(15);

			builder.Property(e => e.Description).HasColumnType("text");

			builder.Property(e => e.Picture).HasColumnType("bytea");
		}
	}
}
