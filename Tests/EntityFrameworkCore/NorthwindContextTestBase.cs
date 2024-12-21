using System;

using LinqToDB.EntityFrameworkCore.Tests.Models.Northwind;

using Microsoft.EntityFrameworkCore;

using Tests;

namespace LinqToDB.EntityFrameworkCore.Tests
{
	public abstract class NorthwindContextTestBase : ContextTestBase<NorthwindContextBase>
	{
		protected NorthwindContextBase CreateContext(string provider, bool enableFilter)
		{
			var ctx = CreateContext(provider);
			ctx.IsSoftDeleteFilterEnabled = enableFilter;
			return ctx;
		}

		protected override NorthwindContextBase CreateProviderContext(string provider, DbContextOptions<NorthwindContextBase> options)
		{
			return provider switch
			{
				_ when provider.IsAnyOf(TestProvName.AllPostgreSQL) => new PostgreSQL.Models.Northwind.NorthwindContext(options),
				_ when provider.IsAnyOf(TestProvName.AllMySql) => new Pomelo.Models.Northwind.NorthwindContext(options),
				_ when provider.IsAnyOf(TestProvName.AllSQLite) => new SQLite.Models.Northwind.NorthwindContext(options),
				_ when provider.IsAnyOf(TestProvName.AllSqlServer) => new SqlServer.Models.Northwind.NorthwindContext(options),
				_ => throw new InvalidOperationException($"{nameof(CreateProviderContext)} is not implemented for provider {provider}")
			};
		}

		protected override void OnDatabaseCreated(string provider, NorthwindContextBase context)
		{
			base.OnDatabaseCreated(provider, context);
			NorthwindData.Seed(context);
		}
	}
}
