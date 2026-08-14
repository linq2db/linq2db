using System;
using System.Linq;
using System.Runtime.CompilerServices;

using LinqToDB.Internal.Common;
using LinqToDB.Mapping;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

using Microsoft.Extensions.DependencyInjection;

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

		const string SqliteNameColumn    = "name_sqlite";
		const string SqlServerNameColumn = "name_mssql";

		public class ProviderSplitItem
		{
			public int     Id   { get; set; }
			public string? Name { get; set; }
		}

		/// <summary>
		/// One context type mapping a property differently per provider — the shape reported in
		/// <a href="https://github.com/linq2db/linq2db/issues/5778">#5778</a>. EF's default model
		/// cache key is the context type alone, so it cannot tell the two models apart.
		/// </summary>
		public class ProviderSplitContext(DbContextOptions options) : DbContext(options)
		{
			public DbSet<ProviderSplitItem> Items { get; set; } = null!;

			protected override void OnModelCreating(ModelBuilder modelBuilder)
			{
				modelBuilder.Entity<ProviderSplitItem>()
					.Property(e => e.Name)
					.HasColumnName(Database.IsSqlite() ? SqliteNameColumn : SqlServerNameColumn);
			}
		}

		/// <summary>
		/// The schema cache must not share a schema between contexts of the same type running on
		/// different providers: the model, and therefore the schema, differs per provider. Neither
		/// context connects — only the model and the mapping schema are built.
		/// </summary>
		[Test]
		public void MappingSchemaNotSharedBetweenProviders()
		{
			using var sqlite = new ProviderSplitContext(
				new DbContextOptionsBuilder<ProviderSplitContext>()
					.UseSqlite("Data Source=:memory:")
					.Options);

			using var sqlServer = new ProviderSplitContext(
				new DbContextOptionsBuilder<ProviderSplitContext>()
					.UseSqlServer("Server=.;Database=MappingSchemaCacheTests;Integrated Security=SSPI;TrustServerCertificate=true")
					.Options);

			var sqliteSchema    = LinqToDBForEFTools.GetMappingSchema(sqlite   .Model, sqlite   , null);
			var sqlServerSchema = LinqToDBForEFTools.GetMappingSchema(sqlServer.Model, sqlServer, null);

			GetNameColumn(sqliteSchema   ).ShouldBe(SqliteNameColumn);
			GetNameColumn(sqlServerSchema).ShouldBe(SqlServerNameColumn);
		}

		const string CustomizedNameColumn = "name_customized";

		sealed class RenamingModelCustomizer(ModelCustomizerDependencies dependencies) : ModelCustomizer(dependencies)
		{
			public override void Customize(ModelBuilder modelBuilder, DbContext context)
			{
				base.Customize(modelBuilder, context);

				modelBuilder.Entity<ProviderSplitItem>()
					.Property(e => e.Name)
					.HasColumnName(CustomizedNameColumn);
			}
		}

		/// <summary>
		/// Same context type, same provider, but a replaced service that changes the model. EF treats
		/// a replaced service as requiring its own internal service provider (and therefore its own
		/// model), so the schema cache must keep the two apart as well — which the provider name
		/// alone could not express.
		/// </summary>
		[Test]
		public void MappingSchemaNotSharedBetweenReplacedServices()
		{
			using var plain = new ProviderSplitContext(
				new DbContextOptionsBuilder<ProviderSplitContext>()
					.UseSqlite("Data Source=:memory:")
					.Options);

			using var customized = new ProviderSplitContext(
				new DbContextOptionsBuilder<ProviderSplitContext>()
					.UseSqlite("Data Source=:memory:")
					.ReplaceService<IModelCustomizer, RenamingModelCustomizer>()
					.Options);

			GetNameColumn(LinqToDBForEFTools.GetMappingSchema(plain     .Model, plain     , null)).ShouldBe(SqliteNameColumn);
			GetNameColumn(LinqToDBForEFTools.GetMappingSchema(customized.Model, customized, null)).ShouldBe(CustomizedNameColumn);
		}

		static string? GetNameColumn(MappingSchema schema)
			=> schema.GetEntityDescriptor(typeof(ProviderSplitItem))
				.Columns
				.Single(c => c.MemberName == nameof(ProviderSplitItem.Name))
				.ColumnName;

		public class RetentionContext(DbContextOptions options) : DbContext(options)
		{
			public DbSet<Item> Items { get; set; } = null!;
		}

		/// <summary>
		/// The process-wide caches must not outlive the application service provider of the context
		/// that populated them. With <c>EnableServiceProviderCaching(false)</c> EF builds a fresh
		/// <c>IModel</c> per context, so a strongly-keyed cache entry per model pins one application
		/// scope per <see cref="DbContext"/> instance. EF avoids this in its own long-lived key:
		/// <c>ServiceProviderCache.GetOrAdd</c> replaces the options' core extension with
		/// <c>WithApplicationServiceProvider(null)</c> before storing it.
		/// </summary>
		[Test]
		public void CachesDoNotRetainApplicationServiceProvider()
		{
			var applicationServices = BuildSchemaAndDiscardContext();

			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();

			applicationServices.IsAlive.ShouldBeFalse();

			[MethodImpl(MethodImplOptions.NoInlining)]
			static WeakReference BuildSchemaAndDiscardContext()
			{
				var services = new ServiceCollection().BuildServiceProvider();

				using (var ctx = new RetentionContext(
					new DbContextOptionsBuilder<RetentionContext>()
						.EnableServiceProviderCaching(false)
						.UseApplicationServiceProvider(services)
						.UseSqlite("Data Source=:memory:")
						.Options))
				{
					LinqToDBForEFTools.GetMappingSchema(ctx.Model, ctx, null);
				}

				return new WeakReference(services);
			}
		}

		/// <summary>
		/// The metadata-reader cache is keyed on the <c>IModel</c> instance, so it must not keep that
		/// model alive. A configuration that defeats EF's model caching — <c>EnableServiceProviderCaching(false)</c>,
		/// or a freshly built model passed to <c>UseModel</c> per context — would otherwise add one
		/// never-evicted entry per <see cref="DbContext"/> instance.
		/// </summary>
		[Test]
		public void MetadataReaderCacheDoesNotRetainModel()
		{
			var model = BuildReaderAndDiscardContext();

			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();

			model.IsAlive.ShouldBeFalse();

			[MethodImpl(MethodImplOptions.NoInlining)]
			static WeakReference BuildReaderAndDiscardContext()
			{
				using var ctx = new RetentionContext(
					new DbContextOptionsBuilder<RetentionContext>()
						.EnableServiceProviderCaching(false)
						.UseSqlite("Data Source=:memory:")
						.Options);

				LinqToDBForEFTools.GetMetadataReader(ctx.Model, ctx);

				return new WeakReference(ctx.Model);
			}
		}
	}
}
