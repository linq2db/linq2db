using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

using FluentNHibernate.Cfg;

using LinqToDB.Data;
using LinqToDB.DataProvider.Firebird;
using LinqToDB.DataProvider.Oracle;
using LinqToDB.NHibernate.Tests.Models.Northwind;

using NHibernate;
using NHibernate.Dialect;
using NHibernate.Driver;
using NHibernate.Mapping;
using NHibernate.Tool.hbm2ddl;

using NUnit.Framework;

using Tests;

namespace LinqToDB.NHibernate.Tests
{
	/// <summary>
	/// Base for NHibernate integration tests. Mirrors the <c>LinqToDB.EntityFrameworkCore</c> test harness:
	/// connection strings are resolved from <c>UserDataProviders.json</c> through linq2db's test configuration,
	/// and each test runs once per enabled provider (via <c>[NHIncludeDataSources]</c> on the test parameter).
	/// Every mapped table is given a distinct name prefix (<see cref="TablePrefix"/>) so the NHibernate test
	/// schema never collides with linq2db's own test tables in the shared per-provider database — the schema is
	/// (re)created by <see cref="SchemaExport"/> and seeded once, touching only the prefixed tables.
	/// </summary>
	public abstract class NHTestBase
	{
		// Prefix applied to every mapped table so the NHibernate test schema is isolated from linq2db's own
		// test tables inside the same database. Kept short to stay under Oracle/Firebird identifier limits.
		const string TablePrefix = "l2dbnh_";

		static NHTestBase()
		{
			// Trigger linq2db's test-settings preload so the UserDataProviders.json connection strings are
			// registered with DataConnection before GetConnectionString is used (same trick as the EF Core base).
			_ = TestConfiguration.StoreMetrics;

			// Oracle and Firebird fold unquoted identifiers to UPPER-CASE, but by default linq2db quotes lower-case
			// identifiers (preserving their case) — which would never match the upper-cased tables NHibernate creates
			// from our lower-cased mappings. Tell linq2db to fold lower-case identifiers to upper-case unquoted, the
			// same way NHibernate's DDL does, so both sides agree. (PostgreSQL keeps lower-case and needs no change.)
			OracleOptions.Default   = OracleOptions.Default   with { DontEscapeLowercaseIdentifiers = true };
			FirebirdOptions.Default = FirebirdOptions.Default with { IdentifierQuoteMode = FirebirdIdentifierQuoteMode.None };
		}

		/// <summary>
		/// After each test, dump the SQL captured by the LinqToDB.NHibernate logger to that test's baseline (a no-op
		/// when no baselines path is configured or the test failed) and release the per-test context.
		/// </summary>
		[TearDown]
		public virtual void OnAfterTest()
		{
			BaselinesManager.Dump(false, ".NH");
			CustomTestContext.Release();
		}

		static readonly ConcurrentDictionary<string, Lazy<ISessionFactory>> _factories = new();

		/// <summary>
		/// Returns a cached <see cref="ISessionFactory"/> for <paramref name="provider"/>, building its schema
		/// and seed data on first use.
		/// </summary>
		protected static ISessionFactory GetSessionFactory(string provider)
		{
			// Lazy guarantees BuildFactory (schema drop+create + seed) runs exactly once per provider even when
			// several fixtures request the same provider concurrently: ConcurrentDictionary.GetOrAdd alone may run
			// its value factory more than once under contention, which would race two SchemaExports.
			return _factories.GetOrAdd(provider, p => new Lazy<ISessionFactory>(() => BuildFactory(p))).Value;
		}

		/// <summary>
		/// Disposes every cached <see cref="ISessionFactory"/>. Invoked once from the assembly-level
		/// <c>NHTestAssemblyTeardown</c> so the shared cache is never disposed while another fixture is using it.
		/// </summary>
#pragma warning disable NUnit1028 // Internal helper invoked by the assembly-level fixture, not a test method.
		internal static void DisposeFactories()
		{
			foreach (var sf in _factories.Values)
				if (sf.IsValueCreated)
					sf.Value.Dispose();

			_factories.Clear();
		}
#pragma warning restore NUnit1028 // Internal helper invoked by the assembly-level fixture, not a test method.

		static ISessionFactory BuildFactory(string provider)
		{
			var cfg = BuildConfiguration(provider);

			var sf = cfg.BuildSessionFactory();

			// Recreate the (prefixed) schema on every run — drop + create — mirroring EF Core EnsureDeleted/EnsureCreated.
			new SchemaExport(cfg).Create(false, true);

			Seed(sf);

			return sf;
		}

