using System.Linq;

using NUnit.Framework;

namespace Tests.xUpdate
{
	using LinqToDB;

	// tests for iqueryable targets (cte, non-cte)
	public partial class MergeTests
	{
		[Test]
		[ActiveIssue(2363)]
		public void MergeIntoIQueryable([MergeDataContextSource] string context)
		{
			using (var db = GetDataContext(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = GetSource1(db)
					.MergeInto(table.Where(_ => _.Id >= 1))
					.OnTargetKey()
					.InsertWhenNotMatched()
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(2, rows, context);

				Assert.AreEqual(6, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);
				AssertRow(InitialTargetData[3], result[3], null, null);
				AssertRow(InitialSourceData[2], result[4], null, null);
				AssertRow(InitialSourceData[3], result[5], null, 216);
			}
		}

		[Test]
		[ActiveIssue(3015, Configurations = new[] 
		{ 
			"Access", "Access.Odbc", 
			"SqlCe", 
			"SQLite.Classic", "SQLite.MS", "SQLite.Classic.MPU", "SQLite.Classic.MPM",
			"PostgreSQL", "PostgreSQL.9.2", "PostgreSQL.9.3", "PostgreSQL.9.5", "PostgreSQL.10", "PostgreSQL.11", "PostgreSQL.12", "PostgreSQL.13",
			"MySql", "MySqlConnector", "MySql55", "MariaDB",
		})]
		public void MergeIntoCte([MergeDataContextSource] string context)
		{
			using (var db = GetDataContext(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = GetSource1(db)
					.MergeInto(table.Where(_ => _.Id >= 1).AsCte())
					.OnTargetKey()
					.InsertWhenNotMatched()
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(2, rows, context);

				Assert.AreEqual(6, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);
				AssertRow(InitialTargetData[3], result[3], null, null);
				AssertRow(InitialSourceData[2], result[4], null, null);
				AssertRow(InitialSourceData[3], result[5], null, 216);
			}
		}

		[Test]
		[ActiveIssue(2363)]
		public void MergeFromIQueryable([MergeDataContextSource] string context)
		{
			using (var db = GetDataContext(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table.Where(_ => _.Id >= 1)
					.Merge().Using(GetSource1(db))
					.OnTargetKey()
					.InsertWhenNotMatched()
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(2, rows, context);

				Assert.AreEqual(6, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);
				AssertRow(InitialTargetData[3], result[3], null, null);
				AssertRow(InitialSourceData[2], result[4], null, null);
				AssertRow(InitialSourceData[3], result[5], null, 216);
			}
		}

		[Test]
		[ActiveIssue(3015, Configurations = new[]
		{
			"Access", "Access.Odbc",
			"SqlCe",
			"SQLite.Classic", "SQLite.MS", "SQLite.Classic.MPU", "SQLite.Classic.MPM",
			"PostgreSQL", "PostgreSQL.9.2", "PostgreSQL.9.3", "PostgreSQL.9.5", "PostgreSQL.10", "PostgreSQL.11", "PostgreSQL.12", "PostgreSQL.13",
			"MySql", "MySqlConnector", "MySql55", "MariaDB",
		})]
		public void MergeFromCte([MergeDataContextSource] string context)
		{
			using (var db = GetDataContext(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table.Where(_ => _.Id >= 1).AsCte()
					.Merge().Using(GetSource1(db))
					.OnTargetKey()
					.InsertWhenNotMatched()
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(2, rows, context);

				Assert.AreEqual(6, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);
				AssertRow(InitialTargetData[3], result[3], null, null);
				AssertRow(InitialSourceData[2], result[4], null, null);
				AssertRow(InitialSourceData[3], result[5], null, 216);
			}
		}

		[Test]
		[ActiveIssue(3015, Configurations = new[]
		{
			"Access", "Access.Odbc",
			"SqlCe",
			"SQLite.Classic", "SQLite.MS", "SQLite.Classic.MPU", "SQLite.Classic.MPM",
			"PostgreSQL", "PostgreSQL.9.2", "PostgreSQL.9.3", "PostgreSQL.9.5", "PostgreSQL.10", "PostgreSQL.11", "PostgreSQL.12", "PostgreSQL.13",
			"MySql", "MySqlConnector", "MySql55", "MariaDB",
		})]
		public void MergeUsingCteJoin([MergeDataContextSource] string context)
		{
			using (var db = GetDataContext(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge().Using(GetSource1(db).Where(_ => _.Id >= 1).AsCte())
					.On(t => t.Id, s => s.Id)
					.InsertWhenNotMatched()
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(2, rows, context);

				Assert.AreEqual(6, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);
				AssertRow(InitialTargetData[3], result[3], null, null);
				AssertRow(InitialSourceData[2], result[4], null, null);
				AssertRow(InitialSourceData[3], result[5], null, 216);
			}
		}

		[Test]
		[ActiveIssue(3015, Configurations = new[]
		{
			"Access", "Access.Odbc",
			"SqlCe",
			"SQLite.Classic", "SQLite.MS", "SQLite.Classic.MPU", "SQLite.Classic.MPM",
			"PostgreSQL", "PostgreSQL.9.2", "PostgreSQL.9.3", "PostgreSQL.9.5", "PostgreSQL.10", "PostgreSQL.11", "PostgreSQL.12", "PostgreSQL.13",
			"MySql", "MySqlConnector", "MySql55", "MariaDB",
		})]
		public void MergeUsingCteWhere([MergeDataContextSource] string context)
		{
			using (var db = GetDataContext(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge().Using(GetSource1(db).Where(_ => _.Id >= 1).AsCte())
					.On((t, s) => t.Id == s.Id)
					.InsertWhenNotMatched()
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(2, rows, context);

				Assert.AreEqual(6, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);
				AssertRow(InitialTargetData[3], result[3], null, null);
				AssertRow(InitialSourceData[2], result[4], null, null);
				AssertRow(InitialSourceData[3], result[5], null, 216);
			}
		}
	}
}
