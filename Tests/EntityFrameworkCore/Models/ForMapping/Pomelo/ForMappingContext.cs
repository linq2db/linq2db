using LinqToDB.EntityFrameworkCore.Tests.Models.ForMapping;

using Microsoft.EntityFrameworkCore;

namespace LinqToDB.EntityFrameworkCore.Tests.Pomelo.Models.ForMapping
{
	public class ForMappingContext : ForMappingContextBase
	{
		public ForMappingContext(DbContextOptions options) : base(options)
		{
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);

			modelBuilder.Entity<WithIdentity>(b =>
			{
				b.HasKey(e => e.Id);
#if !NET10_0
				b.Property(e => e.Id)
					.UseMySqlIdentityColumn();
#endif
			});

			modelBuilder.Entity<NoIdentity>(b =>
			{
				b.HasKey(e => e.Id);
			});
			
			modelBuilder.Entity<WithInheritance>(b =>
			{
				b.HasDiscriminator(x => x.Discriminator);
			});
		}
	}
}
