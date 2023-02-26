using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.SqlServer;
using LinqToDB.Mapping;
using LinqToDB.Tools.DataProvider.SqlServer.Schemas;

using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class TemporalTableTests : TestBase
	{
		class TemporalTest
		{
			public int      ID;
			public string?  Name;
			[Column(SkipOnUpdate = true, SkipOnInsert = true)]
			public DateTime StartedOn;
			[Column(SkipOnUpdate = true, SkipOnInsert = true)]
			public DateTime EndedOn;
		}

		[Test]
		public void Test([IncludeDataSources(false, TestProvName.AllSqlServer2016Plus)] string context, [Values(true, false)] bool inlinParameters)
		{
			using var db = GetDataConnection(context);
			using var sy = new SystemDB(db.Options.WithOptions<ConnectionOptions>(o => o with { DbConnection = db.Connection, DataProvider = db.DataProvider}));

			if (!sy.Object.Objects.Any(o => o.ObjectID == SqlFn.ObjectID("dbo.TemporalTest")))
			{
				db.Execute(@"
					CREATE TABLE dbo.TemporalTest
					(
						[ID]        int NOT NULL PRIMARY KEY CLUSTERED,
						[Name]      nvarchar(100) NOT NULL,
						[StartedOn] datetime2 GENERATED ALWAYS AS ROW START,
						[EndedOn]   datetime2 GENERATED ALWAYS AS ROW END,
						PERIOD FOR SYSTEM_TIME (StartedOn, EndedOn)
					)
					WITH (SYSTEM_VERSIONING = ON)");
			}

			db.InlineParameters = inlinParameters;

			var n = 0;

			var q =
				from p in db.GetTable<TemporalTest>()
					.AsSqlServer()
					.TemporalTableHint("AS OF", Sql.AsSql(DateTime.Now))
					//.TemporalTableHint("AS OF", new (2023, 1, 1))
				//.InlineParameters()
				where p.ID == n
				select p;

			_ = q.ToList();
		}
	}
}
