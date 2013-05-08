using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	using Model;
	using Tests;

	[TestFixture]
	public class FirstOrDefaultNullReferenceExceptionTest : TestBase
	{
		class Table1
		{
			public int Field1 { get; set; }
		}

		class Table2
		{
			public int Field1 { get; set; }
			public int Field2 { get; set; }

			[Association(ThisKey = "Field1", OtherKey = "Field1", CanBeNull = true)]
			public List<Table1> Table1s { get; set; }
		}

		class Table3
		{
			public int Field2 { get; set; }

			[Association(ThisKey = "Field2", OtherKey = "Field2", CanBeNull = true)]
			public List<Table2> Table2s { get; set; }
		}

		[Test]
		public void Test()
		{
			using (var db = new TestDataConnection())
			{
				/*
				var query =
					from t3 in db.Parent
					//let t1 = t3.Children.SelectMany(x => x.GrandChildren)
					//let t2 = t3.Table2s.SelectMany(x => x.Table1s)
					select new
					{
						//c2 = t1.Count(),
						c1 = t3.Children.SelectMany(x => x.GrandChildren),
					};
				 */

				var query =
					from t3 in db.GetTable<Table3>()
					let t1 = t3.Table2s.SelectMany(x => x.Table1s)
					//let t2 = t3.Table2s.SelectMany(x => x.Table1s)
					select new
					{
						c2 = t1.Count(),
						c1 = t3.Table2s.SelectMany(x => x.Table1s).Count(),
					};

				query.FirstOrDefault(p => p.c2 > 1);
				query.FirstOrDefault();
			}
		}
	}
}
