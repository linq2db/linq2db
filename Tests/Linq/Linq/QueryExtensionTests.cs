using System;
using System.Diagnostics;
using System.Linq;

using LinqToDB;
using LinqToDB.DataProvider.SqlServer;
using LinqToDB.DataProvider.Oracle;

using NUnit.Framework;

namespace Tests.Linq
{
	using Model;

	[TestFixture]
	public class QueryExtensionTests : TestBase
	{
		[Test]
		public void SelfJoinWithDifferentHint([NorthwindDataContext] string context)
		{
			using var db = new NorthwindDB(context);

			var query =
				from p in db.GetTable<JoinOptimizeTests.AdressEntity>().TableHint("NOLOCK")
				join a in db.GetTable<JoinOptimizeTests.AdressEntity>()
					on p.Id equals a.Id //PK column
				select p;

			Console.WriteLine(query);

			Assert.AreEqual(1, query.GetTableSource().Joins.Count);
		}

		[Test]
		public void SelfJoinWithDifferentHint2([NorthwindDataContext] string context)
		{
			using var db = new NorthwindDB(context);

			var query =
				from p in db.GetTable<JoinOptimizeTests.AdressEntity>().TableHint("NOLOCK")
				join a in db.GetTable<JoinOptimizeTests.AdressEntity>().TableHint("READUNCOMMITTED")
					on p.Id equals a.Id //PK column
				select p;

			Debug.WriteLine(query);

			Assert.AreEqual(1, query.GetTableSource().Joins.Count);
		}

		[Test]
		public void DatabaseSpecificTest([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
					.AsSqlServerSpecific()
						.TableHint(SqlServerHints.Table.NoLock)
						.TableHint(SqlServerHints.Table.NoWait)
					.AsOracleSpecific()
						.TableHint(OracleHints.Table.Full)
						.TableHint(OracleHints.Table.Hash)
				select p
			)
			.AsSqlServerSpecific()
			.AsOracleSpecific();

			_ = q.ToList();

			var sqlServerHints = "[Parent] [p] WITH (NoLock, NoWait)";
			var oracleHints    = "SELECT /*+ FULL(p) HASH(p) */";

			if (context.StartsWith("SqlServer"))
				Assert.That(LastQuery,Contains.Substring(sqlServerHints).And.Not.Contains(oracleHints));
			else if (context.StartsWith("Oracle"))
				Assert.That(LastQuery,Is.Not.Contains(sqlServerHints).And.Contains(oracleHints));
		}
	}
}
