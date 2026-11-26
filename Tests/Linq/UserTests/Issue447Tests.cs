using System;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;

using NUnit.Framework;

using Shouldly;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue447Tests : TestBase
	{
		[Test]
		public void TestLinqToDBComplexQueryWithParameters([DataSources] string context)
		{
			var value = true;

			using var db = GetDataContext(context);
			var query = from p in db.Parent where p.ParentID > 2 && value && true && !false select p;

			var res = query.ToList();
		}
	}
}
