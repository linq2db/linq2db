using LinqToDB.EntityFrameworkCore.Tests.Models.Shared;

using Microsoft.EntityFrameworkCore;

namespace LinqToDB.EntityFrameworkCore.Tests.Models.Shared
{
	public sealed class IdTestContext : DbContext
	{
		public IdTestContext(DbContextOptions options) : base(options) { }

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);
			modelBuilder.Entity<Entity2Item>().HasKey(x => new { x.EntityId, x.ItemId });
			modelBuilder
				.UseSnakeCase()
				.UseIdAsKey()
				.UseOneIdSequence<long>("test", sn => $"nextval('{sn}')");
		}


		public DbSet<Entity> Entities { get; set; } = null!;
		public DbSet<Detail> Details { get; set; } = null!;
		public DbSet<SubDetail> SubDetails { get; set; } = null!;
		public DbSet<Item> Items { get; set; } = null!;
		public DbSet<Child> Children { get; set; } = null!;
	}
}
