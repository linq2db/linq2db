using System;
using System.Data.Common;

using LinqToDB.Data;
using LinqToDB.DataProvider;

using Npgsql;

using NUnit.Framework;

using Shouldly;

using Tests;

namespace LinqToDB.EntityFrameworkCore.Tests
{
	/// <summary>
	/// The linq2db data provider resolved for an EF Core context must reflect the server that context
	/// connects to. The provider carries the SQL dialect, so one resolved from a different server
	/// silently generates SQL for the wrong version.
	/// </summary>
	[TestFixture]
	public class DataProviderCacheTests : TestBase
	{
		// RFC 2606 reserves .invalid, so this host cannot resolve. Resolving a provider for it has to fail
		// rather than quietly hand back the dialect detected from another connection.
		const string UnreachableConnectionString = "Host=linq2db-nonexistent.invalid;Database=x;Username=x;Password=x;Timeout=1;Pooling=false";

		/// <summary>
		/// A context configured with a <c>DbDataSource</c> (or an externally-supplied
		/// <see cref="DbConnection"/>) carries no connection string on its EF options extension, so
		/// <see cref="EFConnectionInfo.ConnectionString"/> is <see langword="null"/> and the connection is
		/// the only thing identifying the server. The provider cache has to key on it — otherwise every
		/// server of one family shares a single entry and all but the first are served the dialect detected
		/// from someone else's server.
		/// </summary>
		[Test]
		public void ProviderNotSharedBetweenConnectionsWithoutConnectionString([EFIncludeDataSources(TestProvName.AllPostgreSQL)] string provider)
		{
			using var configured  = new NpgsqlConnection(DataConnection.GetConnectionString(provider));
			using var unreachable = new NpgsqlConnection(UnreachableConnectionString);

			Resolve(configured).Name.ShouldBe(DataConnection.GetDataProvider(provider).Name);

			Shouldly.Should.Throw<Exception>(() => Resolve(unreachable));

			static IDataProvider Resolve(DbConnection connection)
				=> LinqToDBForEFTools.GetDataProvider(
					new DataOptions(),
					new EFProviderInfo   { Connection = connection },
					new EFConnectionInfo { Connection = connection });
		}
	}
}
