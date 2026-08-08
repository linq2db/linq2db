using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

using Tests;

#if NETFRAMEWORK
using System.Linq;
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
#if !NET10_0
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

		private void InitializeDatabase(TContext context, string provider, string connectionString)
		{
			using var _ = new DisableBaseline("create db");

			try
			{
				context.Database.EnsureDeleted();
				context.Database.EnsureCreated();
			}
			catch (Exception ex) when (provider.IsAnyOf(TestProvName.AllPostgreSQL) && IsInvalidDatabaseError(ex))
			{
				// Recover exactly as Postgres's own HINT says, then retry once.
				DropPostgresDatabaseIfExists(connectionString);
				context.Database.EnsureCreated();
			}

			TestContextTracker.LastContexts[connectionString] = typeof(TContext);

			OnDatabaseCreated(provider, context);

			// remove potential CT pollution by OnDatabaseCreated
			ResetChangeTracker(context);
		}

		// EnsureDeleted()'s own Exists()-then-DROP path doesn't recover from Postgres's "invalid database"
		// state (observed here: a prior run's EnsureCreated() left the target database marked invalid - a
		// connection attempt fails with "55000: cannot connect to invalid database", which Exists() doesn't
		// treat as "doesn't exist" the way it does the ordinary "3D000: database does not exist", so the
		// invalid state persists and every subsequent test against that provider fails identically until
		// someone manually drops it). Postgres's own fix for this is exactly DROP DATABASE - it works on
		// both an invalid and a normal existing database.
		//
		// Scoped to fire only on that exact error, not unconditionally on every InitializeDatabase call:
		// InitializeDatabase reruns whenever the cached (connectionString, TContext type) pair in
		// TestContextTracker changes - i.e. potentially many times per suite run, not just once at the
		// start. An earlier version of this fix ran DROP DATABASE ... WITH (FORCE) unconditionally before
		// every EnsureDeleted/EnsureCreated call, which killed other still-pooled connections to the same
		// database on every one of those calls ("57P01: terminating connection due to administrator
		// command") - a regression worse than the problem it fixed.
		static bool IsInvalidDatabaseError(Exception ex)
		{
			for (var e = ex; e != null; e = e.InnerException)
				if (e is Npgsql.PostgresException { SqlState: "55000" } pg && pg.MessageText.Contains("invalid database", StringComparison.OrdinalIgnoreCase))
					return true;

			return false;
		}

		static void DropPostgresDatabaseIfExists(string connectionString)
		{
			var builder = new Npgsql.NpgsqlConnectionStringBuilder(connectionString);
			var dbName  = builder.Database!;

			builder.Database = "postgres";

			using var connection = new Npgsql.NpgsqlConnection(builder.ConnectionString);
			connection.Open();

			using var command = connection.CreateCommand();
			command.CommandText = $"DROP DATABASE IF EXISTS \"{dbName.Replace("\"", "\"\"")}\" WITH (FORCE);";
			command.ExecuteNonQuery();
		}

		protected static void ResetChangeTracker(TContext context)
		{
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
