using System;
using System.Collections.Concurrent;
using System.IO;

using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;

using LinqToDB.Data;
using LinqToDB.NHibernate.Tests.Models.Northwind;

using NHibernate;
using NHibernate.Driver;
using NHibernate.Tool.hbm2ddl;

using Tests;

namespace LinqToDB.NHibernate.Tests
{
	/// <summary>
	/// Base for NHibernate integration tests. Mirrors the <c>LinqToDB.EntityFrameworkCore</c> test harness:
	/// connection strings are resolved from <c>UserDataProviders.json</c> through linq2db's test configuration,
	/// each test runs once per enabled provider (via <c>[IncludeDataSources]</c> on the test parameter), and
	/// every provider gets an isolated database — a <c>.NH</c>-suffixed SQL Server catalog or a separate SQLite
	/// file — whose schema is (re)created by NHibernate <see cref="SchemaExport"/> and seeded once. The shared
	/// linq2db test database is never touched.
	/// </summary>
	public abstract class NHTestBase
	{
		const string DbSuffix = "NH";

		static NHTestBase()
		{
			// Trigger linq2db's test-settings preload so the UserDataProviders.json connection strings are
			// registered with DataConnection before GetConnectionString is used (same trick as the EF Core base).
			_ = TestConfiguration.StoreMetrics;
		}

		static readonly ConcurrentDictionary<string, ISessionFactory> _factories = new();

		/// <summary>
		/// Returns a cached <see cref="ISessionFactory"/> for <paramref name="provider"/>, building its isolated
		/// database, schema and seed data on first use.
		/// </summary>
		protected static ISessionFactory GetSessionFactory(string provider)
		{
			return _factories.GetOrAdd(provider, BuildFactory);
		}

		/// <summary>
		/// Disposes every cached <see cref="ISessionFactory"/>. Invoked once from the assembly-level
		/// <c>NHTestAssemblyTeardown</c> so the shared cache is never disposed while another fixture is using it.
		/// </summary>
		internal static void DisposeFactories()
		{
			foreach (var sf in _factories.Values)
				sf.Dispose();

			_factories.Clear();
		}

		static ISessionFactory BuildFactory(string provider)
		{
			var cfg = BuildConfiguration(provider);

			var sf = cfg.BuildSessionFactory();

			// Recreate the schema on every run (drop + create), mirroring EF Core EnsureDeleted/EnsureCreated.
			new SchemaExport(cfg).Create(false, true);

			Seed(sf);

			return sf;
		}

		static global::NHibernate.Cfg.Configuration BuildConfiguration(string provider)
		{
			FluentConfiguration fluent;

			if (provider.IsAnyOf(TestProvName.AllSqlServer))
			{
				// Microsoft.Data.SqlClient is referenced by the test project and connects to any SQL Server
				// version; the attach path detects the linq2db provider from the live connection regardless.
				var config = MsSqlConfiguration.MsSql2012
					.ConnectionString(GetIsolatedSqlServerConnectionString(provider))
					.Driver<MicrosoftDataSqlClientDriver>();

				fluent = Fluently.Configure().Database(config);
			}
			else if (provider.IsAnyOf(TestProvName.AllSQLite))
			{
				// NHibernate's SQLite20Driver uses System.Data.SQLite (linq2db SQLiteClassic). A fresh file per
				// provider gives an isolated, throwaway database.
				var dbFile = Path.Combine(Path.GetTempPath(), $"nh_l2db.{provider}.{DbSuffix}.db");

				if (File.Exists(dbFile))
					File.Delete(dbFile);

				fluent = Fluently.Configure().Database(SQLiteConfiguration.Standard.UsingFile(dbFile));
			}
			else
			{
				throw new InvalidOperationException($"NHibernate tests are not configured for provider '{provider}'.");
			}

			return fluent
				.Mappings(m => m.FluentMappings.AddFromAssembly(typeof(NHTestBase).Assembly))
				.BuildConfiguration();
		}

		static string GetIsolatedSqlServerConnectionString(string provider)
		{
			var baseConnectionString = DataConnection.GetConnectionString(provider);

			// Mirror EF Core: run against a separate database so SchemaExport never touches the shared Northwind.
			var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(baseConnectionString);
			builder.InitialCatalog += $".{DbSuffix}";

			EnsureSqlServerDatabase(baseConnectionString, builder.InitialCatalog);

			return builder.ConnectionString;
		}

		static void EnsureSqlServerDatabase(string baseConnectionString, string database)
		{
			var master = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(baseConnectionString)
			{
				InitialCatalog = "master"
			};

			using var cn = new Microsoft.Data.SqlClient.SqlConnection(master.ConnectionString);
			cn.Open();

			using var cmd = cn.CreateCommand();
			// QUOTENAME bracket-quotes the (dotted) database name; the statement is built into a variable
			// because EXEC() only concatenates string literals/variables, not function calls.
			cmd.CommandText =
				"IF DB_ID(@name) IS NULL " +
				"BEGIN " +
					"DECLARE @sql nvarchar(300) = N'CREATE DATABASE ' + QUOTENAME(@name); " +
					"EXEC(@sql); " +
				"END";

			var p = cmd.CreateParameter();
			p.ParameterName = "@name";
			p.Value         = database;
			cmd.Parameters.Add(p);

			cmd.ExecuteNonQuery();
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
