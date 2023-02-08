using System;
using System.Linq;

using LinqToDB;
using LinqToDB.DataProvider.SqlServer;

using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class TemporalTableTests : TestBase
	{
		[Test]
		public void Test([IncludeDataSources(false, TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataContext(context))
			{
				//db.InlineParameters = true;

				var q =
					from p in db.Parent
						.AsSqlServer()
						//.TemporalTableHint("AS OF", Sql.AsSql(DateTime.Now))
						.TemporalTableHint("AS OF", new (2023, 1, 1))
						.InlineParameters()
					select p;

				_ = q.ToList();
			}
		}
	}
}
