using Microsoft.EntityFrameworkCore;

namespace LinqToDB.EntityFrameworkCore.Tests.Models.IssueModel
{
	public class IssueContext : DbContext
	{
		public DbSet<Issue73Entity> Issue73Entities { get; set; } = null!;

		public DbSet<Patent> Patents { get; set; } = null!;

		public DbSet<Parent> Parents { get; set; } = null!;

		public IssueContext(DbContextOptions options) : base(options)
		{
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<Issue73Entity>(b =>
			{
				b.HasKey(x => new { x.Id });

				b.HasOne(x => x.Parent!)
					.WithMany(x => x.Childs)
					.HasForeignKey(x => new { x.ParentId })
					.HasPrincipalKey(x => new { x.Id });

				b.HasData(
				[
					new Issue73Entity
					{
						Id = 2,
						Name = "Name1_2",
					},
					new Issue73Entity
					{
						Id = 3,
						Name = "Name1_3",
						ParentId = 2
					},
				]);
			});

			modelBuilder
				.Entity<Patent>()
				.HasOne(p => p.Assessment!)
				.WithOne(pa => pa.Patent)
				.HasForeignKey<PatentAssessment>(pa => pa.PatentId)
				.OnDelete(DeleteBehavior.Restrict);

			modelBuilder.Entity<Parent>(e =>
			{
				e.Property(e => e.Id).ValueGeneratedNever();
				e.Property(e => e.ParentId);
				e.HasMany(e => e.Children).WithOne(e => e.Parent).HasForeignKey(e => e.ParentId);
			});
			modelBuilder.Entity<Child>(e =>
			{
				e.Property(e => e.Id).ValueGeneratedNever();
				e.Property(e => e.ParentId);
				e.HasMany(e => e.GrandChildren).WithOne(e => e.Child).HasForeignKey(e => e.ChildId);
			});
			modelBuilder.Entity<GrandChild>(e =>
			{
				e.Property(e => e.Id).ValueGeneratedNever();
				e.Property(e => e.ChildId);
			});

		}
	}
}
