using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

using Tests;

#if NETFRAMEWORK
using MySqlConnectionStringBuilder = MySql.Data.MySqlClient.MySqlConnectionStringBuilder;
#elif !NET9_0
using MySqlConnectionStringBuilder = MySqlConnector.MySqlConnectionStringBuilder;
#endif

namespace LinqToDB.EntityFrameworkCore.Tests
{
	internal static class TestContextTracker
	{
		// cannot add it to ContextTestBase as it will have separate instance per-TContext
		public static readonly Dictionary<string, Type> LastContexts = new ();
	}

	public abstract class ContextTestBase<TContext> : TestBase
		where TContext: DbContext
	{
		protected virtual DbContextOptionsBuilder<TContext> ProviderSetup(string provider, string connectionString, DbContextOptionsBuilder<TContext> optionsBuilder)
		{
			return provider switch
			{
				// UseNodaTime called due to bug in Npgsql v8, where UseNodaTime ignored, when UseNpgsql already called without it
				_ when provider.IsAnyOf(TestProvName.AllPostgreSQL)
					=> optionsBuilder
					.UseNpgsql(connectionString, o => o.UseNodaTime())
					.UseLinqToDB(builder => builder.AddCustomOptions(o => o.UseMappingSchema(NodaTimeSupport))),
#if !NET9_0
				_ when provider.IsAnyOf(TestProvName.AllMySql) => optionsBuilder
#if !NETFRAMEWORK
					.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)),
#else
					.UseMySql(connectionString),
#endif
#endif
				_ when provider.IsAnyOf(TestProvName.AllSQLite) => optionsBuilder.UseSqlite(connectionString),
				_ when provider.IsAnyOf(TestProvName.AllSqlServer) => optionsBuilder.UseSqlServer(connectionString),
				_ => throw new InvalidOperationException($"{nameof(ProviderSetup)} is not implemented for provider {provider}")
			};
		}

		protected abstract TContext CreateProviderContext(string provider, DbContextOptions<TContext> options);

		protected virtual void OnDatabaseCreated(string provider, TContext context)
		{
		}

		protected string GetConnectionString(string provider)
		{
			var efProvider = provider + ".EF";
			var connectionString = DataConnection.TryGetConnectionString(efProvider);

			if (connectionString == null)
			{
				var originalCS = connectionString = DataConnection.GetConnectionString(provider);
				var dbProvider = DataConnection.GetDataProvider(provider);

				// create and register ef-specific connection string
				// and create database if needed (if EnsureCreated doesn't do it)
				switch (provider)
				{
					case var _ when provider.IsAnyOf(TestProvName.AllSqlServer):
					{
						var cnb = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString);
						cnb.InitialCatalog += ".ef";
						connectionString = cnb.ConnectionString;
						break;
					}
					case var _ when provider.IsAnyOf(TestProvName.AllPostgreSQL):
					{
						var cnb = new Npgsql.NpgsqlConnectionStringBuilder(connectionString);
						cnb.Database += "_ef";
						connectionString = cnb.ConnectionString;
						break;
					}
#if !NET9_0
					case var _ when provider.IsAnyOf(TestProvName.AllMySql):
					{
						var cnb = new MySqlConnectionStringBuilder(connectionString);
						cnb.Database += "_ef";
						cnb.PersistSecurityInfo = true;
						connectionString = cnb.ConnectionString;
						break;
					}
#endif
					case var _ when provider.IsAnyOf(TestProvName.AllSQLite):
					{
						var cnb = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(connectionString);
						cnb.DataSource = $"sqlite.{provider}.ef.db";
						connectionString = cnb.ConnectionString;
						break;
					}
					default:
						throw new InvalidOperationException($"{nameof(GetConnectionString)} is not implemented for provider {provider}");
				}

				DataConnection.AddConfiguration(efProvider, connectionString, dbProvider);
			}

			return connectionString;
		}

		private void InitializeDatabase(TContext context, string provider, string connectionString)
		{
			using var _ = new DisableBaseline("create db");

			context.Database.EnsureDeleted();
			context.Database.EnsureCreated();

			TestContextTracker.LastContexts[connectionString] = typeof(TContext);

			OnDatabaseCreated(provider, context);

			// remove potential CT pollution by OnDatabaseCreated
#if !NETFRAMEWORK
			context.ChangeTracker.Clear();
#else
			var undetachedEntriesCopy = context.ChangeTracker.Entries()
				.Where(e => e.State != EntityState.Detached)
				.ToList();

			foreach (var entry in undetachedEntriesCopy)
				entry.State = EntityState.Detached;
#endif
		}

		protected TContext CreateContext(string provider, Func<DataOptions, DataOptions>? optionsSetter = null, Func<DbContextOptionsBuilder<TContext>, DbContextOptionsBuilder<TContext>>? optionsBuilderSetter = null)
		{
			var connectionString = GetConnectionString(provider);

			var optionsBuilder = new DbContextOptionsBuilder<TContext>();
			optionsBuilder.UseLoggerFactory(LoggerFactory);

			// 20 cached contexts is not enough for us when tests run for multiple providers
			optionsBuilder.ConfigureWarnings(warnings => warnings.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));

			optionsBuilder = ProviderSetup(provider, connectionString, optionsBuilder);

			if (optionsSetter! != null)
				optionsBuilder.UseLinqToDB(builder => builder.AddCustomOptions(optionsSetter));

			if (optionsBuilderSetter! != null)
				optionsBuilder = optionsBuilderSetter(optionsBuilder);

			var ctx = CreateProviderContext(provider, optionsBuilder.Options);

			if (!TestContextTracker.LastContexts.TryGetValue(connectionString, out var contextType) || contextType != (typeof(TContext)))
				InitializeDatabase(ctx, provider, connectionString);

			return ctx;
		}
	}
}
