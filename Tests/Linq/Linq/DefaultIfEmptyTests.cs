using System.Linq;

using LinqToDB;
using LinqToDB.Internal.Common;

using NUnit.Framework;

using Shouldly;

using Tests.Model;

namespace Tests.Linq
{
	public class DefaultIfEmptyTests : TestBase
	{
		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSybase, ErrorMessage = ErrorHelper.Sybase.Error_JoinToDerivedTableWithTakeInvalid)]
		public void WithoutDefault([DataSources(TestProvName.AllAccess, TestProvName.AllClickHouse, TestProvName.AllInformix)] string context)
		{
			using var db = GetDataContext(context);

			var children = db.Child
				.Take(0)
				.DefaultIfEmpty()
				.ToList();

			children.Count.ShouldBe(1);
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSybase, ErrorMessage = ErrorHelper.Sybase.Error_JoinToDerivedTableWithTakeInvalid)]
		public void WithDefault([DataSources(TestProvName.AllAccess, TestProvName.AllClickHouse, TestProvName.AllInformix)] string context)
		{
			using var db = GetDataContext(context);

			var defaultValue = new Child{ Parent1 = new Parent1()};

			var children = db.Child
				.Take(0)
				.DefaultIfEmpty(defaultValue)
				.ToList();

			children.Count.ShouldBe(1);

			children[0].Parent1.ShouldBeSameAs(defaultValue.Parent1);
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllMySql57, TestProvName.AllOracle11, TestProvName.AllMariaDB, ErrorMessage = ErrorHelper.Error_OUTER_Joins)]
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

		[YdbTableNotFound]
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
