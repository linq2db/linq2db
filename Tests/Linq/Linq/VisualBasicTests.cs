using System;
using System.Linq;

using LinqToDB;

using NUnit.Framework;

#if !MONO

namespace Tests.Linq
{
	using Model;
	using VisualBasic;

	[TestFixture]
	public class VisualBasicTests : TestBase
	{
		[Test, DataContextSource]
		public void CompareString(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in db.Person where p.FirstName == "John" select p,
					CompilerServices.CompareString(db));
		}

		[Test, DataContextSource]
		public void CompareString1(string context)
		{
			using (var db = GetDataContext(context))
			{
				var str = CompilerServices.CompareString(db).ToString();
				Assert.That(str.IndexOf("CASE"), Is.EqualTo(-1));
			}
		}

		[Test, DataContextSource(ProviderName.SapHana)]
		public void ParameterName(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent where p.ParentID == 1 select p,
					VisualBasicCommon.ParamenterName(db));
		}

		[Test, DataContextSource(ProviderName.Access)]
		public void SearchCondition1(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in Types
					where !t.BoolValue && (t.SmallIntValue == 5 || t.SmallIntValue == 7 || (t.SmallIntValue | 2) == 10)
					select t,
					VisualBasicCommon.SearchCondition1(db));
		}

		[Test, NorthwindDataContext]
		public void SearchCondition2(string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var dd = GetNorthwindAsList(context);
				AreEqual(
					from cust in dd.Customer
					where cust.Orders.Count > 0 && cust.CompanyName.StartsWith("H")
					select cust.CustomerID,
					VisualBasicCommon.SearchCondition2(db));
			}
		}

		[Test, NorthwindDataContext]
		public void SearchCondition3(string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var cQuery =
					from order in db.Order
					where order.OrderDate == new DateTime(1997, 11, 14)
					select order.OrderID;

				var cSharpResults = cQuery.ToList();

				var vbResults = (VisualBasicCommon.SearchCondition3(db)).ToList();

				AreEqual(
					cSharpResults,
					vbResults);
			}
		}

		[Test, NorthwindDataContext]
		public void SearchCondition4(string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var cQuery =
					from order in db.Order
					where order.OrderDate == new DateTime(1997, 11, 14)
					select order.OrderID;

				var cSharpResults = cQuery.ToList();

				var vbResults = (VisualBasicCommon.SearchCondition4(db)).ToList();

				AreEqual(
					cSharpResults,
					vbResults);
			}
		}
	}
}

#endif
