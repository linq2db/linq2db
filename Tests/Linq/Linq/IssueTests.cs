using System;
using System.Diagnostics;
using System.Linq;

using LinqToDB;

using NUnit.Framework;

using Tests.Model;

namespace Tests.Linq
{
	[TestFixture]
	public class IssueTests : TestBase
	{
		// https://github.com/linq2db/linq2db/issues/38
		//
		[Test, DataContextSource(false)]
		public void Issue38Test(string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					from a in Child
					select new { Count = a.GrandChildren.Count() },
					from a in db.Child
					select new { Count = a.GrandChildren1.Count() });

				var sql = ((TestDataConnection)db).LastQuery;

				Assert.That(sql, Is.Not.Contains("INNER JOIN"));

				Debug.WriteLine(sql);
			}
		}

		// https://github.com/linq2db/linq2db/issues/42
		//
		[Test, IncludeDataContextSource(ProviderName.Access)]
		public void Issue42Test(string context)
		{
			using (var db = GetDataContext(context))
			{
				var t1 = db.Types2.First();

				t1.BoolValue = !t1.BoolValue;

				db.Update(t1);

				var t2 = db.Types2.First();

				Assert.That(t2.BoolValue, Is.EqualTo(t1.BoolValue));

				t1.BoolValue = !t1.BoolValue;

				db.Update(t1);
			}
		}
	}
}
