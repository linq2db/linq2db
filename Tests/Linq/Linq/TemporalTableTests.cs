using System;
using System.Linq;
using System.Threading;

using FluentAssertions;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.SqlServer;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;
using LinqToDB.Tools.DataProvider.SqlServer.Schemas;

using NUnit.Framework;

using Tests.Model;

namespace Tests.Linq
{
	[TestFixture]
	public class TemporalTableTests : TestBase
	{
		public class TemporalTest
		{
			[PrimaryKey]
			public int      ID;
			public string?  Name;
			[Column(SkipOnUpdate = true, SkipOnInsert = true)]
			public DateTime StartedOn;
			[Column(SkipOnUpdate = true, SkipOnInsert = true)]
			public DateTime EndedOn;
		}

		TemporalTest[] CreateTestTable(ITestDataContext db)
		{
			using var dc = db is TestDataConnection dcx ?
				new DataConnection(db.Options.UseConnection(dcx.DataProvider, dcx.OpenConnection())) :
				new DataConnection(db.ConfigurationString);

			using var sy = new SystemDB(db.ConfigurationString!);

			if (!sy.Object.Objects.Any(o => o.ObjectID == SqlFn.ObjectID("dbo.TemporalTest")))
			{
				dc.Execute(@"
-- ALTER TABLE [dbo].[TemporalTest] SET ( SYSTEM_VERSIONING = OFF)
-- DROP TABLE [dbo].[TemporalTest]
-- DROP TABLE [dbo].[TemporalTestHistory]

					CREATE TABLE dbo.TemporalTest
					(
						[ID]        int NOT NULL PRIMARY KEY CLUSTERED,
						[Name]      nvarchar(100) NOT NULL,
						[StartedOn] datetime2 GENERATED ALWAYS AS ROW START,
						[EndedOn]   datetime2 GENERATED ALWAYS AS ROW END,
						PERIOD FOR SYSTEM_TIME (StartedOn, EndedOn)
					)
					WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.TemporalTestHistory))");

				db.Insert(new TemporalTest { ID = 1, Name = "Name1" });
				Thread.Sleep(250);
				db.Update(new TemporalTest { ID = 1, Name = "Name2" });
				Thread.Sleep(250);
				db.Update(new TemporalTest { ID = 1, Name = "Name3" });
			}

			return db
				.GetTable<TemporalTest>()
				.AsSqlServer()
				.TemporalTableAll()
				.OrderBy(t => t.StartedOn)
				.ToArray();
		}

		[Test]
		public void AsOfTest([IncludeDataSources(true, TestProvName.AllSqlServer2016Plus)] string context, [Values] bool inlinParameters)
		{
			using var _  = new DisableBaseline("Current datetime parameters used");
			using var db = GetDataContext(context);

			var data = CreateTestTable(db);

			db.InlineParameters = inlinParameters;

			var q =
				from p in db.GetTable<TemporalTest>()
					.AsSqlServer()
					.TemporalTableHint(SqlServerHints.TemporalTable.AsOf, data[1].StartedOn.AddMilliseconds(100))
				select p;

			var list = q.ToList();

			Assert.That(list[0].StartedOn, Is.EqualTo(data[1].StartedOn));
		}

		[Test]
		public void AsOfTest2([IncludeDataSources(true, TestProvName.AllSqlServer2016Plus)] string context, [Values] bool inlinParameters)
		{
			using var _  = new DisableBaseline("Current datetime parameters used");
			using var db = GetDataContext(context);

			var data = CreateTestTable(db);

			db.InlineParameters = inlinParameters;

			var n = 1;

			var q =
				from p in db.GetTable<TemporalTest>()
					.AsSqlServer()
					.TemporalTableAsOf(data[0].StartedOn.AddMilliseconds(100))
				where p.ID == n
				select p;

			var list = q.ToList();

			Assert.That(list[0].StartedOn, Is.EqualTo(data[0].StartedOn));
		}

