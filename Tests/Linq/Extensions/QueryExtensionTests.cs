using System;
using System.Diagnostics;
using System.Linq;

using LinqToDB;
using LinqToDB.DataProvider.Access;
using LinqToDB.DataProvider.Oracle;
using LinqToDB.DataProvider.MySql;
using LinqToDB.DataProvider.PostgreSQL;
using LinqToDB.DataProvider.SqlCe;
using LinqToDB.DataProvider.SQLite;
using LinqToDB.DataProvider.SqlServer;

using NUnit.Framework;

namespace Tests.Extensions
{
	using Linq;
	using Model;

	[TestFixture]
	public class QueryExtensionTests : TestBase
	{
		[Test]
		public void SelfJoinWithDifferentHintTest([NorthwindDataContext] string context)
		{
			using var db = new NorthwindDB(context);

			var query =
				from p in db.GetTable<JoinOptimizeTests.AdressEntity>().TableHint("NOLOCK")
				join a in db.GetTable<JoinOptimizeTests.AdressEntity>()
					on p.Id equals a.Id //PK column
				select p;

			Console.WriteLine(query);

			Assert.That(query.GetTableSource().Joins, Has.Count.EqualTo(1));
		}

		[Test]
		public void SelfJoinWithDifferentHintTest2([NorthwindDataContext(true)] string context)
		{
			using var db = new NorthwindDB(context);
			using var tb = db.CreateLocalTable<JoinOptimizeTests.AdressEntity>();

			var query =
				from p in db.GetTable<JoinOptimizeTests.AdressEntity>().TableHint("NOLOCK")
				join a in db.GetTable<JoinOptimizeTests.AdressEntity>().TableHint("READUNCOMMITTED")
					on p.Id equals a.Id //PK column
				select p;

			query.ToArray();

			Assert.That(query.GetTableSource().Joins, Has.Count.EqualTo(1));
		}

		[Test]
		public void DatabaseSpecificTest([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from t in db.Child.TableID("ch")
					.AsSqlServer()
						.WithNoLock()
						.WithNoWait()
					.AsSqlCe()
						.WithNoLock()
					.AsOracle()
						.FullHint()
						.HashHint()
					.AsMySql()
						.UseIndexHint("IX_ChildIndex")
					.AsSQLite()
						.NotIndexedHint()
				select t
			)
			.AsSqlServer()
				.WithReadUncommittedInScope()
				.OptionRecompile()
			.AsOracle()
				.ParallelHint(2)
			.AsAccess()
				.WithOwnerAccessOption()
			.AsMySql()
				.MaxExecutionTimeHint(1000)
			.AsPostgreSQL()
				.ForShareHint(Sql.TableAlias("ch"));

			_ = q.ToList();

			string sqlCeHints, sqlServerHints, oracleHints, mySqlHints, accessHints, postgreSQLHints, sqliteHints;

			var testSql = new[]
			{
				accessHints     = "WITH OWNERACCESS OPTION",
				oracleHints     = "SELECT /*+ FULL(t) HASH(t) PARALLEL(2) */",
				mySqlHints      = "SELECT /*+ MAX_EXECUTION_TIME(1000) */",
				sqlCeHints      = "[Child] [t] WITH (NoLock)",
				sqlServerHints  = "[Child] [t] WITH (NoLock, NoWait, ReadUncommitted)",
				postgreSQLHints = "FOR SHARE OF t",
				sqliteHints     = "NOT INDEXED",
			};

			string? current = null;

			if      (context.IsAnyOf(TestProvName.AllAccess    )) current = accessHints;
			else if (context.IsAnyOf(TestProvName.AllOracle    )) current = oracleHints;
			else if (context.IsAnyOf(TestProvName.AllMySql     )) current = mySqlHints;
			else if (context.IsAnyOf(ProviderName.SqlCe        )) current = sqlCeHints;
			else if (context.IsAnyOf(TestProvName.AllSqlServer )) current = sqlServerHints;
			else if (context.IsAnyOf(TestProvName.AllPostgreSQL)) current = postgreSQLHints;
			else if (context.IsAnyOf(TestProvName.AllSQLite    )) current = sqliteHints;

			if (current != null)
			{
				foreach (var sql in testSql)
				{
					if (sql == current) Assert.That(LastQuery, Contains.Substring(sql));
					else                Assert.That(LastQuery, Is.Not.Contains(sql));
				}
			}
		}

		[Test]
		public void UnionTest([DataSources(TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from t in db.Child.TableID("ch")
				select t
			)
			.Union
			(
				from t in db.Child
				where t.ChildID < 10
				select t
			)
			.AsSqlServer()
				.WithReadUncommittedInScope()
				.OptionRecompile()
			.AsOracle()
				.ParallelHint(2)
			.AsAccess()
				.WithOwnerAccessOption()
			.AsMySql()
				.MaxExecutionTimeHint(1000)
			;

			_ = q.ToList();
		}
	}
}
