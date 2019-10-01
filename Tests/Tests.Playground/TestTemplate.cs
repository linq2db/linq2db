using System;
using System.Diagnostics;
using System.Linq;

using LinqToDB;
using LinqToDB.Tools;

using NUnit.Framework;

namespace Tests.Playground
{
	using Model;

	[TestFixture]
	public class ValuesTableTests : TestBase
	{
		[Test, Explicit("Work In Progress")]
		public void AsValuesTableTests([IncludeDataSources(ProviderName.SqlServer2014)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var arr = new[]
				{
					new { Field1 = 1, Field2 = "1" },
					new { Field1 = 2, Field2 = "2" },
				};

				var q = arr.AsValuesTable(db);

				foreach (var item in q)
				{
					Debug.WriteLine(item);
				}
			}
		}

		readonly Parent[] _parenArray =
		{
			new Parent { ParentID = 1, Value1 = 1    },
			new Parent { ParentID = 2, Value1 = null },
			new Parent { ParentID = 3, Value1 = 3    },
			new Parent { ParentID = 4, Value1 = 4    },
			new Parent { ParentID = 5, Value1 = null },
		};

		[Test, Explicit("Work In Progress")]
		public void InArrayTest1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var arr = _parenArray.Select(p => new { p.ParentID, p.Value1 });

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

		[Test, Explicit("Work In Progress")]
		public void InArrayTest2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				AreSame(
					from p in Parent
					where new { p.ParentID, p.Value1 }.In(_parenArray.Select(t => new { t.ParentID, t.Value1 }))
					orderby p.ParentID, p.Value1
					select p,
					from p in db.Parent
					where new { p.ParentID, p.Value1 }.In(_parenArray.Select(t => new { t.ParentID, t.Value1 }))
					orderby p.ParentID, p.Value1
					select p);
			}
		}

		[Test]
		public void NotInArrayTest1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var arr = _parenArray.Select(p => new { p.ParentID, p.Value1 });

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
		public void NotInArrayTest2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				AreSame(
					from p in Parent
					where new { p.ParentID, p.Value1 }.NotIn(_parenArray.Select(t => new { t.ParentID, t.Value1 }))
					orderby p.ParentID, p.Value1
					select p,
					from p in db.Parent
					where new { p.ParentID, p.Value1 }.NotIn(_parenArray.Select(t => new { t.ParentID, t.Value1 }))
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
