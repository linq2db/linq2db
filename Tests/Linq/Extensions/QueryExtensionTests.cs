using System;
using System.Diagnostics;
using System.Linq;

using LinqToDB;
using LinqToDB.DataProvider.SqlCe;
using LinqToDB.DataProvider.SqlServer;
using LinqToDB.DataProvider.Oracle;

using NUnit.Framework;

namespace Tests.Extensions
{
	using Linq;
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
					.AsSqlServer()
						.WithNoLock()
						.WithNoWait()
					.AsSqlCe()
						.WithNoLock()
					.AsOracle()
						.FullHint()
						.HashHint()
				select p
			)
			.AsSqlServer()
				.WithReadUncommittedInScope()
			.AsOracle()
				.ParallelHint(2);

			_ = q.ToList();

			string sqlCeHints, sqlServerHints, oracleHints;

			var testSql = new[]
			{
				sqlCeHints     = "[Parent] [p] WITH (NoLock)",
				sqlServerHints = "[Parent] [p] WITH (NoLock, NoWait, ReadUncommitted)",
				oracleHints    = "SELECT /*+ FULL(p) HASH(p) PARALLEL(2) */",
			};

			string? current = null;

			if      (context.StartsWith("SqlCe"))     current = sqlCeHints;
			else if (context.StartsWith("SqlServer")) current = sqlServerHints;
			else if (context.StartsWith("Oracle"))    current = oracleHints;

			if (current != null)
			{
				foreach (var sql in testSql)
				{
					if (sql == current) Assert.That(LastQuery, Contains.Substring(sql));
					else                Assert.That(LastQuery, Is.Not.Contains(sql));
				}
			}
		}
	}
}
