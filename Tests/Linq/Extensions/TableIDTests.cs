using System;
using System.Linq;

using LinqToDB;

using NUnit.Framework;

namespace Tests.Extensions
{
	[TestFixture]
	public class TableIDTests : TestBase
	{
		[Sql.Expression("'*** {0} ***'", ServerSideOnly = true, CanBeNull = false)]
		static string PrintSqlID(Sql.SqlID id)
		{
			throw new NotImplementedException();
		}

		[Test]
		public void TableTest([DataSources(TestProvName.AllAccess)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from c in db.Child
				join p in db.Parent.TableID("pp").AsSubQuery() on c.ParentID equals p.ParentID
				where PrintSqlID(Sql.TableAlias("pp")) == PrintSqlID(new (Sql.SqlIDType.TableName, "pp"))
				select new
				{
					alias2 = PrintSqlID(new (Sql.SqlIDType.TableAlias, "pp")) + "4",
					alias1 = PrintSqlID(Sql.TableAlias("pp")),
					alias3 = PrintSqlID(Sql.TableName ("pp")),
					alias4 = PrintSqlID(Sql.TableSpec ("pp")),
				};

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("*** p ***"));
			Assert.That(LastQuery, Contains.Substring("*** p_1.p ***"));
			Assert.That(LastQuery,
				Contains.Substring("*** \"Parent\" ***")
					.Or.Contains("*** Parent ***")
					.Or.Contains("*** `Parent` ***")
					.Or.Contains("*** [Parent] ***"));
		}
	}
}
