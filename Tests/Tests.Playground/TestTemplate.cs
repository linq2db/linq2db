using System;
using System.Linq;
using LinqToDB.Tools;
using NUnit.Framework;

namespace Tests.Playground
{
	[TestFixture]
	public class ValuesTests : TestBase
	{
		[Test]
		public void InArrayTest([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var arr = new[]
				{
					new { ParentID = 1, Value1 = (int?)1    },
					new { ParentID = 2, Value1 = (int?)null },
					new { ParentID = 3, Value1 = (int?)3    },
					new { ParentID = 4, Value1 = (int?)4    },
					new { ParentID = 5, Value1 = (int?)null },
				};

				AreSame(
					from p in Parent
					where new { p.ParentID, p.Value1 }.In(arr)
					orderby p.ParentID, p.Value1
					select p,
					from p in db.Parent
					where new { p.ParentID, p.Value1 }.In(arr)
					orderby p.ParentID, p.Value1
					select p);
			}
		}

		[Test]
		public void NotInArrayTest([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var arr = new[]
				{
					new { ParentID = 1, Value1 = (int?)1    },
					new { ParentID = 2, Value1 = (int?)null },
					new { ParentID = 3, Value1 = (int?)3    },
					new { ParentID = 4, Value1 = (int?)4    },
					new { ParentID = 5, Value1 = (int?)null },
				};

				AreSame(
					from p in Parent
					where new { p.ParentID, p.Value1 }.NotIn(arr)
					orderby p.ParentID, p.Value1
					select p,
					from p in db.Parent
					where new { p.ParentID, p.Value1 }.NotIn(arr)
					orderby p.ParentID, p.Value1
					select p);
			}
		}

		[Test]
		public void Test2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var data =
				(
					from p in db.Parent
					where new { p.ParentID, p.Value1 }.In(new[]
					{
						new { ParentID = 1, Value1 = (int?)1 },
						new { ParentID = 2, Value1 = (int?)2 },
						new { ParentID = 3, Value1 = (int?)3 },
					})
					select p
				)
				.ToList();
			}
		}
	}
}
