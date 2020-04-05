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
			[IncludeDataSources(false, TestProvName.AllSQLite)] string context, 
			[Values(true, false)] bool includeX, 
			[Values(true, false)] bool includeY,
			[Values(true, false)] bool includeZ)
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
					Assert.AreEqual(expected.Length, query.Count());
				else
					AreEqual(expected, query);
			}
		}
	}
}