		[Test]
		public void RangeTest(
			[IncludeDataSources(true, TestProvName.AllSqlServer2016Plus)] string context,
			[Values(SqlServerHints.TemporalTable.FromTo, SqlServerHints.TemporalTable.Between, SqlServerHints.TemporalTable.ContainedIn)] string hint,
			[Values] bool inlinParameters)
		{
			using var _  = new DisableBaseline("Current datetime parameters used");
			using var db = GetDataContext(context);

			var data = CreateTestTable(db);

			db.InlineParameters = inlinParameters;

			var q =
				from p in db.GetTable<TemporalTest>()
					.AsSqlServer()
					.TemporalTableHint(hint, data[0].StartedOn.AddMilliseconds(-100), data[1].StartedOn.AddMilliseconds(100))
				orderby p.StartedOn
				select p;

			var list = q.ToList();

			Assert.That(list[0].StartedOn, Is.EqualTo(data[0].StartedOn));
		}

		[Test]
		public void FromToTest([IncludeDataSources(true, TestProvName.AllSqlServer2016Plus)] string context)
		{
			using var _  = new DisableBaseline("Current datetime parameters used");
			using var db = GetDataContext(context);

			var data = CreateTestTable(db);

			var q =
				from p in db.GetTable<TemporalTest>()
					.AsSqlServer()
					.TemporalTableFromTo(data[0].StartedOn.AddMilliseconds(-100), data[1].StartedOn.AddMilliseconds(100))
				orderby p.StartedOn
				select p;

			var list = q.ToList();

			Assert.That(list[0].StartedOn, Is.EqualTo(data[0].StartedOn));
		}

		[Test]
		public void BetweenTest([IncludeDataSources(true, TestProvName.AllSqlServer2016Plus)] string context)
		{
			using var _  = new DisableBaseline("Current datetime parameters used");
			using var db = GetDataContext(context);

			var data = CreateTestTable(db);

			var q =
				from p in db.GetTable<TemporalTest>()
					.AsSqlServer()
					.TemporalTableBetween(data[0].StartedOn.AddMilliseconds(-100), data[1].StartedOn.AddMilliseconds(100))
				join t in db.GetTable<TemporalTest>()
					.AsSqlServer()
					.TemporalTableBetween(data[0].StartedOn.AddMilliseconds(-100), data[1].StartedOn.AddMilliseconds(100))
					.InlineParameters()
				on p.ID equals t.ID
				orderby p.StartedOn
				select p;

			var list = q.ToList();

			Assert.That(list[0].StartedOn, Is.EqualTo(data[0].StartedOn));
		}

		[Test]
		public void ContainedInTest([IncludeDataSources(true, TestProvName.AllSqlServer2016Plus)] string context)
		{
			using var _  = new DisableBaseline("Current datetime parameters used");
			using var db = GetDataContext(context);

			var data = CreateTestTable(db);

			var d1 = data[0].StartedOn.AddMilliseconds(-100);
			var d2 = data[1].StartedOn.AddMilliseconds(100);

			var q =
				from p in db.GetTable<TemporalTest>()
					.AsSqlServer()
					.TemporalTableContainedIn(d1, d2)
				join t in db.GetTable<TemporalTest>()
					.AsSqlServer()
					.TemporalTableContainedIn(d1, d2)
				on p.ID equals t.ID
				orderby p.StartedOn
				select p;

			var list = q.ToList();

			Assert.That(list[0].StartedOn, Is.EqualTo(data[0].StartedOn));
		}

		[Test]
		public void TemporalNoOptimization([IncludeDataSources(true, TestProvName.AllSqlServer2012Plus)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from t in db.GetTable<TemporalTest>()
				from p in db.GetTable<TemporalTest>()
					.AsSqlServer()
					.TemporalTableAsOf(TestData.DateTime)
					.Where(p => p.ID == t.ID)
					.DefaultIfEmpty()
				select t;

			_ = q.ToSqlQuery();

			var selectQuery = q.GetSelectQuery();

			selectQuery.From.Tables.Should().HaveCount(1);
			selectQuery.From.Tables[0].Joins.Should().HaveCount(1);
			selectQuery.From.Tables[0].Joins[0].JoinType.Should().Be(JoinType.Left);
		}

	}
}
