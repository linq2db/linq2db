using System;
using System.Linq;

using LinqToDB.Data.DataProvider;

using NUnit.Framework;

namespace Tests.Linq
{
	using Model;

	[TestFixture]
	public class DistinctTest : TestBase
	{
		[Test]
		public void Distinct1()
		{
			ForEachProvider(db => AreEqual(
				(from ch in    Child select ch.ParentID).Distinct(),
				(from ch in db.Child select ch.ParentID).Distinct()));
		}

		[Test]
		public void Distinct2()
		{
			ForEachProvider(db => AreEqual(
				(from p in    Parent select p.Value1 ?? p.ParentID % 2).Distinct(),
				(from p in db.Parent select p.Value1 ?? p.ParentID % 2).Distinct()));
		}

		[Test]
		public void Distinct3()
		{
			ForEachProvider(db => AreEqual(
				(from p in    Parent select new { Value = p.Value1 ?? p.ParentID % 2, p.Value1 }).Distinct(),
				(from p in db.Parent select new { Value = p.Value1 ?? p.ParentID % 2, p.Value1 }).Distinct()));
		}

		[Test]
		public void Distinct4()
		{
			ForEachProvider(db => AreEqual(
				(from p in    Parent select new Parent { ParentID = p.Value1 ?? p.ParentID % 2, Value1 = p.Value1 }).Distinct(),
				(from p in db.Parent select new Parent { ParentID = p.Value1 ?? p.ParentID % 2, Value1 = p.Value1 }).Distinct()));
		}

		[Test]
		public void Distinct5()
		{
			var id = 2;

			ForEachProvider(db => AreEqual(
				(from p in    Parent select new Parent { ParentID = p.Value1 ?? p.ParentID % 2, Value1 = id + 1 }).Distinct(),
				(from p in db.Parent select new Parent { ParentID = p.Value1 ?? p.ParentID % 2, Value1 = id + 1 }).Distinct()));
		}

		[Test]
		public void Distinct6()
		{
			var id = 2;

			ForEachProvider(new[] { ProviderName.Informix }, db => AreEqual(
				(from p in    Parent select new Parent { ParentID = p.Value1 ?? p.ParentID + id % 2, Value1 = id + 1 }).Distinct(),
				(from p in db.Parent select new Parent { ParentID = p.Value1 ?? p.ParentID + id % 2, Value1 = id + 1 }).Distinct()));
		}

		[Test]
		public void DistinctCount()
		{
			var expected =
				from p in Parent
					join c in Child on p.ParentID equals c.ParentID
				where c.ChildID > 20
				select p;

			ForEachProvider(db =>
			{
				var result =
					from p in db.Parent
						join c in db.Child on p.ParentID equals c.ParentID
					where c.ChildID > 20
					select p;

				Assert.AreEqual(expected.Distinct().Count(), result.Distinct().Count());
			});
		}

		[Test]
		public void DistinctMax()
		{
			var expected =
				from p in Parent
					join c in Child on p.ParentID equals c.ParentID
				where c.ChildID > 20
				select p;

			ForEachProvider(db =>
			{
				var result =
					from p in db.Parent
						join c in db.Child on p.ParentID equals c.ParentID
					where c.ChildID > 20
					select p;

				Assert.AreEqual(expected.Distinct().Max(p => p.ParentID), result.Distinct().Max(p => p.ParentID));
			});
		}

		[Test]
		public void TakeDistinct()
		{
			ForEachProvider(new[] { ProviderName.Sybase, ProviderName.SQLite },
				db => AreEqual(
					(from ch in    Child orderby ch.ParentID select ch.ParentID).Take(4).Distinct(),
					(from ch in db.Child orderby ch.ParentID select ch.ParentID).Take(4).Distinct()));
		}
	}
}
