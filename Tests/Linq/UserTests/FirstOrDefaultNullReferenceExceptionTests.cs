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
	public class FirstOrDefaultNullReferenceExceptionTests : TestBase
	{
		[Table("GrandChild")]
		class Table1
		{
			[Column] public int ChildID { get; set; }
		}

		[Table("Child")]
		class Table2
		{
			[Column] public int ChildID  { get; set; }
			[Column] public int ParentID { get; set; }

			[Association(ThisKey = "ChildID", OtherKey = "ChildID", CanBeNull = true)]
			public List<Table1> GrandChildren { get; set; }
		}

		[Table("Parent")]
		class Table3
		{
			[Column] public int ParentID { get; set; }

			[Association(ThisKey = "ParentID", OtherKey = "ParentID", CanBeNull = true)]
			public List<Table2> Children { get; set; }
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
					let t1 = t3.Children.SelectMany(x => x.GrandChildren)
					//let t2 = t3.Children.SelectMany(x => x.GrandChildren)
					select new
					{
						c2 = t1.Count(),
						c1 = t3.Children.SelectMany(x => x.GrandChildren).Count(),
					};

				query.FirstOrDefault(p => p.c2 > 1);
				query.FirstOrDefault();
			}
		}
	}
}
