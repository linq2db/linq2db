using NUnit.Framework;
using System;
using System.Linq;
using LinqToDB;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue1750Tests : TestBase
	{
		[Test]
		public void Issue1750Test(
			[IncludeDataSources(false, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context,
			[Values] bool includeX,
			[Values] bool includeY,
			[Values] bool includeZ)
		{
			using (var db = GetDataContext(context))
			{
				// Arrange
				var childrenIn = (int[]?)null;
				var grandChildIn = GrandChild.Select(x => x.ParentID).Distinct().ToArray();

				// Act
				var query = from p in db.Parent
					where
						(childrenIn == null || childrenIn.Contains(p.ParentID)) && (grandChildIn == null || grandChildIn.Contains(p.ParentID)) &&
						((includeX && p.Value1 == 1) || (includeY && p.Value1 == 2) || (includeZ && p.ParentID % 2 == 0))
					select p;

				var expected = (from p in Parent
					where
						(childrenIn == null || childrenIn.Contains(p.ParentID)) && (grandChildIn == null || grandChildIn.Contains(p.ParentID)) &&
						((includeX && p.Value1 == 1) || (includeY && p.Value1 == 2) || (includeZ && p.ParentID % 2 == 0))
					select p).ToArray();

				if (expected.Length == 0)
					Assert.That(query.Count(), Is.EqualTo(expected.Length));
				else
					AreEqual(expected, query);
			}
		}
	}
}
