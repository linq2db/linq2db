using System;
using System.Diagnostics;
using System.Linq;

using LinqToDB;

using NUnit.Framework;

namespace Tests.Linq
{
	using Model;

	[TestFixture]
	public class QueryExtensionTests : TestBase
	{
		[Test]
		public void SelfJoinWithDifferentHint([NorthwindDataContext] string context)
		{
			using var db = new NorthwindDB(context);

			var query =
				from p in db.GetTable<JoinOptimizeTests.AdressEntity>().TableHint("NOLOCK")
				join a in db.GetTable<JoinOptimizeTests.AdressEntity>()
					on p.Id equals a.Id //PK column
				select p;

			Console.WriteLine(query);

			Assert.AreEqual(1, query.GetTableSource().Joins.Count);
		}

		[Test]
		public void SelfJoinWithDifferentHint2([NorthwindDataContext] string context)
		{
			using var db = new NorthwindDB(context);

			var query =
				from p in db.GetTable<JoinOptimizeTests.AdressEntity>().TableHint("NOLOCK")
				join a in db.GetTable<JoinOptimizeTests.AdressEntity>().TableHint("READUNCOMMITTED")
					on p.Id equals a.Id //PK column
				select p;

			Debug.WriteLine(query);

			Assert.AreEqual(1, query.GetTableSource().Joins.Count);
		}
	}
}
