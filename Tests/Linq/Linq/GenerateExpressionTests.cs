using System;
using System.Linq;

using LinqToDB;

using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class GenerateExpressionTests : TestBase
	{
		[OneTimeSetUp]
		public void SetUp()
		{
			LinqToDB.Common.Configuration.Linq.GenerateExpressionTest = true;
		}

		[OneTimeTearDown]
		public void TearDown()
		{
			LinqToDB.Common.Configuration.Linq.GenerateExpressionTest = false;
		}

		[Test, IncludeDataContextSource(ProviderName.SQLite, TestProvName.SQLiteMs)]
		public void Test1(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q2 =
					from gc1 in db.GrandChild
						join max in
							from gch in db.GrandChild
							group gch by gch.ChildID into g
							select g.Max(c => c.GrandChildID)
						on gc1.GrandChildID equals max
					select gc1;

				var result =
					from ch in db.Child
						join p   in db.Parent on ch.ParentID equals p.ParentID
						join gc2 in q2        on p.ParentID  equals gc2.ParentID into g
						from gc3 in g.DefaultIfEmpty()
				where gc3 == null || !new[] { 111, 222 }.Contains(gc3.GrandChildID.Value)
				select new { p.ParentID, gc3 };

				result.ToList();
			}
		}
	}
}