		static global::NHibernate.Cfg.Configuration BuildConfiguration(string provider)
		{
			Type dialect;
			Type driver;

			if      (provider.IsAnyOf(TestProvName.AllSqlServer))     { dialect = typeof(MsSql2012Dialect);    driver = typeof(MicrosoftDataSqlClientDriver); }
			else if (provider.IsAnyOf(TestProvName.AllSQLite))        { dialect = typeof(SQLiteDialect);       driver = typeof(SQLite20Driver); }
			else if (provider.IsAnyOf(TestProvName.AllPostgreSQL))    { dialect = typeof(PostgreSQL83Dialect); driver = typeof(NpgsqlDriver); }
			else if (provider.IsAnyOf(TestProvName.AllMySql))         { dialect = typeof(MySQL57Dialect);      driver = typeof(MySqlDataDriver); }
			else if (provider.IsAnyOf(TestProvName.AllOracleManaged)) { dialect = typeof(Oracle12cDialect);    driver = typeof(OracleManagedDataClientDriver); }
			else if (provider.IsAnyOf(TestProvName.AllFirebird))      { dialect = typeof(FirebirdDialect);     driver = typeof(FirebirdClientDriver); }
			else throw new InvalidOperationException($"NHibernate tests are not configured for provider '{provider}'.");

			// SQLite has no shared server database: use an isolated throwaway file (recreated per run). Every
			// server provider reuses linq2db's per-provider test database, kept collision-free by the table prefix.
			string connectionString;
			if (provider.IsAnyOf(TestProvName.AllSQLite))
			{
				var dbFile = Path.Combine(Path.GetTempPath(), $"nh_l2db.{provider}.db");
				if (File.Exists(dbFile))
					File.Delete(dbFile);
				connectionString = $"Data Source={dbFile}";
			}
			else
			{
				connectionString = DataConnection.GetConnectionString(provider);
			}

			var cfg = new global::NHibernate.Cfg.Configuration();

			// Fold every mapped identifier to lower-case so linq2db's default (Auto) identifier quoting never
			// quotes them: unquoted lower-case names then case-fold identically on both sides (PostgreSQL keeps
			// lower-case, Oracle/Firebird go upper-case), so linq2db's queries hit the tables NHibernate created.
			cfg.SetNamingStrategy(LowerCaseNamingStrategy.Instance);

			cfg.SetProperty("dialect",                      dialect.AssemblyQualifiedName);
			cfg.SetProperty("connection.driver_class",      driver.AssemblyQualifiedName);
			cfg.SetProperty("connection.connection_string", connectionString);
			// Neither linq2db nor NHibernate quotes identifiers here, so reserved-word columns (e.g. "Number")
			// fold the same way on both sides and stay resolvable through the linq2db attach path.
			cfg.SetProperty("hbm2ddl.keywords",             "none");

			return Fluently.Configure(cfg)
				.Mappings(m => m.FluentMappings.AddFromAssembly(typeof(NHTestBase).Assembly))
				.ExposeConfiguration(c => PostConfigure(c, provider))
				.BuildConfiguration();
		}

		// Prefix every mapped table (entity + collection) so the NHibernate test schema never collides with
		// linq2db's own tables. A one-to-many collection table shares the child entity's Table instance, so the
		// distinct-set guard prevents a double prefix; the quoted flag is preserved for names like "Order Details".
		static void PostConfigure(global::NHibernate.Cfg.Configuration cfg, string provider)
		{
			var seen = new HashSet<Table>();

			// Firebird rejects a composite index whose key exceeds its size limit — two default VARCHAR(255)
			// columns under a multi-byte charset already do. The test data is short, so cap string lengths there.
			var maxStringLength = provider.IsAnyOf(TestProvName.AllFirebird) ? 100 : int.MaxValue;

			foreach (var pc in cfg.ClassMappings)
				PrefixTable(pc.Table, seen, maxStringLength);

			foreach (var col in cfg.CollectionMappings)
				PrefixTable(col.CollectionTable, seen, maxStringLength);
		}

		static void PrefixTable(Table? table, HashSet<Table> seen, int maxStringLength)
		{
			if (table == null || !seen.Add(table))
				return;

			// The Table.Name setter treats a leading backtick as "quoted"; re-wrap so quoted names stay quoted.
			table.Name = table.IsQuoted ? $"`{TablePrefix}{table.Name}`" : $"{TablePrefix}{table.Name}";

			if (maxStringLength != int.MaxValue)
				foreach (var column in table.ColumnIterator)
					if (column.Length > maxStringLength)
						column.Length = maxStringLength;
		}

		// Lower-cases every mapped table and column name at binding time (so foreign keys pick it up too). This
		// keeps NHibernate's DDL and linq2db's queries in agreement on the case-folding providers — see the note
		// on the SetNamingStrategy call in BuildConfiguration.
		sealed class LowerCaseNamingStrategy : global::NHibernate.Cfg.INamingStrategy
		{
			public static readonly LowerCaseNamingStrategy Instance = new();

			public string ClassToTableName(string className)                         => className.ToLowerInvariant();
			public string PropertyToColumnName(string propertyName)                  => propertyName.ToLowerInvariant();
			public string TableName(string tableName)                                => tableName.ToLowerInvariant();
			public string ColumnName(string columnName)                              => columnName.ToLowerInvariant();
			public string PropertyToTableName(string className, string propertyName) => propertyName.ToLowerInvariant();
			public string LogicalColumnName(string columnName, string propertyName)  => string.IsNullOrEmpty(columnName) ? propertyName.ToLowerInvariant() : columnName.ToLowerInvariant();
		}

		static void Seed(ISessionFactory sf)
		{
			using var session = sf.OpenSession();
			using var tx      = session.BeginTransaction();

			session.Save(new Customer { CustomerId = "ALFKI", CompanyName = "Alfreds Futterkiste" });

			tx.Commit();
		}
	}
}
