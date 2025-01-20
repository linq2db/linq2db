using Microsoft.EntityFrameworkCore;

namespace LinqToDB.EntityFrameworkCore.Tests.Models.NpgSqlEntities
{
	public class NpgSqlEntitiesContext : DbContext
	{
		public NpgSqlEntitiesContext(DbContextOptions options)
			: base(options)
		{
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<Event>(entity =>
				entity.Property(e => e.Duration).HasColumnType("tsrange")
			);

			modelBuilder.Entity<EventView>(entity =>
				{
					entity.HasNoKey();
					entity.ToView("EventsView", "views");
				});

			modelBuilder.Entity<EntityWithArrays>(entity =>
			{
			});

			modelBuilder.Entity<EntityWithXmin>(entity =>
			{
#if NET8_0_OR_GREATER
				entity.Property<uint>(nameof(NpgSqlEntities.EntityWithXmin.xmin)).IsRowVersion();
#else
				entity.UseXminAsConcurrencyToken();
#endif
			});

			modelBuilder.Entity<TimeStampEntity>(e =>
			{
				e.Property(e => e.Timestamp1).HasColumnType("timestamp");
				e.Property(e => e.Timestamp2).HasColumnType("timestamp");
				e.Property(e => e.TimestampTZ1).HasColumnType("timestamp with time zone");
				e.Property(e => e.TimestampTZ2).HasColumnType("timestamp with time zone");
				e.Property(e => e.TimestampTZ3).HasColumnType("timestamp with time zone");
			});
		}

		public virtual DbSet<Event> Events { get; set; } = null!;
		public virtual DbSet<EntityWithArrays> EntityWithArrays { get; set; } = null!;
		public virtual DbSet<EntityWithXmin> EntityWithXmin { get; set; } = null!;
		public virtual DbSet<TimeStampEntity> TimeStamps { get; set; } = null!;
	}
}
