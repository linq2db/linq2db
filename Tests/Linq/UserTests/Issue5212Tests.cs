using System.Linq;

using LinqToDB;
using LinqToDB.Data;
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

		[Test]
		[Ignore("Test disabled, cause MapUnary/MapBinary calls could not be removed and could so break other tests.")]
		public void MapUnary([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			Expressions.MapUnary<int, int>((v) => -v, (v) => UnaryTest(v));

			using (var db = GetDataContext(context))
			{
				try
				{
					var query = from p in db.Parent
								where -p.ParentID == 0
								select p;

					query.ToList();
				}
				catch { }

				Assert.That(((DataConnection)db).LastQuery, Does.Contain("unarytest"));
			}
		}
	}
}
