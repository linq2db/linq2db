using LinqToDB.EntityFrameworkCore.Tests.Models.Northwind;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinqToDB.EntityFrameworkCore.Tests.Pomelo.Models.Northwind.Mapping
{
	public class EmployeeTerritoriesMap : IEntityTypeConfiguration<EmployeeTerritory>
	{
		public void Configure(EntityTypeBuilder<EmployeeTerritory> builder)
		{
			builder.HasKey(e => new { e.EmployeeId, e.TerritoryId });

			builder.Property(e => e.EmployeeId).HasColumnName("EmployeeID");

			builder.Property(e => e.TerritoryId)
				.HasColumnName("TerritoryID")
				.HasMaxLength(20);

			builder.HasOne(d => d.Employee)
				.WithMany(p => p.EmployeeTerritories)
				.HasForeignKey(d => d.EmployeeId)
				.OnDelete(DeleteBehavior.ClientSetNull)
				.HasConstraintName("FK_EmployeeTerritories_Employees");

			builder.HasOne(d => d.Territory)
				.WithMany(p => p.EmployeeTerritories)
				.HasForeignKey(d => d.TerritoryId)
				.OnDelete(DeleteBehavior.ClientSetNull)
				.HasConstraintName("FK_EmployeeTerritories_Territories");
		}
	}
}
