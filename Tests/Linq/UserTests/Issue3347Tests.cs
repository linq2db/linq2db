using System.Collections.Generic;
using System.Linq;
using LinqToDB;
using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue3347Tests
	{
		[Test]
		public void TestSqlLike()
		{
			var lst = new List<string>() {"aabbcc", "aaaaaa", "bbbbb", "ccccc", "***", "...", "aa%bb_cc"};

			Assert.AreEqual(3, lst.Count(x => Sql.Like(x, "%bb%")));
			Assert.AreEqual(3, lst.Count(x => Sql.Like(x, "aa%")));
			Assert.AreEqual(7, lst.Count(x => Sql.Like(x, "%")));
			Assert.AreEqual(3, lst.Count(x => Sql.Like(x, "%cc%")));
			Assert.AreEqual(1, lst.Count(x => Sql.Like(x, "...")));
			Assert.AreEqual(1, lst.Count(x => Sql.Like(x, "%.%")));
			Assert.AreEqual(0, lst.Count(x => Sql.Like(x, "%$%bb")));
			Assert.AreEqual(1, lst.Count(x => Sql.Like(x, "%$%bb", '$')));
		}
	}
}
