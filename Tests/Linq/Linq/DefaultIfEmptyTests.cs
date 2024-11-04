using System.Linq;

using FluentAssertions;

using LinqToDB;
using LinqToDB.Linq;

using NUnit.Framework;

using Tests.Model;

namespace Tests.Linq
{
	public class DefaultIfEmptyTests : TestBase
	{
		[Test]
		[ThrowsForProvider(typeof(LinqException), TestProvName.AllSybase, "Provider has issue with JOIN to limited recordset.")]
		public void WithoutDefault([DataSources(TestProvName.AllAccess, TestProvName.AllClickHouse, TestProvName.AllInformix)] string context)
		{
			using var db = GetDataContext(context);

			var children = db.Child
				.Take(0)
				.DefaultIfEmpty()
				.ToList();

			children.Should().HaveCount(1);
		}

		[Test]
		[ThrowsForProvider(typeof(LinqException), TestProvName.AllSybase, "Provider has issue with JOIN to limited recordset.")]
		public void WithDefault([DataSources(TestProvName.AllAccess, TestProvName.AllClickHouse, TestProvName.AllInformix)] string context)
		{
			using var db = GetDataContext(context);

			var defaultValue = new Child{ Parent1 = new Parent1()};

			var children = db.Child
				.Take(0)
				.DefaultIfEmpty(defaultValue)
				.ToList();

			children.Should().HaveCount(1);

			children[0].Parent1.Should().BeSameAs(defaultValue.Parent1);
		}

		[Test]
		[ThrowsForProvider(typeof(LinqException), TestProvName.AllMySql57, TestProvName.AllOracle11, TestProvName.AllMariaDB, ErrorMessage = ErrorHelper.Error_OUTER_Joins)]
		public void WithDefaultInSubquery([DataSources(TestProvName.AllAccess, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);

			var defaultValue = new Child{ Parent1 = new Parent1()};

			var query =
				from p in db.Parent
				select new
				{
					Sum = p.Children.DefaultIfEmpty(new Child { ParentID = -100 })
						.Select(c => c.ParentID)
						.Sum()
				};

			var exptected = 
				from p in Parent
				select new
				{
					Sum = p.Children.DefaultIfEmpty(new Child { ParentID = -100 })
						.Select(c => c.ParentID)
						.Sum()
				};


			AreEqual(exptected, query);
		}

		[Test]
		public void WithDefaultInSelectMany([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);

			var query =
				from p in db.Parent.LoadWith(p => p.Children)
				from c in p.Children.DefaultIfEmpty(new Child { ChildID = -100 })
				select new { Parent = p.ParentID, Child = c }
				into s
				where s.Child.ChildID < 0
				select s;

			AssertQuery(query);
		}

	}
}
