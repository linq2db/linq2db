using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Internal.Extensions;
using LinqToDB.Linq;
using LinqToDB.SqlQuery;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue5212Tests : TestBase
	{
		[Sql.Expression("unarytest", Precedence = Precedence.Primary)]
		private static int UnaryTest(int v) => -v;

		static Issue5212Tests()
		{
			Expressions.MapUnary<int, int>((v) => -v, (v) => UnaryTest(v));
		}

		[Test]
		public void MapUnary([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using (var db = GetDataContext(context))
			{
				try
				{
					var query = from p in db.Parent
								where -p.ParentID == 0
								select p;

					var _ = query.ToList();
				}
				catch { }

				Assert.That(((DataConnection)db).LastQuery, Does.Contain("unarytest"));
			}
		}
	}
}
