using System;
using System.Linq;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.Firebird;
using LinqToDB.SqlProvider;
using LinqToDB.SqlQuery;
using NUnit.Framework;
using Tests.DataProvider;

namespace Tests.UserTests
{
	[TestFixture]
	public class MsSqlDatetime2PrecisionTest : TestBase
	{
		[Test]
		public void TestDateTime2([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				var dateTime2 = new DateTime(2012, 12, 12, 12, 12, 12, 0).AddTicks(1);

				Assert.That(conn.Execute<DateTime> ("SELECT Cast('2012-12-12 12:12:12.0000001' as datetime2)"), Is.EqualTo(dateTime2));
				Assert.That(conn.Execute<DateTime?>("SELECT Cast('2012-12-12 12:12:12.0000001' as datetime2)"), Is.EqualTo(dateTime2));

				Assert.That(conn.Execute<DateTime> ("SELECT @p", DataParameter.DateTime2("p", dateTime2)),               Is.EqualTo(dateTime2));
				Assert.That(conn.Execute<DateTime> ("SELECT @p", DataParameter.Create   ("p", dateTime2)),               Is.EqualTo(dateTime2));
				Assert.That(conn.Execute<DateTime?>("SELECT @p", new DataParameter("p", dateTime2, DataType.DateTime2)), Is.EqualTo(dateTime2));
			}
		}
	}
}
