using System;
using System.Linq;

using LinqToDB;

using NUnit.Framework;

namespace Tests.Linq
{
	using Model;
	using VisualBasic;

	[TestFixture]
	public class VisualBasicTest : TestBase
	{
		[Test]
		public void CompareString()
		{
			ForEachProvider(db => AreEqual(
				from p in db.Person where p.FirstName == "John" select p,
				CompilerServices.CompareString(db)));
		}

		[Test]
		public void CompareString1()
		{
			ForEachProvider(db =>
			{
				var str = CompilerServices.CompareString(db).ToString();
				Assert.That(str.IndexOf("CASE"), Is.EqualTo(-1));
			});
		}

		[Test]
		public void ParameterName()
		{
			ForEachProvider(db => AreEqual(
				from p in Parent where p.ParentID == 1 select p,
				VisualBasicCommon.ParamenterName(db)));
		}

		[Test]
		public void SearchCondition1()
		{
			ForEachProvider(
				new[] { ProviderName.Access },
				db => AreEqual(
					from t in Types
					where !t.BoolValue && (t.SmallIntValue == 5 || t.SmallIntValue == 7 || (t.SmallIntValue | 2) == 10)
					select t,
					VisualBasicCommon.SearchCondition1(db)));
		}

		[Test]
		public void SearchCondition2()
		{
			using (var db = new NorthwindDB())
			{
				AreEqual(
					from cust in Customer
					where cust.Orders.Count > 0 && cust.CompanyName.StartsWith("H")
					select cust.CustomerID,
					VisualBasicCommon.SearchCondition2(db));
			}
		}
	}
}
