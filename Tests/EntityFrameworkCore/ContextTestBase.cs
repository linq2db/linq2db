using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

using LinqToDB.Data;
using LinqToDB.EntityFrameworkCore.Tests.Logging;
using LinqToDB.Tools;
using LinqToDB.Tools.Comparers;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

using NUnit.Framework;

using Tests;

namespace LinqToDB.EntityFrameworkCore.Tests
{
	public abstract class ContextTestBase<TContext> : TestBase
		where TContext: DbContext
	{
		private static HashSet<(Type, string)> _createdDbs = new HashSet<(Type, string)>();

		protected virtual DbContextOptionsBuilder<TContext> ProviderSetup(string provider, string connectionString, DbContextOptionsBuilder<TContext> optionsBuilder)
		{
			return provider switch
			{
				_ when provider.IsAnyOf(TestProvName.AllPostgreSQL) => optionsBuilder.UseNpgsql(connectionString),
				_ when provider.IsAnyOf(TestProvName.AllMySql) => optionsBuilder
#if !NETFRAMEWORK
					.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)),
#else
					.UseMySql(connectionString),
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

		protected TContext CreateContext(string provider, Func<DataOptions, DataOptions>? optionsSetter = null, Func<DbContextOptionsBuilder<TContext>, DbContextOptionsBuilder<TContext>>? optionsBuilderSetter = null)
		{
			var connectionString = DataConnection.GetConnectionString(provider);

			var optionsBuilder = new DbContextOptionsBuilder<TContext>();
			optionsBuilder.UseLoggerFactory(LoggerFactory);

			optionsBuilder = ProviderSetup(provider, connectionString, optionsBuilder);

			if (optionsSetter! != null)
				optionsBuilder.UseLinqToDB(builder => builder.AddCustomOptions(optionsSetter));

			if (optionsBuilderSetter! != null)
				optionsBuilder = optionsBuilderSetter(optionsBuilder);

			var ctx = CreateProviderContext(provider, optionsBuilder.Options);

			if (_createdDbs.Add((typeof(TContext), connectionString)))
			{
				using var _ = new DisableBaseline("create db");
				ctx.Database.EnsureDeleted();
				ctx.Database.EnsureCreated();
				OnDatabaseCreated(provider, ctx);
			}

			return ctx;
		}
	}
}
