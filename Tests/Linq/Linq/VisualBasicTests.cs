﻿using System;
using System.Linq;

using LinqToDB;

using NUnit.Framework;


namespace Tests.Linq
{
	using Model;
	using VisualBasic;

	[TestFixture]
	public class VisualBasicTests : TestBase
	{
		[Test]
		public void CompareString([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in db.Person where p.FirstName == "John" select p,
					CompilerServices.CompareString(db));
		}

		[Test]
		public void CompareString1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var str = CompilerServices.CompareString(db).ToString()!;
				Assert.That(str.IndexOf("CASE"), Is.EqualTo(-1));
			}
		}

		[Test]
		public void ParameterName([DataSources(TestProvName.AllSapHana)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent where p.ParentID == 1 select p,
					VisualBasicCommon.ParamenterName(db));
		}

		[Test]
		public void SearchCondition1([DataSources(TestProvName.AllAccess)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in Types
					where !t.BoolValue && (t.SmallIntValue == 5 || t.SmallIntValue == 7 || (t.SmallIntValue | 2) == 10)
					select t,
					VisualBasicCommon.SearchCondition1(db));
		}

		[Test]
		public void SearchCondition2([NorthwindDataContext] string context)
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

		[Test]
		public void SearchCondition3([NorthwindDataContext] string context)
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

		[Test]
		public void SearchCondition4([NorthwindDataContext] string context)
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

		[ActiveIssue(649)]
		[Test]
		public void Issue649Test1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<VBTests.Activity649>())
			using (db.CreateLocalTable<VBTests.Person649>())
			{
				var result = VBTests.Issue649Test1(db);
			}
		}

		[Test]
		public void Issue649Test2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<VBTests.Activity649>())
			using (db.CreateLocalTable<VBTests.Person649>())
			{
				var result = VBTests.Issue649Test2(db);
			}
		}

		[Test]
		public void Issue649Test3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<VBTests.Activity649>())
			using (db.CreateLocalTable<VBTests.Person649>())
			{
				var result = VBTests.Issue649Test3(db);
			}
		}

		[ActiveIssue(649)]
		[Test]
		public void Issue649Test4([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.InlineParameters = true;
				var q1 = db.Child.GroupBy(c => new
				{
					c.ParentID,
					c.ChildID
				}, (c, g) => new
				{
					Child = c,
					Grouped = g
				}).Select(data => new
				{
					ParentID  = data.Child.ParentID,
					ChildID   = data.Child.ChildID,
					LastChild = data.Grouped.Max(f => f.ChildID)
				});

				var str = q1.ToString();
			}
		}
		}
}
