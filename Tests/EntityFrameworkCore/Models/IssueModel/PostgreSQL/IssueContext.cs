using LinqToDB.EntityFrameworkCore.Tests.Models.IssueModel;

using Microsoft.EntityFrameworkCore;

namespace LinqToDB.EntityFrameworkCore.Tests.PostgreSQL.Models.IssueModel
{
	public class IssueContext(DbContextOptions options) : IssueContextBase(options)
	{
		public DbSet<PostgreTable> PostgreTestTable { get; set; } = null!;

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);

			modelBuilder.Entity<PostgreTable>(e =>
			{
				e.Property(e => e.Id).ValueGeneratedNever();
				e.Property(e => e.Title);
				e.Property(e => e.SearchVector);
			});
		}
	}
}
