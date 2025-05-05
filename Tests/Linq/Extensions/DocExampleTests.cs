using System.Linq;

using LinqToDB;
using LinqToDB.DataProvider.Access;
using LinqToDB.DataProvider.MySql;
using LinqToDB.DataProvider.Oracle;
using LinqToDB.DataProvider.PostgreSQL;
using LinqToDB.DataProvider.SqlCe;
using LinqToDB.DataProvider.SQLite;
using LinqToDB.DataProvider.SqlServer;

using NUnit.Framework;

namespace Tests.Extensions
{
	[TestFixture]
	public class DocExampleTests : TestBase
	{
		[Test]
		public void AccessTest([IncludeDataSources(true, TestProvName.AllAccess)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				select p
			)
			.QueryHint(AccessHints.Query.WithOwnerAccessOption);

			_ = q.ToList();
		}

		[Test]
		public void MySqlTest([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in
					(
						from p in db.Parent.TableID("Pr")
							.TableHint(MySqlHints.Table.NoBka)
							.TableHint(MySqlHints.Table.Index, "PK_Parent")
						from c in db.Child.TableID("Ch")
							.IndexHint(MySqlHints.Table.UseKeyForOrderBy, "IX_ChildIndex", "IX_ChildIndex2")
						select p
					)
					.AsSubQuery("qq")
				select p
			)
			.QueryHint(MySqlHints.Query.NoBka,  Sql.TableSpec("Pr"), Sql.TableSpec("Ch"))
			.QueryHint(MySqlHints.Query.SetVar, "sort_buffer_size=16M");

			_ = q.ToList();
		}

		[Test]
		public void OracleTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in
					(
						from c in db.Child
							.TableHint(OracleHints.Hint.Full)
							.TableHint(OracleHints.Hint.Parallel, "DEFAULT")
						join p in db.Parent
							.TableHint(OracleHints.Hint.DynamicSampling, 1)
							.TableHint(OracleHints.Hint.Index, "parent_ix")
							.AsSubQuery("Parent")
						on c.ParentID equals p.ParentID
						select p
					)
					.AsSubQuery()
				select p
			)
			.QueryHint(OracleHints.Hint.NoUnnest, "@Parent");

			_ = q.ToList();
		}

		[Test]
		public void PostgreSQLTest([IncludeDataSources(true, TestProvName.AllPostgreSQL95Plus)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in
					(
						from p in
							(
								from p in db.Parent
								from c in db.Child
								where c.ParentID == p.ParentID
								select p
							)
							.SubQueryHint(PostgreSQLHints.ForUpdate)
							.AsSubQuery()
						where p.ParentID < -100
						select p
					)
					.SubQueryHint(PostgreSQLHints.ForShare)
				select p
			)
			.SubQueryHint(PostgreSQLHints.ForKeyShare + " " + PostgreSQLHints.SkipLocked);

			_ = q.ToList();
		}

		[Test]
		public void SqlCeTest([IncludeDataSources(true, ProviderName.SqlCe)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Person
					.TableHint(SqlCeHints.Table.Index, "PK_Person")
					.With(SqlCeHints.Table.NoLock)
				select p;

			_ = q.ToList();
		}

		[Test]
		public void SQLiteTest([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Person.TableHint(SQLiteHints.Hint.IndexedBy("IX_PersonDesc"))
				where p.ID > 0
				select p;

			_ = q.ToList();
		}

		[Test]
		public void SqlServerTest([IncludeDataSources(true, TestProvName.AllSqlServer2012Plus)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
					.TableHint(SqlServerHints.Table.SpatialWindowMaxCells(10))
					.IndexHint(SqlServerHints.Table.Index, "IX_ChildIndex")
				join p in
					(
						from t in db.Parent.With(SqlServerHints.Table.NoLock)
						where t.Children.Any()
						select new { t.ParentID, t.Children.Count }
					)
					.JoinHint(SqlServerHints.Join.Hash) on c.ParentID equals p.ParentID
				select p
			)
			.QueryHint(SqlServerHints.Query.Recompile)
			.QueryHint(SqlServerHints.Query.Fast(10))
			.QueryHint(SqlServerHints.Query.HashJoin);

			_ = q.ToList();
		}

		[Test]
		public void DatabaseSpecificTest([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent.TableID("pr")
					.AsMySql()
						.NoBatchedKeyAccessHint()
						.IndexHint("PK_Parent")
				from c in db.Child.TableID("ch")
					.AsMySql()
						.UseIndexHint("IX_ChildIndex")
					.AsOracle()
						.FullHint()
						.HashHint()
					.AsSqlCe()
						.WithNoLock()
					.AsSQLite()
						.NotIndexedHint()
					.AsSqlServer()
						.WithNoLock()
						.WithNoWait()
				join t in db.Patient.TableID("pt")
					.AsSqlServer()
						.JoinLoopHint()
				on c.ParentID equals t.PersonID
				select t
			)
			.QueryName("qb")
			.AsAccess()
				.WithOwnerAccessOption()
			.AsMySql()
				.MaxExecutionTimeHint(1000)
				.BatchedKeyAccessHint(Sql.TableSpec("ch"))
			.AsOracle()
				.ParallelHint(2)
				.NoUnnestHint("qb")
			.AsPostgreSQL()
				.ForShareHint(Sql.TableAlias("pt"))
			.AsSqlServer()
				.WithReadUncommittedInScope()
				.OptionRecompile()
				.OptionTableHint(Sql.TableAlias("pr"), SqlServerHints.Table.ReadUncommitted)
				.OptionNoPerformanceSpool()
			;

			_ = q.ToList();
		}
	}
}
