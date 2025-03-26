﻿using LinqToDB.EntityFrameworkCore.Tests.Models.IssueModel;

using Microsoft.EntityFrameworkCore;

namespace LinqToDB.EntityFrameworkCore.Tests.PostgreSQL.Models.IssueModel
{
	public class IssueContext(DbContextOptions options) : IssueContextBase(options)
	{
		public DbSet<PostgreTable> PostgreTestTable { get; set; } = null!;
		public DbSet<Issue4641Table> Issue4641Table { get; set; } = null!;
		public DbSet<Issue4643Table> Issue4643Table { get; set; } = null!;
		public DbSet<Issue4667Table> Issue4667 { get; set; } = null!;

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

			modelBuilder.Entity<Issue4641Table>(e =>
			{
				e.Property(e => e.Id).UseSerialColumn();
			});

			modelBuilder.Entity<Issue4643Table>();

			modelBuilder.Entity<Issue4667Table>(e =>
			{
				e.HasKey(x => x.Id);

				e.Property(x => x.Id)
					.HasColumnName("id")
					.ValueGeneratedNever();

				e.Property(x => x.Payload)
					.HasColumnName("payload")
					.HasColumnType("jsonb");

				e.Property(x => x.Headers)
					.HasColumnType("json")
					.HasColumnName("headers");
			});
		}
	}
}
