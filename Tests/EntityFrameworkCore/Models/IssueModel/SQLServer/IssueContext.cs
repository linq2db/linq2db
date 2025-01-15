using LinqToDB.EntityFrameworkCore.Tests.Models.IssueModel;

using Microsoft.EntityFrameworkCore;

namespace LinqToDB.EntityFrameworkCore.Tests.SqlServer.Models.IssueModel
{
	public class IssueContext(DbContextOptions options) : IssueContextBase(options)
	{
		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);

			modelBuilder.Entity<Issue129Table>(e =>
			{
				e
					.Property(e => e.Key)
					.HasComputedColumnSql($"ISNULL({nameof(Issue129Table.Id)}, -1) PERSISTED")
					.ValueGeneratedOnAdd();
			});

			modelBuilder.Entity<Issue4640Table>(e =>
			{
				e.Property(e => e.Items).HasColumnType("nvarchar(max)");
			});
		}
	}
}
