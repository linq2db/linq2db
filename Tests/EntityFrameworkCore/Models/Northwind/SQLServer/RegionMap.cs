using LinqToDB.EntityFrameworkCore.Tests.Models.Northwind;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinqToDB.EntityFrameworkCore.Tests.SqlServer.Models.Northwind.Mapping
{
	public class RegionMap : BaseEntityMap<Region>
	{
		public override void Configure(EntityTypeBuilder<Region> builder)
		{
			base.Configure(builder);

			builder.HasKey(e => e.RegionId)
					.IsClustered(false);

			builder.Property(e => e.RegionId)
				.HasColumnName("RegionID")
				.ValueGeneratedNever();

			builder.Property(e => e.RegionDescription)
				.IsRequired()
				.HasMaxLength(50);
		}
	}
}
