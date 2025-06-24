//------------------------------------------------------------------------------
//     Copyright (C) 2009-2010 ORMBattle.NET.
//     All rights reserved.
//     For conditions of distribution and use, see license.
//     Created by: Alexis Kochetov
//     Created:    2009.07.31
//     Updated by: Svyatoslav Danyliv
//     Updated:    2015.12.14
//
//     This file is generated from LinqTests.tt
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using LinqToDB;

using NUnit.Framework;

using Tests.Model;
using Tests.OrmBattle.Helper;

using static Tests.Model.Northwind;

namespace Tests.OrmBattle
{
	[TestFixture]
	public sealed class OrmBattleTests : TestBase
	{
		private const double doubleDelta = 1E-9;

		List<Northwind.Customer>? Customers;
		List<Northwind.Employee>? Employees;
		List<Northwind.Order>?    Order;
		List<Northwind.Product>?  Products;

		[MemberNotNull(nameof(Customers), nameof(Employees), nameof(Order), nameof(Products))]
		private NorthwindDB Setup(string context, bool guardGrouping = true)
		{
			using (new DisableLogging())
			{
				var db = new NorthwindDB(new DataOptions().UseConfiguration(context).UseGuardGrouping(guardGrouping));

				Customers ??= db.Customer.ToList();
				Employees ??= db.Employee.ToList();
				Products ??= db.Product.ToList();

				if (Order == null)
				{
					Order = db.Order.ToList();

					foreach (var o in Order)
					{
						o.Customer = Customers.SingleOrDefault(c => c.CustomerID == o.CustomerID);
						o.Employee = Employees.SingleOrDefault(e => e.EmployeeID == o.EmployeeID);
					}

					foreach (var c in Customers)
					{
						c.Orders = Order.Where(o => c.CustomerID == o.CustomerID).ToList();
					}
				}

				return db;
			}
		}

		// DTO for testing purposes.
		public class OrderDTO
		{
			public int Id { get; set; }
			public string? CustomerId { get; set; }
			public DateTime? OrderDate { get; set; }
		}

		#region Filtering tests

