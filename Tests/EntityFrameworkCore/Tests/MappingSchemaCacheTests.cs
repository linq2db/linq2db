using LinqToDB.Internal.Common;

using Microsoft.EntityFrameworkCore;

using NUnit.Framework;

using Shouldly;

namespace LinqToDB.EntityFrameworkCore.Tests
{
	/// <summary>
	/// The linq2db mapping schema built from an EF Core model must have a stable identity
	/// across <see cref="DbContext"/> instances of the same model. linq2db keys its query
	/// cache on the mapping schema's <see cref="IConfigurationID.ConfigurationID"/>; if two
	/// contexts of the same model produce different schema identities, every query recompiles
	/// (cache miss) per context.
	/// </summary>
	[TestFixture]
	public class MappingSchemaCacheTests
	{
		public class Item
		{
			public int     Id   { get; set; }
			public string? Name { get; set; }
		}

		public class CacheTestContext(DbContextOptions options) : DbContext(options)
		{
			public DbSet<Item> Items { get; set; } = null!;
		}

		static CacheTestContext CreateContext()
		{
			var options = new DbContextOptionsBuilder<CacheTestContext>()
				// Force a fresh EF internal service provider (and therefore a fresh IModel)
				// per context — the scenario where linq2db's model-instance-keyed schema
				// cache misses. With provider caching on, EF shares one model and the bug hides.
				.EnableServiceProviderCaching(false)
				.UseSqlite("Data Source=:memory:")
				.Options;

			return new CacheTestContext(options);
		}

		[Test]
		public void MappingSchemaIdentityStableAcrossContexts()
		{
			int id1, id2;

			using (var ctx = CreateContext())
			using (var db  = ctx.CreateLinqToDBContext())
				id1 = ((IConfigurationID)db.MappingSchema).ConfigurationID;

			using (var ctx = CreateContext())
			using (var db  = ctx.CreateLinqToDBContext())
				id2 = ((IConfigurationID)db.MappingSchema).ConfigurationID;

			id2.ShouldBe(id1);
		}
	}
}
