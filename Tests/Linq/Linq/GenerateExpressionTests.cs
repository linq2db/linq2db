using System;
using System.Linq;

using LinqToDB;

using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class GenerateExpressionTests : TestBase
	{
		[Test]
		public void Test1([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using(new GenerateExpressionTest(true))
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
				where gc3 == null || 
						!new[] { 111, 222 }.Contains(gc3.GrandChildID!.Value)
				select new { p.ParentID, gc3 };


				var test = result.GenerateTestString();

				TestContext.Out.WriteLine(test);

				var _ = result.ToList();
			}
		}

		[Test]
		public void Test2([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q2 =
					from gc1 in db.Person
					where gc1.Gender == Model.Gender.Male
					select gc1;


				var test = q2.GenerateTestString();

				TestContext.Out.WriteLine(test);

				var _ = q2.ToList();
			}
		}
	}
}
