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

			modelBuilder.Entity<Issue155Table>(e =>
			{
				e.HasData(new Issue155Table() { Id = 1, Linked = [2] }, new Issue155Table() { Id = 2, Linked = [1, 3] }, new Issue155Table() { Id = 3, Linked = [1] });
			});

			modelBuilder.Entity<PostgreTable>(e =>
			{
				e.Property(e => e.Id).ValueGeneratedNever();
				e.Property(e => e.Title);
				e.Property(e => e.SearchVector);
			});

			modelBuilder.Entity<Issue4640Table>(e =>
			{
				e.Property(e => e.Items).HasColumnType("text");
			});
		}
	}
}
