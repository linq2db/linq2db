using LinqToDB.EntityFrameworkCore.Tests.Models.ForMapping;

using Microsoft.EntityFrameworkCore;

namespace LinqToDB.EntityFrameworkCore.Tests.SqlServer.Models.ForMapping
{
	public class ForMappingContext : ForMappingContextBase
	{
		public ForMappingContext(DbContextOptions options) : base(options)
		{
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<WithIdentity>(b =>
			{
				b.HasKey(e => e.Id);

				b.Property(e => e.Id)
					.UseIdentityColumn();
			});

			modelBuilder.Entity<NoIdentity>(b =>
			{
				b.HasKey(e => e.Id);
			});

			modelBuilder.Entity<StringTypes>(b =>
				{
					b.Property(e => e.AsciiString).HasMaxLength(50).IsUnicode(false);
					b.Property(e => e.UnicodeString).HasMaxLength(50).IsUnicode();
				}
			);

			modelBuilder.Entity<TypesTable>(b =>
			{
				b.Property(e => e.DateTime);
				b.Property(e => e.String).HasMaxLength(100);
			});

			modelBuilder.Entity<WithInheritance>(b =>
			{
				b.HasDiscriminator(x => x.Discriminator);
			});
		}
	}
}