		[Test]
		public void WhereTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var result = from o in db.Order
				where o.ShipCity == "Seattle"
				select o;
			var expected = from o in Order
				where o.ShipCity == "Seattle"
				select o;
			var list = result.ToList();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(list, Has.Count.EqualTo(14));
				Assert.That(expected.Except(list), Is.Empty);
			}
		}

		[Test]
		public void WhereParameterTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var city = "Seattle";
			var result = from o in db.Order
				where o.ShipCity == city
				select o;
			var expected = from o in Order
				where o.ShipCity == city
				select o;
			var list = result.ToList();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(list, Has.Count.EqualTo(14));
				Assert.That(expected.Except(list), Is.Empty);
			}

			city = "Rio de Janeiro";
			list = result.ToList();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(list, Has.Count.EqualTo(34));
				Assert.That(expected.Except(list), Is.Empty);
			}
		}

		[Test]
		public void WhereConditionsTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var result = from p in db.Product
				where p.UnitsInStock < p.ReorderLevel && p.UnitsOnOrder == 0
				select p;
			var list = result.ToList();
			Assert.That(list, Has.Count.EqualTo(1));
		}

		[Test]
		public void WhereNullTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var result = from o in db.Order
				where o.ShipRegion == null
				select o;
			var list = result.ToList();
			Assert.That(list, Has.Count.EqualTo(507));
		}

		[Test]
		public void WhereNullParameterTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			string? region = null;
			var result = from o in db.Order
				where o.ShipRegion == region
				select o;
			var list = result.ToList();
			Assert.That(list, Has.Count.EqualTo(507));

			region = "WA";
			list = result.ToList();
			Assert.That(list, Has.Count.EqualTo(19));
		}

		[Test]
		public void WhereNullableTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var result = from o in db.Order
				where !o.ShippedDate.HasValue
				select o;
			var list = result.ToList();
			Assert.That(list, Has.Count.EqualTo(21));
		}

		[Test]
		public void WhereNullableParameterTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			DateTime? shippedDate = null;
			var result = from o in db.Order
				where o.ShippedDate == shippedDate
				select o;
			var list = result.ToList();
			Assert.That(list, Has.Count.EqualTo(21));
		}

		[Test]
		public void WhereCoalesceTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var result = from o in db.Order
				where (o.ShipRegion ?? "N/A") == "N/A"
				select o;
			var list = result.ToList();
			Assert.That(list, Has.Count.EqualTo(507));
		}

		[Test]
		public void WhereConditionalTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var result = from o in db.Order
				where (o.ShipCity == "Seattle" ? "Home" : "Other") == "Home"
				select o;
			var list = result.ToList();
			Assert.That(list, Has.Count.EqualTo(14));
		}

		[Test]
		public void WhereConditionalBooleanTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var result = from o in db.Order
				where o.ShipCity == "Seattle" ? true : false
				select o;
			var list = result.ToList();
			Assert.That(list, Has.Count.EqualTo(14));
		}

		[Test]
		public void WhereAnonymousParameterTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var cityRegion = new {City = "Seattle", Region = "WA"};
			var result = from o in db.Order
				where new {City = o.ShipCity, Region = o.ShipRegion} == cityRegion
				select o;
			var list = result.ToList();
			Assert.That(list, Has.Count.EqualTo(14));
		}

		[Test]
		public void WhereEntityParameterTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var order = db.Order.OrderBy(o => o.OrderDate).First();
			var result = from o in db.Order
				where o == order
				select o;
			var list = result.ToList();
			Assert.That(list, Has.Count.EqualTo(1));
			Assert.That(list[0], Is.EqualTo(order));
			//Assert.AreSame(order, list[0]);
		}

		#endregion

		#region Projection tests

		[Test]
		public void SelectTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var result = from o in db.Order
				select o.ShipRegion;
			var expected = from o in Order
				select o.ShipRegion;
			var list = result.ToList();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(list, Has.Count.EqualTo(expected.Count()));
				Assert.That(expected.Except(list), Is.Empty);
			}
		}

		[Test]
		public void SelectBooleanTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var result = from o in db.Order
				select o.ShipRegion == "WA";
			var expected = from o in Order
				select o.ShipRegion == "WA";
			var list = result.ToList();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(list, Has.Count.EqualTo(expected.Count()));
				Assert.That(expected.Except(list), Is.Empty);
			}
		}

		[Test]
		public void SelectCalculatedTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var result = from o in db.Order
				select o.Freight * 1000;
			var expected = from o in Order
				select o.Freight * 1000;
			var list = result.ToList();
			var expectedList = expected.ToList();
			list.Sort();
			expectedList.Sort();

			// Assert.AreEqual(expectedList.Count, list.Count);
			// expectedList.Zip(list, (i, j) => {
			//                       Assert.AreEqual(i,j);
			//                       return true;
			//                     });
			Assert.That(list, Is.EquivalentTo(expectedList));
		}

		[Test]
		public void SelectNestedCalculatedTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var result = from r in
				from o in db.Order
				select o.Freight * 1000
				where r > 100000
				select r / 1000;
			var expected = from o in Order
				where o.Freight > 100
				select o.Freight;
			var list = result.ToList();
			var expectedList = expected.ToList();
			list.Sort();
			expectedList.Sort();
			Assert.That(list, Has.Count.EqualTo(187));
			// Assert.AreEqual(expectedList.Count, list.Count);
			// expectedList.Zip(list, (i, j) => {
			//                       Assert.AreEqual(i,j);
			//                       return true;
			//                     });
			Assert.That(list, Is.EquivalentTo(expectedList));
		}

		[Test]
		public void SelectAnonymousTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var result = from o in db.Order
				select new {OrderID = o.OrderID, o.OrderDate, o.Freight};
			var expected = from o in Order
				select new {OrderID = o.OrderID, o.OrderDate, o.Freight};
			var list = result.ToList();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(list, Has.Count.EqualTo(expected.Count()));
				Assert.That(expected.Except(list).Count(), Is.Zero);
			}
		}

		[Test]
		public void SelectSubqueryTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			Assert.That(db.GetType().FullName, Is.Not.EqualTo("OrmBattle.EF7Model.NorthwindContext"),
				"EF7 has infinite loop here");

			var result = from o in db.Order
				select db.Customer.Where(c => c.CustomerID == o.Customer!.CustomerID);
			var expected = from o in Order
				select Customers.Where(c => c.CustomerID == o.Customer!.CustomerID);
			var list = result.ToList();

			var expectedList = expected.ToList();
			Assert.That(list, Is.EquivalentTo(expectedList));

			//Assert.AreEqual(expected.Count(), list.Count);
			//expected.Zip(result, (expectedCustomers, actualCustomers) => {
			//                       Assert.AreEqual(expectedCustomers.Count(), actualCustomers.Count());
			//                       Assert.AreEqual(0, expectedCustomers.Except(actualCustomers));
			//                       return true;
			//                     });
		}

		[Test]
		public void SelectDtoTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var result = from o in db.Order
				select new OrderDTO {Id = o.OrderID, CustomerId = o.Customer!.CustomerID, OrderDate = o.OrderDate};
			var list = result.ToList();
			Assert.That(list, Has.Count.EqualTo(Order.Count()));
		}

		[Test]
		public void SelectNestedDtoTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var result = from r in
				from o in db.Order
				select new OrderDTO {Id = o.OrderID, CustomerId = o.Customer!.CustomerID, OrderDate = o.OrderDate}
				where r.OrderDate > new DateTime(1998, 01, 01)
				select r;
			var list = result.ToList();
			Assert.That(list, Has.Count.EqualTo(267));
		}

		[Test]
		public void SelectManyAnonymousTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var result = from c in db.Customer
				from o in c.Orders
				where o.Freight < 500.00M
				select new {CustomerId = c.CustomerID, o.OrderID, o.Freight};
			var list = result.ToList();
			Assert.That(list, Has.Count.EqualTo(817));
		}

		[Test]
		public void SelectManyLetTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var result = from c in db.Customer
				from o in c.Orders
				let freight = o.Freight
				where freight < 500.00M
				select new {CustomerId = c.CustomerID, o.OrderID, freight};
			var list = result.ToList();
			Assert.That(list, Has.Count.EqualTo(817));
		}

		[Test]
		public void SelectManyGroupByTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var result = db.Order
				.GroupBy(o => o.Customer)
				.Where(g => g.Count() > 20)
				.SelectMany(g => g.Select(o => o.Customer));

			var list = result.ToList();
			Assert.That(list, Has.Count.EqualTo(89));
		}

		[Test]
		public void SelectManyOuterProjectionTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var result = db.Customer.SelectMany(i => i.Orders.Select(t => i));

			var list = result.ToList();
			Assert.That(list, Has.Count.EqualTo(830));
		}

		[Test]
		public void SelectManyLeftJoinTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var result =
				from c in db.Customer
				from o in c.Orders.Select(o => new {o.OrderID, c.CompanyName}).DefaultIfEmpty()
				select new {c.ContactName, o};

			var list = result.ToList();
			Assert.That(list, Is.Not.Empty);
		}

		#endregion

		#region Take / Skip tests

		[Test]
		public void TakeTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var result = (from o in db.Order
				orderby o.OrderDate, o.OrderID
				select o).Take(10);
			var expected = (from o in Order
				orderby o.OrderDate, o.OrderID
				select o).Take(10);
			var list = result.ToList();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(list, Has.Count.EqualTo(10));
				Assert.That(expected.SequenceEqual(list), Is.True);
			}
		}

		[Test]
		public void SkipTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var result = (from o in db.Order
				orderby o.OrderDate, o.OrderID
				select o).Skip(10);
			var expected = (from o in Order
				orderby o.OrderDate, o.OrderID
				select o).Skip(10);
			var list = result.ToList();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(list, Has.Count.EqualTo(820));
				Assert.That(expected.SequenceEqual(list), Is.True);
			}
		}

		[Test]
		public void TakeSkipTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var result = (from o in db.Order
				orderby o.OrderDate, o.OrderID
				select o).Skip(10).Take(10);
			var expected = (from o in Order
				orderby o.OrderDate, o.OrderID
				select o).Skip(10).Take(10);
			var list = result.ToList();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(list, Has.Count.EqualTo(10));
				Assert.That(expected.SequenceEqual(list), Is.True);
			}
		}

		[Test]
		public void TakeNestedTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var result =
				from c in db.Customer
				select new {Customer = c, TopOrder = c.Orders.OrderByDescending(o => o.OrderDate).Take(5)};
			var expected =
				from c in Customers
				select new {Customer = c, TopOrder = c.Orders.OrderByDescending(o => o.OrderDate).Take(5)};
			var list = result.ToList();
			Assert.That(list, Has.Count.EqualTo(expected.Count()));
			foreach (var anonymous in list)
			{
				var count = anonymous.TopOrder.ToList().Count;
				Assert.That(count, Is.GreaterThanOrEqualTo(0));
				Assert.That(count, Is.LessThanOrEqualTo(5));
			}
		}

		[Test]
		public void ComplexTakeSkipTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var original = Order.ToList()
				.OrderBy(o => o.OrderDate)
				.Skip(100)
				.Take(50)
				.OrderBy(o => o.RequiredDate)
				.Where(o => o.OrderDate != null)
				.Select(o => o.RequiredDate)
				.Distinct()
				.OrderByDescending(_ => _)
				.Skip(10);
			var result = db.Order
				.OrderBy(o => o.OrderDate)
				.Skip(100)
				.Take(50)
				.OrderBy(o => o.RequiredDate)
				.Where(o => o.OrderDate != null)
				.Select(o => o.RequiredDate)
				.Distinct()
				.OrderByDescending(_ => _)
				.Skip(10);
			var originalList = original.ToList();
			var resultList = result.ToList();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(resultList, Has.Count.EqualTo(originalList.Count));
				Assert.That(originalList.SequenceEqual(resultList), Is.True);
			}
		}

		#endregion

		#region Ordering tests

		[Test]
		public void OrderByTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var result =
				from o in db.Order
				orderby o.OrderDate, o.ShippedDate descending, o.OrderID
				select o;
			var expected =
				from o in Order
				orderby o.OrderDate, o.ShippedDate descending, o.OrderID
				select o;

			var list = result.ToList();
			var expectedList = expected.ToList();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(list, Has.Count.EqualTo(expectedList.Count));
				Assert.That(expected.SequenceEqual(list), Is.True);
			}
		}

		[Test]
		public void OrderByWhereTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var result = (from o in db.Order
				orderby o.OrderDate, o.OrderID
				where o.OrderDate > new DateTime(1997, 1, 1)
				select o).Take(10);
			var expected = (from o in Order
				where o.OrderDate > new DateTime(1997, 1, 1)
				orderby o.OrderDate, o.OrderID
				select o).Take(10);
			var list = result.ToList();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(list, Has.Count.EqualTo(10));
				Assert.That(expected.SequenceEqual(list), Is.True);
			}
		}

		[Test]
		public void OrderByCalculatedColumnTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var result =
				from o in db.Order
				orderby o.Freight * o.OrderID descending
				select o;
			var expected =
				from o in Order
				orderby o.Freight * o.OrderID descending
				select o;
			Assert.That(expected.SequenceEqual(result), Is.True);
		}

		[Test]
		public void OrderByEntityTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var result =
				from o in db.Order
				orderby o
				select o;
			var expected =
				from o in Order
				orderby o.OrderID
				select o;
			Assert.That(expected.SequenceEqual(result, new GenericEqualityComparer<Order>(o => o.OrderID)), Is.True);
		}

		[Test]
		public void OrderByAnonymousTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var result =
				from o in db.Order
				orderby new {o.OrderDate, o.ShippedDate, o.OrderID}
				select o;
			var expected =
				from o in Order
				orderby o.OrderDate, o.ShippedDate, o.OrderID
				select o;
			Assert.That(expected.SequenceEqual(result, new GenericEqualityComparer<Order>(o => o.OrderID)), Is.True);
		}

		[Test]
		[ActiveIssue("Bad database data", Configuration = TestProvName.AllSQLiteNorthwind)]
		public void OrderByDistinctTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var result = db.Customer
				.OrderBy(c => c.CompanyName)
				.Select(c => c.City)
				.Distinct()
				.OrderBy(c => c)
				.Select(c => c);
			var expected = Customers
				.OrderBy(c => c.CompanyName)
				.Select(c => c.City)
				.Distinct()
				.OrderBy(c => c)
				.Select(c => c);
			Assert.That(expected.SequenceEqual(result), Is.True);
		}

		[Test]
		public void OrderBySelectManyTest([NorthwindDataContext(true)] string context)
		{
			using var db = Setup(context);
			var result =
				from c in db.Customer.OrderBy(c => c.ContactName)
				from o in db.Order.OrderBy(o => o.OrderDate)
				where c == o.Customer
				select new {c.ContactName, o.OrderDate};
			var expected =
				from c in Customers.OrderBy(c => c.ContactName)
				from o in Order.OrderBy(o => o.OrderDate)
				where c == o.Customer
				select new {c.ContactName, o.OrderDate};
			Assert.That(expected.SequenceEqual(result), Is.True);
		}

		[Test]
		public void OrderByPredicateTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var result =
				db.Order.OrderBy(o => o.Freight > 0 && o.ShippedDate != null).ThenBy(o => o.OrderID).Select(o => o.OrderID);
			var list = result.ToList();
			var original =
				Order.OrderBy(o => o.Freight > 0 && o.ShippedDate != null).ThenBy(o => o.OrderID).Select(o => o.OrderID).ToList();
			Assert.That(list.SequenceEqual(original), Is.True);
		}

		#endregion

		#region Grouping tests

		[Test]
		public void GroupByTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context, false);
			var result = from o in db.Order
				group o by o.OrderDate;
			var list = result.ToList();
			Assert.That(list, Has.Count.EqualTo(480));
		}

		[Test]
		public void GroupByReferenceTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context, false);
			var result = from o in db.Order
				group o by o.Customer;
			var list = result.ToList();
			Assert.That(list, Has.Count.EqualTo(89));
		}

		[Test]
		public void GroupByWhereTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context, false);
			var result =
				from o in db.Order
				group o by o.OrderDate
				into g
				where g.Count() > 5
				select g;
			var list = result.ToList();
			Assert.That(list, Has.Count.EqualTo(1));
		}

		[Test]
		public void GroupByTestAnonymous([NorthwindDataContext] string context)
		{
			using var db = Setup(context, false);
			var result = from c in db.Customer
				group c by new { c.Region, c.City };
			var list = result.ToList();
			Assert.That(list, Has.Count.EqualTo(69));
		}

		[Test]
		public void GroupByCalculatedTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context, false);

			var result =
				from o in db.Order
				group o by o.Freight > 50 ? o.Freight > 100 ? "expensive" : "average" : "cheap"
				into g
				select g;

			var list = result.ToList();

			Assert.That(list, Has.Count.EqualTo(3));
		}

		[Test]
		public void GroupBySelectManyTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var result = db.Customer
				.GroupBy(c => c.City)
				.SelectMany(g => g);

			var list = result.ToList();
			Assert.That(list, Has.Count.EqualTo(91));
		}

		[Test]
		public void GroupByCalculateAggregateTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var result =
				from o in db.Order
				group o by o.Customer
				into g
				select g.Sum(o => o.Freight);

			var list = result.ToList();
			Assert.That(list, Has.Count.EqualTo(89));
		}

		[Test]
		public void GroupByCalculateManyAggreagetes([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var result =
				from o in db.Order
				group o by o.Customer
				into g
				select new
				{
					Sum = g.Sum(o => o.Freight),
					Min = g.Min(o => o.Freight),
					Max = g.Max(o => o.Freight),
					Avg = g.Average(o => o.Freight)
				};

			var list = result.ToList();
			Assert.That(list, Has.Count.EqualTo(89));
		}

		[Test]
		public void GroupByAggregate([NorthwindDataContext] string context)
		{
			using var db = Setup(context,false);

			var result =
				from c in db.Customer
				group c by c.Orders.Average(o => o.Freight) >= 80;

			var list = result.ToList();
			Assert.That(list, Has.Count.EqualTo(2));
			var firstGroupList = list.First(g => !g.Key).ToList();
			Assert.That(firstGroupList, Has.Count.EqualTo(71));
		}

		[Test]
		public void ComplexGroupingTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context, false);

			var result =
				from c in db.Customer
				select new
				{
					c.CompanyName,
					YearGroups =
					from o in c.Orders
					group o by o.OrderDate!.Value.Year
					into yg
					select new
					{
						Year = yg.Key,
						MonthGroups =
						from order in yg
						group order by order.OrderDate!.Value.Month
						into mg
						select new {Month = mg.Key, Order = mg}
					}
				};
			var list = result.ToList();
			foreach (var customer in list)
			{
				var OrderList = customer.YearGroups.ToList();
				Assert.That(OrderList, Has.Count.LessThanOrEqualTo(3));
			}
		}

		#endregion

		#region Set operations / Distinct tests

		[Test]
		public void ConcatTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var result = db.Customer.Where(c => c.Orders.Count <= 1)
				.Concat(db.Customer.Where(c => c.Orders.Count > 1));
			var list = result.ToList();
			Assert.That(list, Has.Count.EqualTo(91));
		}

		[Test]
		public void UnionTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var result = (
					from c in db.Customer
					select c.Phone)
				.Union(
					from c in db.Customer
					select c.Fax)
				.Union(
					from e in db.Employee
					select e.HomePhone
				);

			var list = result.ToList();
			Assert.That(list, Has.Count.EqualTo(167));
		}

		[Test]
		public void ExceptTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var result =
				db.Customer.Except(db.Customer.Where(c => c.Orders.Count() > 0));
			var list = result.ToList();
			Assert.That(list, Has.Count.EqualTo(2));
		}

		[Test]
		public void IntersectTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var result =
				db.Customer.Intersect(db.Customer.Where(c => c.Orders.Count() > 0));
			var list = result.ToList();
			Assert.That(list, Has.Count.EqualTo(89));
		}

		[Test]
		public void DistinctTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var result = db.Order.Select(c => c.Freight).Distinct();
			var list = result.ToList();
			Assert.That(list, Has.Count.EqualTo(799));
		}

		[Test]
		public void DistinctTakeLastTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var result =
			(from o in db.Order
				orderby o.OrderDate
				select o.OrderDate).Distinct().Take(5);
			var list = result.ToList();
			Assert.That(list, Has.Count.EqualTo(5));
		}

		[Test]
		public void DistinctTakeFirstTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var result =
			(from o in db.Order
				orderby o.OrderDate
				select o.OrderDate).Take(5).Distinct();
			var list = result.ToList();
			Assert.That(list, Has.Count.EqualTo(4));
		}

		[Test]
		public void DistinctEntityTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var result = db.Customer.Distinct();
			var list = result.ToList();
			Assert.That(list, Has.Count.EqualTo(91));
		}

		[Test]
		public void DistinctAnonymousTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var result = db.Customer.Select(c => new {c.Region, c.City}).Distinct();
			var list = result.ToList();
			Assert.That(list, Has.Count.EqualTo(69));
		}

		#endregion

		#region Type casts

		[Test]
		public void TypeCastIsChildTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var result = db.Product.Where(p => p is DiscontinuedProduct);
			var expected = Products.Where(p => p is DiscontinuedProduct);
			var list = result.ToList();
			Assert.That(list, Is.Not.Empty);
			Assert.That(list, Has.Count.EqualTo(expected.Count()));
		}

		[Test]
		public void TypeCastIsParentTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var result = db.Product.Where(p => p is Product);
			var expected = Products.ToList();
			var list = result.ToList();
			Assert.That(list, Is.Not.Empty);
			Assert.That(list, Has.Count.EqualTo(expected.Count()));
		}

		[Test, ActiveIssue(573)]
		public void TypeCastIsChildConditionalTest([NorthwindDataContext] string context)
		{
			//TODO: sdanyliv: strange test for me
			using var db = Setup(context);
			var result = db.Product
				.Select(x => x is DiscontinuedProduct
					? x
					: null);

			var expected = Products
				.Select(x => x is DiscontinuedProduct
					? x
					: null);

			var list = result.ToList();
			Assert.That(list, Is.Not.Empty);
			Assert.That(list, Has.Count.EqualTo(expected.Count()));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(list.Except(expected).Count(), Is.Zero);
				Assert.That(list, Does.Contain(null));
			}
		}

		[Test]
		public void TypeCastOfTypeTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var result = db.Product.OfType<DiscontinuedProduct>();
			var expected = Products.OfType<DiscontinuedProduct>();
			var list = result.ToList();
			Assert.That(list, Is.Not.Empty);
			Assert.That(list, Has.Count.EqualTo(expected.Count()));
		}

		[Test]
		public void TypeCastAsTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var result = db.DiscontinuedProduct
				.Select(discontinuedProduct => discontinuedProduct as Product)
				.Select(product =>
					product == null
						? "NULL"
						: product.ProductName);

			var expected = db.DiscontinuedProduct.ToList()
				.Select(discontinuedProduct => discontinuedProduct as Product)
				.Select(product =>
					product == null
						? "NULL"
						: product.ProductName);

			var list = result.ToList();
			Assert.That(list, Is.Not.Empty);
			Assert.That(list, Has.Count.EqualTo(expected.Count()));
			Assert.That(list.Except(expected).Count(), Is.Zero);
		}

		#endregion

		#region Element operations

		[Test]
		public void FirstTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var customer = db.Customer.First();
			Assert.That(customer, Is.Not.Null);
		}

		[Test]
		public void FirstOrDefaultTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var customer = db.Customer.Where(c => c.CustomerID == "ALFKI").FirstOrDefault();
			Assert.That(customer, Is.Not.Null);
		}

		[Test]
		public void FirstPredicateTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var customer = db.Customer.First(c => c.CustomerID == "ALFKI");
			Assert.That(customer, Is.Not.Null);
		}

		[Test]
		public void NestedFirstOrDefaultTest([IncludeDataSources(TestProvName.AllNorthwind)] string context)
		{
			using var db = Setup(context);
			var result =
				from p in db.Product
				select new
				{
					ProductID = p.ProductID,
					MaxOrder = db.OrderDetail
						.Where(od => od.Product == p)
						.OrderByDescending(od => od.UnitPrice * od.Quantity)
						.FirstOrDefault()!
						.Order
				};
			var list = result.ToList();
			Assert.That(list, Is.Not.Empty);
		}

		[Test]
		public void FirstOrDefaultEntitySetTest([IncludeDataSources(TestProvName.AllNorthwind)] string context)
		{
			using var db = Setup(context);
			var customersCount = Customers.Count;
			var result = db.Customer.Select(c => c.Orders.FirstOrDefault());
			var list = result.ToList();
			Assert.That(list, Has.Count.EqualTo(customersCount));
		}

		[Test]
		public void NestedSingleOrDefaultTest([IncludeDataSources(TestProvName.AllNorthwind)] string context)
		{
			using var db = Setup(context);
			var customersCount = Customers.Count;
			var result = db.Customer.Select(c => c.Orders.Take(1).SingleOrDefault());
			var list = result.ToList();
			Assert.That(list, Has.Count.EqualTo(customersCount));
		}

		[Test]
		public void NestedSingleTest([IncludeDataSources(TestProvName.AllNorthwind)] string context)
		{
			using var db = Setup(context);
			var result = db.Customer.Where(c => c.Orders.Count() > 0).Select(c => c.Orders.Take(1).Single());
			var list = result.ToList();
			Assert.That(list, Is.Not.Empty);
		}

		[Test]
		public void ElementAtTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var customer = db.Customer.OrderBy(c => c.CustomerID).ElementAt(15);
			Assert.That(customer, Is.Not.Null);
			Assert.That(customer.CustomerID, Is.EqualTo("CONSH"));
		}

		[Test]
		public void NestedElementAtTest([IncludeDataSources(TestProvName.AllNorthwind)] string context)
		{
			using var db = Setup(context);
			var result =
				from c in db.Customer
				where c.Orders.Count() > 5
				select c.Orders.ElementAt(3);

			var list = result.ToList();
			Assert.That(list, Has.Count.EqualTo(63));
		}

		#endregion

		#region Contains / Any / All tests

		[Test]
		public void AllNestedTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var result =
				from c in db.Customer
				where
				db.Order.Where(o => o.Customer == c)
					.All(o => db.Employee.Where(e => o.Employee == e).Any(e => e.FirstName.StartsWith("A")))
				select c;
			var list = result.ToList();
			Assert.That(list, Has.Count.EqualTo(2));
		}

		[Test]
		public void ComplexAllTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var result =
				(from o in db.Order
				where
				db.Customer.Where(c => c == o.Customer).All(c => c.CompanyName.StartsWith("A")) ||
				db.Employee.Where(e => e == o.Employee).All(e => e.FirstName.EndsWith("t"))
				select o).ToList();
			var expected =
				from o in Order
				where
				Customers.Where(c => c == o.Customer).All(c => c.CompanyName.StartsWith("A")) ||
				Employees.Where(e => e == o.Employee).All(e => e.FirstName.EndsWith("t"))
				select o;
			using (Assert.EnterMultipleScope())
			{
				Assert.That(expected.Except(result).Count(), Is.Zero);
				Assert.That(result.ToList(), Has.Count.EqualTo(366));
			}
		}

		[Test]
		public void ContainsNestedTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var result = from c in db.Customer
				select new
				{
					Customer = c,
					HasNewOrder = db.Order
						.Where(o => o.OrderDate > new DateTime(2001, 1, 1))
						.Select(o => o.Customer)
						.Contains(c)
				};

			var resultList = result.ToList();

			var expected =
				from c in Customers
				select new
				{
					Customer = c,
					HasNewOrder = Order
						.Where(o => o.OrderDate > new DateTime(2001, 1, 1))
						.Select(o => o.Customer)
						.Contains(c)
				};
			using (Assert.EnterMultipleScope())
			{
				Assert.That(expected.Except(resultList).Count(), Is.Zero);
				Assert.That(resultList.Count(i => i.HasNewOrder), Is.Zero);
			}
		}

		[Test]
		public void AnyTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var result = db.Customer.Where(c => c.Orders.Any(o => o.Freight > 400)).ToList();
			var expected = Customers.Where(c => c.Orders.Any(o => o.Freight > 400));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(expected.Except(result).Count(), Is.Zero);
				Assert.That(result.ToList(), Has.Count.EqualTo(10));
			}
		}

		[Test]
		public void AnyParameterizedTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var ids = new[] {"ABCDE", "ALFKI"};
			var result = db.Customer.Where(c => ids.Any(id => c.CustomerID == id));
			var list = result.ToList();
			Assert.That(list, Is.Not.Empty);
		}

		[Test]
		public void ContainsParameterizedTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var customerIDs = new[] {"ALFKI", "ANATR", "AROUT", "BERGS"};
			var result = db.Order.Where(o => customerIDs.Contains(o.Customer!.CustomerID));
			var list = result.ToList();
			Assert.That(list, Is.Not.Empty);
			Assert.That(list, Has.Count.EqualTo(41));
		}

		#endregion

		#region Aggregates tests

		[Test]
		public void SumTest([NorthwindDataContext(false, true)] string context)
		{
			using var db = Setup(context);
			var sum = db.Order.Select(o => o.Freight).Sum();
			var sum1 = Order.Select(o => o.Freight).Sum();
			Assert.That((double)sum, Is.EqualTo((double)sum1).Within(doubleDelta));
		}

		[Test]
		public void CountPredicateTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var count = db.Order.Count(o => o.OrderID > 10);
			var count1 = Order.Count(o => o.OrderID > 10);
			Assert.That(count, Is.EqualTo(count1));
		}

		[Test]
		public void NestedCountTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var result = db.Customer.Where(c => db.Order.Count(o => o.Customer!.CustomerID == c.CustomerID) > 5);
			var expected = Customers.Where(c => Order.Count(o => o.Customer!.CustomerID == c.CustomerID) > 5);

			Assert.That(expected.Except(result).Count(), Is.Zero);
		}

		[Test]
		public void NullableSumTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var sum = db.Order.Select(o => (int?) o.OrderID).Sum();
			var sum1 = Order.Select(o => (int?) o.OrderID).Sum();
			Assert.That(sum, Is.EqualTo(sum1));
		}

		[Test]
		public void MaxCountTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var max = db.Customer.Max(c => db.Order.Count(o => o.Customer!.CustomerID == c.CustomerID));
			var max1 = Customers.Max(c => Order.Count(o => o.Customer!.CustomerID == c.CustomerID));
			Assert.That(max, Is.EqualTo(max1));
		}

		#endregion

		#region Join tests

		[Test]
		public void GroupJoinTest([NorthwindDataContext] string context)
		{
			//TODO: sdanyliv: o.Customer.CustomerID - it is association that means additional JOIN. We have to decide if it is a bug.
			using var db = Setup(context);
			var result =
				from c in db.Customer
				join o in db.Order on c.CustomerID equals o.Customer!.CustomerID into go
				join e in db.Employee on c.City equals e.City into ge
				select new
				{
					OrderCount = go.Count(),
					EmployeesCount = ge.Count()
				};
			var list = result.ToList();
			Assert.That(list, Has.Count.EqualTo(91));
		}

		[Test]
		public void JoinTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var result =
				from p in db.Product
				join s in db.Supplier on p.Supplier!.SupplierID equals s.SupplierID
				select new {p.ProductName, s.ContactName, s.Phone};

			var list = result.ToList();
			Assert.That(list, Is.Not.Empty);
		}

		[Test]
		public void JoinByAnonymousTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var result =
				from c in db.Customer
				join o in db.Order on new {Customer = c, Name = c.ContactName} equals
				new {o.Customer, Name = o.Customer!.ContactName}
				select new {c.ContactName, o.OrderDate};

			var list = result.ToList();
			Assert.That(list, Is.Not.Empty);
		}

		[Test]
		public void LeftJoinTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var result =
				from c in db.Category
				join p in db.Product on c.CategoryID equals p.Category!.CategoryID into g
				from p in g.DefaultIfEmpty()
				select new {Name = p == null ? "Nothing!" : p.ProductName, c.CategoryName};

			var list = result.ToList();
			Assert.That(list, Has.Count.EqualTo(77));
		}

		#endregion

		#region References tests

		[Test]
		public void JoinByReferenceTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var result =
				from c in db.Customer
				join o in db.Order on c equals o.Customer
				select new {c.ContactName, o.OrderDate};

			var list = result.ToList();
			Assert.That(list, Has.Count.EqualTo(830));
		}

		[Test]
		public void CompareReferenceTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var result =
				from c in db.Customer
				from o in db.Order
				where c == o.Customer
				select new {c.ContactName, o.OrderDate};

			var list = result.ToList();
			Assert.That(list, Has.Count.EqualTo(830));
		}

		[Test]
		public void ReferenceNavigationTestTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var result =
				from od in db.OrderDetail
				where od.Product.Category!.CategoryName == "Seafood"
				select new {od.Order, od.Product};

			var list = result.ToList();
			Assert.That(list, Has.Count.EqualTo(330));
			foreach (var anonymous in list)
			{
				Assert.That(anonymous, Is.Not.Null);
				using (Assert.EnterMultipleScope())
				{
					Assert.That(anonymous.Order, Is.Not.Null);
					Assert.That(anonymous.Product, Is.Not.Null);
				}
			}
		}

		[Test]
		public void EntitySetCountTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var result = db.Category.Where(c => c.Products.Count > 10);
			var list = result.ToList();
			Assert.That(list, Has.Count.EqualTo(4));
		}

		#endregion

		#region Complex tests

		[Test]
		public void ComplexTest1([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var result = db.Supplier.Select(
					supplier => db.Product.Select(
						product => db.Product.Where(p =>
							p.ProductID == product.ProductID && p.Supplier!.SupplierID == supplier.SupplierID)))
				.ToList();
			var count = result.Count;
			Assert.That(count, Is.GreaterThan(0));
			foreach (var queryable in result)
			{
				foreach (var queryable1 in queryable)
				{
					foreach (var product in queryable1)
					{
						Assert.That(product, Is.Not.Null);
					}
				}
			}
		}

		[Test]
		public void ComplexTest2([NorthwindDataContext] string context)
		{
			using var db = Setup(context);

			//TODO: sdanyliv: It can be replaced by the following linq expression. We have to decide that we have time to implement.
			var r = from c in db.Customer
				group c by c.Country
				into g
				from gi in g
				where gi.CompanyName.Substring(0, 1) == g.Key.Substring(0, 1)
				select gi;

			var result = db.Customer
				.GroupBy(c => c.Country!,
					(country, customers) =>
						customers.Where(k => k.CompanyName.Substring(0, 1) == country.Substring(0, 1)))
				.SelectMany(k => k);
			var expected = Customers
				.GroupBy(c => c.Country!,
					(country, customers) =>
						customers.Where(k => k.CompanyName.Substring(0, 1) == country.Substring(0, 1)))
				.SelectMany(k => k);

			Assert.That(expected.Except(result).Count(), Is.Zero);
		}

		[Test]
		public void ComplexTest3([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var products  = db.Product;
			var suppliers = db.Supplier;
			var result = from p in products
				select new
				{
					Product = p,
					Suppliers = suppliers
						.Where(s => s.SupplierID == p.Supplier!.SupplierID)
						.Select(s => s.CompanyName)
				};
			var list = result.ToList();
			Assert.That(list, Is.Not.Empty);
			foreach (var p in list)
			foreach (var companyName in p.Suppliers)
					Assert.That(companyName, Is.Not.Null);
		}

		[Test, ActiveIssue(573)]
		public void ComplexTest4([NorthwindDataContext] string context)
		{
			//TODO: sdanyliv: This is a bug
			using var db = Setup(context);
			var result = db.Customer
				.Take(2)
				.Select(
					c =>
						db.Order.Select(o => db.Employee.Take(2).Where(e => e.Orders.Contains(o)))
							.Where(o => o.Count() > 0))
				.Select(os => os);

			var list = result.ToList();
			Assert.That(list, Is.Not.Empty);

			foreach (var item in list)
				item.ToList();
		}

		[Test]
		public void ComplexTest5([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var result = db.Customer
				.Select(c => new {Customer = c, Order = db.Order})
				.Select(i => i.Customer.Orders);

			var list = result.ToList();
			Assert.That(list, Is.Not.Empty);

			foreach (var item in list)
				item.ToList();
		}

		[Test]
		public void ComplexTest6([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var r =
				from c in db.Customer
				from o in db.Order
				where o.Customer == c
				select new
				{
					Customer = c,
					Order = o
				};

			var result = db.Customer
				.Select(c => new {Customer = c, Order = db.Order.Where(o => o.Customer == c)})
				.SelectMany(i => i.Order.Select(o => new {i.Customer, Order = o}));

			var list = result.ToList();
			Assert.That(list, Is.Not.Empty);
		}

		#endregion

		#region Standard functions tests

		[Test]
		public void StringStartsWithTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var result = db.Customer.Where(c => c.CustomerID.StartsWith("A") || c.CustomerID.StartsWith("L"));

			var list = result.ToList();
			Assert.That(list, Has.Count.EqualTo(13));
		}

		[Test]
		public void StringStartsWithParameterizedTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var likeA = "A";
			var likeL = "L";
			var result = db.Customer.Where(c => c.CustomerID.StartsWith(likeA) || c.CustomerID.StartsWith(likeL));

			var list = result.ToList();
			Assert.That(list, Has.Count.EqualTo(13));
		}

		[Test]
		public void StringLengthTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var customer = db.Customer.Where(c => c.City!.Length == 7).First();
			Assert.That(customer, Is.Not.Null);
		}

		[Test]
		public void StringContainsTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var customer = db.Customer.Where(c => c.ContactName!.Contains("and")).First();
			Assert.That(customer, Is.Not.Null);
		}

		[Test]
		public void StringToLowerTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var customer = db.Customer.Where(c => c.City!.ToLower() == "seattle").First();
			Assert.That(customer, Is.Not.Null);
		}

		[Test]
		public void StringRemoveTest([NorthwindDataContext(false, true)] string context)
		{
			using var db = Setup(context);
			var customer = db.Customer.Where(c => c.City!.Remove(3) == "Sea").First();
			Assert.That(customer, Is.Not.Null);
		}

		[Test]
		public void StringIndexOfTest([NorthwindDataContext(false, true)] string context)
		{
			using var db = Setup(context);
			var customer = db.Customer.Where(c => c.City!.IndexOf("tt") == 3).First();
			Assert.That(customer, Is.Not.Null);
		}

		[Test]
		public void StringLastIndexOfTest([NorthwindDataContext(false, true)] string context)
		{
			using var db = Setup(context);
			var customer = db.Customer.Where(c => c.City!.LastIndexOf("t", 1, 3) == 3).First();
			Assert.That(customer, Is.Not.Null);
		}

		[Test]
		public void StringPadLeftTest([NorthwindDataContext(false, true)] string context)
		{
			using var db = Setup(context);
			var customer = db.Customer.Where(c => "123" + c.City!.PadLeft(8) == "123 Seattle").First();
			Assert.That(customer, Is.Not.Null);
		}

		[Test]
		public void DateTimeTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var order = db.Order.Where(o => o.OrderDate >= new DateTime(o.OrderDate!.Value.Year, 1, 1)).First();
			Assert.That(order, Is.Not.Null);
		}

		[Test]
		public void DateTimeDayTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var order = db.Order.Where(o => o.OrderDate!.Value.Day == 5).First();
			Assert.That(order, Is.Not.Null);
		}

		[Test]
		public void DateTimeDayOfWeek([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var order = db.Order.Where(o => o.OrderDate!.Value.DayOfWeek == DayOfWeek.Friday).First();
			Assert.That(order, Is.Not.Null);
		}

		[Test]
		public void DateTimeDayOfYear([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var order = db.Order.Where(o => o.OrderDate!.Value.DayOfYear == 360).First();
			Assert.That(order, Is.Not.Null);
		}

		[Test]
		public void MathAbsTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var order = db.Order.Where(o => Math.Abs(o.OrderID) == 10 || o.OrderID > 0).First();
			Assert.That(order, Is.Not.Null);
		}

		[Test]
		public void MathTrignometricTest([NorthwindDataContext(false, true)] string context)
		{
			using var db = Setup(context);
			var order = db.Order.Where(o => Math.Asin(Math.Cos(o.OrderID)) == 0 || o.OrderID > 0).First();
			Assert.That(order, Is.Not.Null);
		}

		[Test]
		public void MathFloorTest([NorthwindDataContext(false, true)] string context)
		{
			using var db = Setup(context);
			var result = db.Order.Where(o => Math.Floor(o.Freight) == 140);
			var list = result.ToList();
			Assert.That(list, Has.Count.EqualTo(2));
		}

		[Test]
		public void MathCeilingTest([NorthwindDataContext(false, true)] string context)
		{
			using var db = Setup(context);
			var result = db.Order.Where(o => Math.Ceiling(o.Freight) == 141);
			var list = result.ToList();
			Assert.That(list, Has.Count.EqualTo(2));
		}

		[Test]
		public void MathTruncateTest([NorthwindDataContext(false, true)] string context)
		{
			using var db = Setup(context);
			var result = db.Order.Where(o => Math.Truncate(o.Freight) == 141);
			var list = result.ToList();
			Assert.That(list, Has.Count.EqualTo(2));
		}

		[Test]
		public void MathRoundAwayFromZeroTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var result = db.Order.Where(o => Math.Round(o.Freight / 10, 1, MidpointRounding.AwayFromZero) == 6.5m);
			var list = result.ToList();
			Assert.That(list, Has.Count.EqualTo(7));
		}

		[Test]
		public void MathRoundToEvenTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var result = db.Order.Where(o => Math.Round(o.Freight / 10, 1, MidpointRounding.ToEven) == 6.5m);
			var list = result.ToList();
			Assert.That(list, Has.Count.EqualTo(6));
		}

		[Test]
		public void MathRoundDefaultTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var result = db.Order.Where(o => Math.Round(o.Freight / 10, 1) == 6.5m);
			var list = result.ToList();
			Assert.That(list, Has.Count.EqualTo(6));
		}

		[Test]
		public void ConvertToInt32([NorthwindDataContext(false, true)] string context)
		{
			using var db = Setup(context);
			var expected = Order.Where(o => Convert.ToInt32(o.Freight * 10) == 592);
			var result = db.Order.Where(o => Convert.ToInt32(o.Freight * 10) == 592);
			var list = result.ToList();
			Assert.That(list, Has.Count.EqualTo(expected.Count()));
		}

		[Test]
		public void StringCompareToTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var customer = db.Customer.Where(c => c.City!.CompareTo("Seattle") >= 0).First();
			Assert.That(customer, Is.Not.Null);
		}

		[Test]
		public void ComparisonWithNullTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var customer = db.Customer.Where(c => null != c.City).First();
			Assert.That(customer, Is.Not.Null);
		}

		[Test]
		public void EqualsWithNullTest([NorthwindDataContext] string context)
		{
			using var db = Setup(context);
			var customer = db.Customer.Where(c => !c.Address!.Equals(null)).First();
			Assert.That(customer, Is.Not.Null);
		}

		#endregion
	}
}
