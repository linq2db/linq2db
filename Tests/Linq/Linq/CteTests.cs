﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Expressions;
using LinqToDB.Mapping;
using LinqToDB.Tools;
using NUnit.Framework;

namespace Tests.Linq
{
	using FluentAssertions;
	using LinqToDB.Linq;
	using Model;

	public class CteTests : TestBase
	{
		public static string[] CteSupportedProviders = new[]
		{
			TestProvName.AllSqlServer2008Plus,
			TestProvName.AllFirebird,
			TestProvName.AllPostgreSQL,
			ProviderName.DB2,
			TestProvName.AllSQLite,
			TestProvName.AllOracle,
			TestProvName.AllMySqlWithCTE,
			// TODO: v14
			//TestProvName.AllInformix,
			// Will be supported in SQL 8.0 - ProviderName.MySql
		}.SelectMany(_ => _.Split(',')).ToArray();

		public class CteContextSourceAttribute : IncludeDataSourcesAttribute
		{
			public CteContextSourceAttribute() : this(true)
			{
			}

			public CteContextSourceAttribute(bool includeLinqService)
				: base(includeLinqService, CteSupportedProviders)
			{
			}

			public CteContextSourceAttribute(params string[] excludedProviders)
				: base(CteSupportedProviders.Except(excludedProviders.SelectMany(_ => _.Split(','))).ToArray())
			{
			}

			public CteContextSourceAttribute(bool includeLinqService, params string[] excludedProviders)
				: base(includeLinqService, CteSupportedProviders.Except(excludedProviders.SelectMany(_ => _.Split(','))).ToArray())
			{
			}
		}

		[Test]
		public void Test1([CteContextSource] string context)
		{
			using (var db = GetDataContext(context))
			{
				var cte1 = db.GetTable<Child>().Where(c => c.ParentID > 1).AsCte();
				var query = from p in db.Parent
					join c in cte1 on p.ParentID equals c.ParentID
					join c2 in cte1 on p.ParentID equals c2.ParentID
					select p;

				var cte1_ = db.GetTable<Child>().Where(c => c.ParentID > 1);

				var expected =
					from p in db.Parent
					join c in cte1_ on p.ParentID equals c.ParentID
					join c2 in cte1_ on p.ParentID equals c2.ParentID
					select p;

				AreEqual(expected, query);
			}
		}

		[Test]
		public void Test2([CteContextSource] string context)
		{
			using (var db = GetDataContext(context))
			{
				var cte1 = db.GetTable<Child>().Where(c => c.ParentID > 1).AsCte("CTE1_");
				var cte2 = db.Parent.Where(p => cte1.Any(c => c.ParentID == p.ParentID)).AsCte("CTE2_");
				var cte3 = db.Parent.Where(p => cte2.Any(c => c.ParentID == p.ParentID)).AsCte("CTE3_");
				var result = from p in cte2
					join c in cte1 on p.ParentID equals c.ParentID
					join c2 in cte2 on p.ParentID equals c2.ParentID
					join c3 in cte3 on p.ParentID equals c3.ParentID
					from c4 in db.Child.Where(c4 => c4.ParentID % 2 == 0).AsCte("LATEST").InnerJoin(c4 => c4.ParentID == c3.ParentID)
					select c3;

				var ncte1 = db.GetTable<Child>().Where(c => c.ParentID > 1);
				var ncte2 = db.Parent.Where(p => ncte1.Any(c => c.ParentID == p.ParentID));
				var ncte3 = db.Parent.Where(p => ncte2.Any(c => c.ParentID == p.ParentID));
				var expected = from p in ncte2
					join c in ncte1 on p.ParentID equals c.ParentID
					join c2 in ncte2 on p.ParentID equals c2.ParentID
					join c3 in ncte3 on p.ParentID equals c3.ParentID
					from c4 in db.Child.Where(c4 => c4.ParentID % 2 == 0).InnerJoin(c4 => c4.ParentID == c3.ParentID)
					select c3;

				var expectedStr = expected.ToString();
				var resultdStr  = result.ToString();

				// Looks like we do not populate needed field for CTE. It is aproblem that needs to be solved
				AreEqual(expected, result);
			}
		}

		[Test]
		public void TestAsTable([CteContextSource] string context)
		{
			using (var db = GetDataContext(context))
			{
				var cte1 = db.GetTable<Child>().AsCte("CTE1_");
				var expected = db.GetTable<Child>();

				AreEqual(expected, cte1);
			}
		}

		static IQueryable<TSource> RemoveCte<TSource>(IQueryable<TSource> source)
		{
			var newExpr = source.Expression.Transform<object?>(null, static (_, e) =>
			{
				if (e is MethodCallExpression methodCall && methodCall.Method.Name == "AsCte")
					return methodCall.Arguments[0];
				return e;
			});

			return source.Provider.CreateQuery<TSource>(newExpr);
		}

		[Test]
		public void ProductAndCategoryNamesOverTenDollars([NorthwindDataContext] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var cteQuery =
					from p in db.Product
					where p.UnitPrice!.Value > 10
					select new
					{
						p.ProductName,
						p.Category!.CategoryName,
						p.UnitPrice
					};

				var result =
					from p in cteQuery.AsCte("ProductAndCategoryNamesOverTenDollars")
					orderby p.CategoryName, p.UnitPrice, p.ProductName
					select p;

				var expected =
					from p in cteQuery
					orderby p.CategoryName, p.UnitPrice, p.ProductName
					select p;

				var expectedStr = expected.ToString();
				var resultdStr  = result.ToString();

				AreEqual(expected, result);
			}
		}

		[Test]
		public void ProductAndCategoryNamesOverTenDollars2([NorthwindDataContext] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var cteQuery =
					from p in db.Product
					where p.UnitPrice!.Value > 10
					select new
					{
						p.ProductName,
						p.Category!.CategoryName,
						p.UnitPrice
					};

				var result =
					from p in cteQuery.AsCte("ProductAndCategoryNamesOverTenDollars")
					orderby p.CategoryName, p.UnitPrice, p.ProductName
					select new
					{
						p.ProductName,
						p.CategoryName,
						p.UnitPrice
					};

				var expected =
					from p in cteQuery
					orderby p.CategoryName, p.UnitPrice, p.ProductName
					select new
					{
						p.ProductName,
						p.CategoryName,
						p.UnitPrice
					};

				var expectedStr = expected.ToString();
				var resultdStr  = result.ToString();

				AreEqual(expected, result);
			}
		}

		[Test]
		public void ProductsOverTenDollars([NorthwindDataContext] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var categoryAndNumberOfProducts =
					from c in db.Category
					select new
					{
						c.CategoryID,
						c.CategoryName,
						NumberOfProducts = db.Product.Where(p => p.CategoryID == c.CategoryID).Count()
					};

				var productsOverTenDollars =
					from p in db.Product
					where p.UnitPrice!.Value > 10
					select p;

				var result =
					from p in productsOverTenDollars.AsCte("ProductsOverTenDollars")
					from c in categoryAndNumberOfProducts.AsCte("CategoryAndNumberOfProducts").InnerJoin(c => c.CategoryID == p.CategoryID)
					orderby p.ProductName
					select new
					{
						c.CategoryName,
						c.NumberOfProducts,
						p.ProductName,
						p.UnitPrice
					};

				var expected =
					from p in productsOverTenDollars
					from c in categoryAndNumberOfProducts.InnerJoin(c => c.CategoryID == p.CategoryID)
					orderby p.ProductName
					select new
					{
						c.CategoryName,
						c.NumberOfProducts,
						p.ProductName,
						p.UnitPrice
					};

				var expectedStr = expected.ToString();
				var resultdStr  = result.ToString();

				AreEqual(expected, result);
			}
		}


		[Test]
		public void EmployeeSubordinatesReport([NorthwindDataContext] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var employeeSubordinatesReport  =
					from e in db.Employee
					select new
					{
						e.EmployeeID,
						e.LastName,
						e.FirstName,
						NumberOfSubordinates = db.Employee.Where(e2 => e2.ReportsTo == e.ReportsTo).Count(),
						e.ReportsTo
					};

				var employeeSubordinatesReportCte = employeeSubordinatesReport.AsCte("EmployeeSubordinatesReport");

				var actualQuery =
					from employee in employeeSubordinatesReportCte
					from manager in employeeSubordinatesReportCte.LeftJoin(manager => employee.ReportsTo == manager.EmployeeID)
					select new
					{
						employee.LastName,
						employee.FirstName,
						employee.NumberOfSubordinates,
						ManagerLastName = manager.LastName,
						ManagerFirstName = manager.FirstName,
						ManagerNumberOfSubordinates = manager.NumberOfSubordinates
					};

				var expectedQuery =
					from employee in employeeSubordinatesReport
					from manager in employeeSubordinatesReport.LeftJoin(manager => employee.ReportsTo == manager.EmployeeID)
					select new
					{
						employee.LastName,
						employee.FirstName,
						employee.NumberOfSubordinates,
						ManagerLastName = manager.LastName,
						ManagerFirstName = manager.FirstName,
						ManagerNumberOfSubordinates = manager.NumberOfSubordinates
					};

				var actual   = actualQuery.ToArray();
				var expected = expectedQuery.ToArray();

				AreEqual(expected, actual);
			}
		}

		class EmployeeHierarchyCTE
		{
			public int EmployeeID;
			public string LastName  = null!;
			public string FirstName = null!;
			public int? ReportsTo;
			public int HierarchyLevel;
		}

		[Test]
		public void EmployeeHierarchy([NorthwindDataContext(true)] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				// just create another CTE
				var employeeCte = db.Employee.Where(e => e.EmployeeID > 0).AsCte();

				var employeeHierarchyCte = db.GetCte<EmployeeHierarchyCTE>(employeeHierarchy =>
				{
					return
						(
							from e in employeeCte
							where e.ReportsTo == null
							select new EmployeeHierarchyCTE
							{
								EmployeeID = e.EmployeeID,
								LastName = e.LastName,
								FirstName = e.FirstName,
								ReportsTo = e.ReportsTo,
								HierarchyLevel = 1
							}
						)
						.Concat
						(
							from e in employeeCte
							from eh in employeeHierarchy.InnerJoin(eh => e.ReportsTo == eh.EmployeeID)
							select new EmployeeHierarchyCTE
							{
								EmployeeID = e.EmployeeID,
								LastName = e.LastName,
								FirstName = e.FirstName,
								ReportsTo = e.ReportsTo,
								HierarchyLevel = eh.HierarchyLevel + 1
							}
						);
				});

				var result =
					from eh in employeeHierarchyCte
					orderby eh.HierarchyLevel, eh.LastName, eh.FirstName
					select eh;

				var resultdStr  = result.ToString();

				var data = result.ToArray();
			}
		}

		[Test]
		public void Test4([CteContextSource] string context)
		{
			using (var db = GetDataContext(context))
			{
				var cte1 = db.GetTable<Child>().Where(c => c.ParentID > 1).AsCte("CTE1_");
				var result = from p in cte1
					from c4 in db.Child.Where(c4 => c4.ParentID % 2 == 0).AsCte("LAST").InnerJoin(c4 => c4.ParentID == p.ParentID)
					select c4;

				var _cte1 = db.GetTable<Child>().Where(c => c.ParentID > 1);
				var expected = from p in _cte1
					from c4 in db.Child.Where(c4 => c4.ParentID % 2 == 0).InnerJoin(c4 => c4.ParentID == p.ParentID)
					select c4;

				var expectedStr = expected.ToString();
				var resultdStr  = result.ToString();

				AreEqual(expected, result);
			}
		}

		[Test]
		public void Test5([CteContextSource] string context)
		{
			using (var db = GetDataContext(context))
			{
				var cte1 = db.GetTable<Child>()
					.Where(c => c.ParentID > 1)
					.Select(child => new
					{
						child.ParentID,
						child.ChildID
					}).Distinct()
					.AsCte();
				var query = from p in db.Parent
					join c in cte1 on p.ParentID equals c.ParentID
					join c2 in cte1 on p.ParentID equals c2.ParentID
					select p;

				var cte1_ = db.GetTable<Child>().Where(c => c.ParentID > 1).Select(child => new
				{
					child.ParentID,
					child.ChildID
				}).Distinct();

				var expected =
					from p in db.Parent
					join c in cte1_ on p.ParentID equals c.ParentID
					join c2 in cte1_ on p.ParentID equals c2.ParentID
					select p;

				Assert.AreEqual(expected.Count(), query.Count());
			}
		}

		[Test]
		public void TestCustomCount([CteContextSource] string context)
		{
			using (var db = GetDataContext(context))
			{
				var cte1 = db.GetTable<Child>()
					.Where(c => c.ParentID > 1)
					.Select(child => new
					{
						child.ParentID,
						child.ChildID
					}).Distinct()
					.AsCte();

				var query = from c in cte1
					select new
					{
						Count = Sql.Ext.Count().ToValue()
					};


				var expected = Child
					.Where(c => c.ParentID > 1)
					.Select(child => new
					{
						child.ParentID,
						child.ChildID
					}).Distinct().Count();


				var actual = query.AsEnumerable().Select(c => c.Count).First();

				Assert.AreEqual(expected, actual);
			}
		}

		private class CteDMLTests
		{
			protected bool Equals(CteDMLTests other)
			{
				return ChildID == other.ChildID && ParentID == other.ParentID;
			}

			public override bool Equals(object? obj)
			{
				if (ReferenceEquals(null, obj)) return false;
				if (ReferenceEquals(this, obj)) return true;
				if (obj.GetType() != GetType()) return false;
				return Equals((CteDMLTests)obj);
			}

			public override int GetHashCode()
			{
				unchecked
				{
					return (ChildID * 397) ^ ParentID;
				}
			}

			public int ChildID  { get; set; }
			public int ParentID { get; set; }
		}

		[Test]
		public void TestNoColumns([CteContextSource(true, ProviderName.DB2)] string context)
		{
			using (var db = GetDataContext(context))
			//using (var testTable = db.CreateLocalTable<CteDMLTests>("CteChild"))
			{
				var expected = db.GetTable<Child>().Count();

				var cte1 = db.GetTable<Child>().AsCte("CTE1_");
				var cnt1 = cte1.Count();

				Assert.AreEqual(expected, cnt1);

				var query = db.GetTable<Child>().Select(c => new { C = new { c.ChildID }});
				var cte2 = query.AsCte("CTE1_");
				var cnt2 = cte2.Count();

				Assert.AreEqual(expected, cnt2);

				var any  = cte2.Any();

				Assert.IsTrue(any);
			}
		}

		[Test]
		public void TestCondition([CteContextSource(true)] string context)
		{
			using (var db = GetDataContext(context))
			{
				int? var3 = 1;
				var cte = db.GetTable<Child>().AsCte();

				var query = cte.Where(t => t.ChildID == var3 || var3 == null);
				var str = query.ToString()!;
				Assert.That(str.Contains("WITH"), Is.EqualTo(true));
			}
		}

		[Test]
		public void TestInsert([CteContextSource(true, ProviderName.DB2)] string context)
		{
			using (var db = GetDataContext(context))
			using (var testTable = db.CreateLocalTable<CteDMLTests>("CteChild"))
			{
				var cte1 = db.GetTable<Child>().Where(c => c.ParentID > 1).AsCte("CTE1_");
				var toInsert = from p in cte1
					from c4 in db.Child.Where(c4 => c4.ParentID % 2 == 0).AsCte("LAST").InnerJoin(c4 => c4.ParentID == p.ParentID)
					select new CteDMLTests
					{
						ChildID = c4.ChildID,
						ParentID = c4.ParentID
					};

				var affected = toInsert.Insert(testTable, c => c);

				var _cte1 = db.GetTable<Child>().Where(c => c.ParentID > 1);
				var expected = from p in _cte1
					from c4 in db.Child.Where(c4 => c4.ParentID % 2 == 0).InnerJoin(c4 => c4.ParentID == p.ParentID)
					select new CteDMLTests
					{
						ChildID = c4.ChildID,
						ParentID = c4.ParentID
					};

				var result = testTable.OrderBy(c => c.ChildID).ThenBy(c => c.ParentID);
				expected   = expected. OrderBy(c => c.ChildID).ThenBy(c => c.ParentID);

				AreEqual(expected, result);
			}
		}

		// MariaDB support expected in v10.6 : https://jira.mariadb.org/browse/MDEV-18511
		[Test]
		public void TestDelete([CteContextSource(TestProvName.AllFirebird, ProviderName.DB2, TestProvName.MariaDB)] string context)
		{
			using (var db  = GetDataContext(context))
			using (var tmp = db.CreateLocalTable("CteChild",
				Enumerable.Range(0, 10).Select(i => new CteDMLTests { ParentID = i, ChildID = 1000 + i })))
			{
				var cte = tmp.Where(c => c.ParentID % 2 == 0).AsCte();

				var toDelete =
					from c in tmp
					from ct in cte.InnerJoin(ct => ct.ParentID == c.ParentID)
					select c;

				var recordsAffected = toDelete.Delete();
				Assert.AreEqual(5, recordsAffected);
			}
		}

		// MariaDB support expected in v10.6 : https://jira.mariadb.org/browse/MDEV-18511
		[ActiveIssue(Configuration = TestProvName.AllOracle, Details = "Oracle needs special syntax for CTE + UPDATE")]
		[Test]
		public void TestUpdate(
			[CteContextSource(TestProvName.AllFirebird, ProviderName.DB2, TestProvName.AllOracle, TestProvName.MariaDB)]
			string context)
		{
			using (var db = GetDataContext(context))
			using (var testTable = db.CreateLocalTable("CteChild",
				Enumerable.Range(0, 10).Select(i => new CteDMLTests { ParentID = i, ChildID = 1000 + i })))
			{
				var cte = testTable.Where(c => c.ParentID % 2 == 0).AsCte();
				var toUpdate =
					from c in testTable
					from ct in cte.InnerJoin(ct => ct.ParentID == c.ParentID)
					select c;

				toUpdate.Update(prev => new CteDMLTests {ParentID = prev.ChildID});

				var expected = testTable.Where(c => c.ParentID % 2 == 0)
					.Select(c => new CteDMLTests { ParentID = c.ChildID, ChildID = c.ChildID });

				var result = testTable.Where(c => c.ParentID % 2 == 0);

				AreEqual(expected, result);
			}
		}

		class RecursiveCTE
		{
			public int? ParentID;
			public int? ChildID;
			public int? GrandChildID;
		}

		[Test]
		public void RecursiveTest([CteContextSource(true, ProviderName.DB2)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var cteRecursive = db.GetCte<RecursiveCTE>(cte =>
						(
							from gc1 in db.GrandChild
							select new RecursiveCTE
							{
								ChildID = gc1.ChildID,
								ParentID = gc1.GrandChildID,
								GrandChildID = gc1.GrandChildID,
							}
						)
						.Concat
						(
							from gc in db.GrandChild
							from p in db.Parent.InnerJoin(p => p.ParentID == gc.ParentID)
							from ct in cte.InnerJoin(ct => Sql.AsNotNull(ct.ChildID) == gc.ChildID)
							where ct.GrandChildID <= 10
							select new RecursiveCTE
							{
								ChildID = ct.ChildID,
								ParentID = ct.ParentID,
								GrandChildID = ct.ChildID + 1
							}
						)
					, "MY_CTE");

				var str = cteRecursive.ToString();
				var result = cteRecursive.ToArray();
			}
		}

		public class HierarchyTree
		{
			public int Id { get; set; }
			public int? ParentId { get; set; }
		}

		HierarchyTree[] GeHirarchyData()
		{
			return new[]
			{
				new HierarchyTree { Id = 1, ParentId = null },
				new HierarchyTree { Id = 2, ParentId = null },

				// level 1

				new HierarchyTree { Id = 10, ParentId = 1 },
				new HierarchyTree { Id = 11, ParentId = 1 },

				new HierarchyTree { Id = 20, ParentId = 2 },
				new HierarchyTree { Id = 22, ParentId = 2 },

				// level 2

				new HierarchyTree { Id = 100, ParentId = 10 },
				new HierarchyTree { Id = 101, ParentId = 10 },
				new HierarchyTree { Id = 102, ParentId = 10 },

				new HierarchyTree { Id = 110, ParentId = 11 },
				new HierarchyTree { Id = 111, ParentId = 11 },
				new HierarchyTree { Id = 112, ParentId = 11 },

				new HierarchyTree { Id = 200, ParentId = 20 },
				new HierarchyTree { Id = 201, ParentId = 20 },
				new HierarchyTree { Id = 202, ParentId = 20 },

				new HierarchyTree { Id = 210, ParentId = 21 },
				new HierarchyTree { Id = 211, ParentId = 21 },
				new HierarchyTree { Id = 212, ParentId = 21 },
			};
		}

		class HierarchyData
		{
			public int Id { get; set; }
			public int Level { get; set; }
		}

		IQueryable<HierarchyData> GetHierarchyDown(IQueryable<HierarchyTree> tree, IDataContext db)
		{
			var subCte1 = tree.Where(t => t.ParentId == null).AsCte();
			var subCte2 = tree.AsCte();

			var cte = db.GetCte<HierarchyData>(hierarchyDown =>
				{
					return subCte1.Select(t => new HierarchyData
						{
							Id = t.Id,
							Level = 0
						})
						.Concat(
							from h in hierarchyDown
							from t in subCte2.InnerJoin(t => t.ParentId == h.Id)
							select new HierarchyData
							{
								Id = t.Id,
								Level = h.Level + 1
							}
						);
				}
			);

			return cte;
		}

		IEnumerable<HierarchyData> EnumerateDown(HierarchyTree[] items, int currentLevel, int? currentParent)
		{
			foreach (var i in items.Where(i => i.ParentId == currentParent))
			{
				yield return new HierarchyData { Id = i.Id, Level = currentLevel };

				foreach (var c in EnumerateDown(items, currentLevel + 1, i.Id))
				{
					yield return c;
				}
			}
		}

		[Test]
		public void RecursiveTest2([CteContextSource(true, ProviderName.DB2)] string context)
		{
			var hierarchyData = GeHirarchyData();

			using (var db = GetDataContext(context))
			using (var tree = db.CreateLocalTable(hierarchyData))
			{
				var hierarchy = GetHierarchyDown(tree, db);

				var result = hierarchy.OrderBy(h => h.Id);
				var expected = EnumerateDown(hierarchyData, 0, null).OrderBy(h => h.Id);

				AreEqualWithComparer(expected, result);
			}
		}

		[Test]
		public void TestDoubleRecursion([CteContextSource(true, ProviderName.DB2)] string context)
		{
			var hierarchyData = GeHirarchyData();

			using (var db = GetDataContext(context))
			using (var tree = db.CreateLocalTable(hierarchyData))
			{
				var hierarchy1 = GetHierarchyDown(tree, db);
				var hierarchy2 = GetHierarchyDown(tree, db);

				var query = from h1 in hierarchy1
					from h2 in hierarchy2.InnerJoin(h2 => h2.Id == h1.Id)
					select new
					{
						h1.Id,
						LevelSum = h2.Level + h1.Level
					};

				var count = query.Count();

				Assert.Greater(count, 0);
			}
		}

		[Test]
		public void RecursiveCount([CteContextSource(true, ProviderName.DB2)] string context)
		{
			var hierarchyData = GeHirarchyData();

			using (var db = GetDataContext(context))
			using (var tree = db.CreateLocalTable(hierarchyData))
			{
				var hierarchy = GetHierarchyDown(tree, db);
				var expected = EnumerateDown(hierarchyData, 0, null);

				Assert.AreEqual(expected.Count(), hierarchy.Count());
			}
		}

		[Test]
		public void RecursiveInsertInto([CteContextSource(true, ProviderName.DB2)] string context)
		{
			var hierarchyData = GeHirarchyData();

			using (var db          = GetDataContext(context))
			using (var tree        = db.CreateLocalTable(hierarchyData))
			using (var resultTable = db.CreateLocalTable<HierarchyData>())
			{
				var hierarchy = GetHierarchyDown(tree, db);
				hierarchy.Insert(resultTable, r => r);

				var result = resultTable.OrderBy(h => h.Id);
				var expected = EnumerateDown(hierarchyData, 0, null).OrderBy(h => h.Id);

				AreEqualWithComparer(expected, result);
			}
		}

		[Test]
		public void RecursiveDeepNesting([CteContextSource(true, ProviderName.DB2)] string context)
		{
			using (var db   = GetDataContext(context))
			using (var tree = db.CreateLocalTable<HierarchyTree>())
			{
				var hierarchy = GetHierarchyDown(tree, db);

				var query = from q in hierarchy
					from data1 in tree.InnerJoin(data1 => data1.Id == q.Id)
					from data2 in tree.InnerJoin(data2 => data2.Id == q.Id)
					from data3 in tree.InnerJoin(data3 => data3.Id == q.Id)
					from data4 in tree.InnerJoin(data4 => data4.Id == q.Id)
					select new
					{
						q.Id,
						q.Level
					};

				Assert.DoesNotThrow(() => TestContext.WriteLine(query.ToString()));
			}
		}

		private class TestWrapper
		{
			public Child? Child { get; set; }

			protected bool Equals(TestWrapper other)
			{
				return Equals(Child, other.Child);
			}

			public override bool Equals(object? obj)
			{
				if (ReferenceEquals(null, obj)) return false;
				if (ReferenceEquals(this, obj)) return true;
				if (obj.GetType() != GetType()) return false;
				var result = Equals((TestWrapper)obj);
				return result;
			}

			public override int GetHashCode()
			{
				return (Child != null ? Child.GetHashCode() : 0);
			}
		}

		private class TestWrapper2
		{
			public Child?  Child   { get; set; }
			public Parent? Parent { get; set; }

			protected bool Equals(TestWrapper2 other)
			{
				return Equals(Child, other.Child) && Equals(Parent, other.Parent);
			}

			public override bool Equals(object? obj)
			{
				if (ReferenceEquals(null, obj)) return false;
				if (ReferenceEquals(this, obj)) return true;
				if (obj.GetType() != GetType()) return false;
				return Equals((TestWrapper2)obj);
			}

			public override int GetHashCode()
			{
				unchecked
				{
					return ((Child != null ? Child.GetHashCode() : 0) * 397) ^ (Parent != null ? Parent.GetHashCode() : 0);
				}
			}
		}

		[Test]
		public void TestWithWrapper([CteContextSource] string context)
		{
			using (var db = GetDataContext(context))
			{
				var cteQuery = db.GetTable<Child>()
					.Select(child => new TestWrapper()
					{
						Child = child
					});

				var cte1 = cteQuery.AsCte();

				var query = from p in db.Parent
					join c in cte1 on p.ParentID equals c.Child!.ParentID
					select new {p, c};

				var result = query.ToArray();

				var expected =
					from p in db.Parent
					join c in cteQuery on p.ParentID equals c.Child!.ParentID
					select new {p, c};

				Assert.AreEqual(expected, result);
			}
		}

		[Test]
		public void TestWithWrapperUnion([CteContextSource] string context)
		{
			using (var db = GetDataContext(context))
			{
				var cte1 = db.GetTable<Child>()
					.Select(child => new TestWrapper2()
					{
						Child = child,
						Parent = child.Parent
					})
					.AsCte();

				var simpleQuery = db.Child.Select(child => new TestWrapper2
				{
					Parent = child.Parent,
					Child = child
				});

				var query1 = simpleQuery.Union(cte1);
				var query2 = cte1.Union(simpleQuery);

				var cte1_ = Child
					.Select(child => new TestWrapper2()
					{
						Child = child,
						Parent = child.Parent
					});

				var simpleQuery_ = Child.Select(child => new TestWrapper2
				{
					Parent = child.Parent,
					Child = child
				});

				var query1_ = simpleQuery_.Union(cte1_);
				var query2_ = cte1_.Union(simpleQuery_);


				AreEqual(query1_, query1);
				AreEqual(query2_, query2);
			}
		}

		[Test]
		public void TestEmbedded([CteContextSource] string context)
		{
			using (var db = GetDataContext(context))
			{
				var cte1 = db.GetTable<Child>().Select(c => c.ChildID).AsCte("CTE_1");
				var cte2 = cte1.Distinct().AsCte("CTE_2");
				var cte3 = cte2.Distinct().AsCte("CTE_3");
				var cte4 = cte3.Distinct().AsCte("CTE_3");

				var qCte = db.Child.Where(w => w.ChildID.NotIn(cte4)).ToList();
			}
		}

		[Test]
		public void TestCteOptimization([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var children = db.Child.Where(c => c.ChildID > 1).AsCte().HasUniqueKey(ct => ct.ChildID);

				var query =
					from c in db.Child
					from ct in children.LeftJoin(ct => c.ChildID == ct.ChildID)
					select c;

				var sql = query.ToString();
				TestContext.WriteLine(sql);

				Assert.That(sql, Is.Not.Contains("WITH"));
			}
		}

		[ActiveIssue("Scalar recursive CTE are not working: SQL logic error near *: syntax error")]
		[Test]
		public void TestRecursiveScalar([CteContextSource] string context)
		{
			using (var db = GetDataContext(context))
			{
				var cteRecursive = db.GetCte<int>(cte =>
						(
							from c in db.Child.Take(1)
							select c.ChildID
						)
						.Concat
						(
							from c in db.Child
							from ct in cte.InnerJoin(ct => ct == c.ChildID + 1)
							select c.ChildID + 1
						)
					, "MY_CTE");

				var result = cteRecursive.ToArray();
			}
		}

		class OrgGroupDepthWrapper
		{
			public OrgGroup? OrgGroup { get; set; }
			public int Depth { get; set; }
		}

		class OrgGroup
		{
			[PrimaryKey]
			public int Id { get; set; }
			public int ParentId { get; set; }
			public string? GroupName { get; set; }
		}

		[ActiveIssue(1644, Details = "Expression 'parent.OrgGroup' is not a Field.")]
		[Test]
		public void TestRecursiveObjects([CteContextSource] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<OrgGroup>())
			{
				var queryable = db.GetTable<OrgGroup>();
				var cte = db.GetCte<OrgGroupDepthWrapper>(previous =>
				    {
				        var parentQuery = from parent in queryable
				            select new OrgGroupDepthWrapper
				            {
				                OrgGroup = parent,
				                Depth = 0
				            };

				        var childQuery = from child in queryable
				            from parent in previous.InnerJoin(parent => parent.OrgGroup!.Id == child.ParentId)
				            orderby parent.Depth + 1, child.GroupName
				            select new OrgGroupDepthWrapper
				            {
				                OrgGroup = child,
				                Depth = parent.Depth + 1
				            };

				        return parentQuery.Union(childQuery);
				    })
				    .Select(wrapper => wrapper.OrgGroup);

				var result = cte.ToList();

			}
		}


		class NestingA
		{
			public string? Property1 { get; set; }
		}

		class NestingB : NestingA
		{
			public string? Property2 { get; set; }
		}

		class NestingC : NestingB
		{
			public string? Property3 { get; set; }
		}

		[Test]
		public void TestNesting([CteContextSource] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<NestingA>())
			using (db.CreateLocalTable<NestingB>())
			using (db.CreateLocalTable<NestingC>())
			{
				var cte1 = db.GetTable<NestingC>().Select(a => new NestingB { Property1 = a.Property2 }).AsCte();
				var cte2 =
					from c1 in cte1
					from t in db.GetTable<NestingC>()
					select new NestingB
					{
						Property1 = c1.Property1,
						Property2 = t.Property2
					};
				var cte3 =
					from c2 in cte2
					from t in db.GetTable<NestingC>()
					select new NestingC
					{
						Property1 = c2.Property1,
						Property2 = t.Property2,
						Property3 = t.Property3
					};

				var sql = cte3.ToArray();
			}
		}

		#region Issue 2029
		[Test]
		public void Issue2029Test([CteContextSource] string context)
		{
			using (new GenerateFinalAliases(true))
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<NcCode>())
			using (db.CreateLocalTable<NcGroupMember>())
			{
				var wipCte = new WipCte(db);

				var ncCodeBo = "NCCodeBO:8110,SETUP_OSCILLOSCO";

				var result = from item in wipCte.AllowedNcCode() where item.NcCodeBo == ncCodeBo select item;
				var sql = ((IExpressionQuery)result).SqlText;

				Assert.True(sql.Replace("\"", "").Replace("`", "").Replace("[", "").Replace("]", "").ToLowerInvariant().Contains("WITH AllowedNcCode (NcCodeBo, NcCode, NcCodeDescription)".ToLowerInvariant()));
			}
		}

		internal class WipCte
		{
			private readonly IDataContext db;

			internal WipCte(IDataContext db)
			{
				this.db = db;
			}

			internal IQueryable<AllowedNcCodeOutput> AllowedNcCode()
			{
				return (from ncCode in db.GetTable<NcCode>()
						join ncGroupMember in db.GetTable<NcGroupMember>()
						on ncCode.Handle equals ncGroupMember.NcCodeOrGroupGbo
						where
							ncGroupMember.NcGroupBo == "NCGroupBO:" + ncCode.Site + ",CATAN_AUTO" ||
							ncGroupMember.NcGroupBo == "NCGroupBO:" + ncCode.Site + ",CATAN_MAN" ||
							ncGroupMember.NcGroupBo == "NCGroupBO:" + ncCode.Site + ",CATAN_ALL"
						select new AllowedNcCodeOutput { NcCodeBo = ncCode.Handle, NcCode = ncCode.NcCodeColumn, NcCodeDescription = ncCode.Description }).Distinct().AsCte(nameof(AllowedNcCode));
			}

			internal class AllowedNcCodeOutput
			{
				internal string? NcCodeBo          { get; set; }
				internal string? NcCode            { get; set; }
				internal string? NcCodeDescription { get; set; }
			}
		}

		[Table(Name = "NC_CODE")]
		public partial class NcCode
		{
			[Column("HANDLE"), NotNull             ] public string    Handle           { get; set; } = null!; // NVARCHAR2(1236)
			[Column("CHANGE_STAMP"), Nullable      ] public decimal?  ChangeStamp      { get; set; } // NUMBER (38,0)
			[Column("SITE"), Nullable              ] public string?   Site             { get; set; } // NVARCHAR2(18)
			[Column("NC_CODE"), Nullable           ] public string?   NcCodeColumn     { get; set; } // NVARCHAR2(48)
			[Column("DESCRIPTION"), Nullable       ] public string?   Description      { get; set; } // NVARCHAR2(120)
			[Column("STATUS_BO"), Nullable         ] public string?   StatusBo         { get; set; } // NVARCHAR2(1236)
			[Column("CREATED_DATE_TIME"), Nullable ] public DateTime? CreatedDateTime  { get; set; } // DATE
			[Column("MODIFIED_DATE_TIME"), Nullable] public DateTime? ModifiedDateTime { get; set; } // DATE
			[Column("NC_CATEGORY"), Nullable       ] public string?   NcCategory       { get; set; } // NVARCHAR2(60)
			[Column("DPMO_CATEGORY_BO"), Nullable  ] public string?   DpmoCategoryBo   { get; set; } // NVARCHAR2(1236)
		}
		[Table(Name = "NC_GROUP_MEMBER")]
		public partial class NcGroupMember
		{
			[Column("HANDLE"), NotNull               ] public string   Handle           { get; set; } = null!; // NVARCHAR2(1236)
			[Column("NC_GROUP_BO"), Nullable         ] public string?  NcGroupBo        { get; set; } // NVARCHAR2(1236)
			[Column("NC_CODE_OR_GROUP_GBO"), Nullable] public string?  NcCodeOrGroupGbo { get; set; } // NVARCHAR2(1236)
			[Column("SEQUENCE"), Nullable            ] public decimal? Sequence         { get; set; } // NUMBER (38,0)
		}
		#endregion

		private class Issue3359Projection
		{
			public string FirstName { get; set; } = null!;
			public string LastName  { get; set; } = null!;
		}

		[Test(Description = "Test that we generate plain UNION without sub-queries (or query will be invalid)")]
		public void Issue3359_MultipleSets([CteContextSource(
			TestProvName.AllOracle, // too many unions (ORA-32041: UNION ALL operation in recursive WITH clause must have only two branches)
			TestProvName.AllPostgreSQL, // too many joins? (42P19: recursive reference to query "cte" must not appear within its non-recursive term)
			ProviderName.DB2 // joins (SQL0345N  The fullselect of the recursive common table expression "cte" must be the UNION of two or more fullselects and cannot include column functions, GROUP BY clause, HAVING clause, ORDER BY clause, or an explicit join including an ON clause.)
			)] string context)
		{
			if (context.Contains(ProviderName.SQLite))
			{
				using var dc = (TestDataConnection)GetDataContext(context.Replace(".LinqService", string.Empty));
				if (TestUtils.GetSqliteVersion(dc) < new Version(3, 34))
				{
					// SQLite Error 1: 'circular reference: cte'.
					Assert.Inconclusive("SQLite version 3.34.0 or greater required");
				}
			}

			using var db = GetDataContext(context);

			var query = db.GetCte<Issue3359Projection>(cte =>
			{
				return db.Person.Select(p => new Issue3359Projection() { FirstName = p.FirstName, LastName = p.LastName })
				.Concat(
					from p in cte
					join d in db.Doctor on p.FirstName equals d.Taxonomy
					select new Issue3359Projection() { FirstName = p.FirstName, LastName = p.LastName }
					)
				.Concat(
					from p in cte
					join pat in db.Patient on p.FirstName equals pat.Diagnosis
					select new Issue3359Projection() { FirstName = p.FirstName, LastName = p.LastName }
					);
			});

			query.ToArray();

			if (db is TestDataConnection cn)
				cn.LastQuery!.Should().Contain("SELECT", Exactly.Times(4));
		}



		public record class  Issue3357RecordClass (int Id, string FirstName, string LastName);
		public class Issue3357RecordLike
		{
			public Issue3357RecordLike(int Id, string FirstName, string LastName)
			{
				this.Id        = Id;
				this.FirstName = FirstName;
				this.LastName  = LastName;
			}

			public int    Id        { get; }
			public string FirstName { get; }
			public string LastName  { get; }
		}

		[Test(Description = "record type support")]
		public void Issue3357_RecordClass([CteContextSource(ProviderName.DB2)] string context)
		{
			using var db = GetDataContext(context);

			var query = db.GetCte<Issue3357RecordClass>(cte =>
			{
				return db.Person.Select(p => new Issue3357RecordClass(p.ID, p.FirstName, p.LastName))
				.Concat(
					from p in cte
					join r in db.Person on p.FirstName equals r.LastName
					select new Issue3357RecordClass(r.ID, r.FirstName, r.LastName)
					);
			});

			AreEqual(
				Person.Select(p => new Issue3357RecordClass(p.ID, p.FirstName, p.LastName)),
				query.ToArray());
		}

		[Test(Description = "record type support")]
		public void Issue3357_RecordClass_DB2([IncludeDataSources(true, ProviderName.DB2)] string context)
		{
			using var db = GetDataContext(context);

			var query = db.GetCte<Issue3357RecordClass>(cte =>
			{
				return db.Person.Select(p => new Issue3357RecordClass(p.ID, p.FirstName, p.LastName))
				.Concat(
					from p in cte
					from r in db.Person
					where p.FirstName == r.LastName
					select new Issue3357RecordClass(r.ID, r.FirstName, r.LastName)
					);
			});

			AreEqual(
				Person.Select(p => new Issue3357RecordClass(p.ID, p.FirstName, p.LastName)),
				query.ToArray());
		}

		[Test(Description = "record type support")]
		public void Issue3357_RecordLikeClass([CteContextSource(ProviderName.DB2)] string context)
		{
			using var db = GetDataContext(context);

			var query = db.GetCte<Issue3357RecordLike>(cte =>
			{
				return db.Person.Select(p => new Issue3357RecordLike(p.ID, p.FirstName, p.LastName))
				.Concat(
					from p in cte
					join r in db.Person on p.FirstName equals r.LastName
					select new Issue3357RecordLike(r.ID, r.FirstName, r.LastName)
					);
			});

			AreEqualWithComparer(
				Person.Select(p => new Issue3357RecordLike(p.ID, p.FirstName, p.LastName)),
				query.ToArray());
		}

		[Test(Description = "record type support")]
		public void Issue3357_RecordLikeClass_DB2([IncludeDataSources(true, ProviderName.DB2)] string context)
		{
			using var db = GetDataContext(context);

			var query = db.GetCte<Issue3357RecordLike>(cte =>
			{
				return db.Person.Select(p => new Issue3357RecordLike(p.ID, p.FirstName, p.LastName))
				.Concat(
					from p in cte
					from r in db.Person
					where p.FirstName == r.LastName
					select new Issue3357RecordLike(r.ID, r.FirstName, r.LastName)
					);
			});

			AreEqualWithComparer(
				Person.Select(p => new Issue3357RecordLike(p.ID, p.FirstName, p.LastName)),
				query.ToArray());
		}

		[Table]
		private class Issue3360Table
		{
			[PrimaryKey] public int Id { get; set; }
			// by default we generate N-literal, which is not compatible with (var)char
			[Column(DataType = DataType.VarChar)] public string? Str { get; set; }
		}

		private class Issue3360Projection
		{
			public int     Id   { get; set; }
			public string? Str  { get; set; }
		}

		// SqlException : Types don't match between the anchor and the recursive part in column "Str" of recursive query "cte".
		[Test(Description = "Test that we type literal/parameter in set query column properly")]
		public void Issue3360_TypeByOtherQuery([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<Issue3360Table>();

			var query = db.GetCte<Issue3360Projection>(cte =>
			{
				return tb.Select(p => new Issue3360Projection() { Id = p.Id, Str = p.Str })
				.Concat(
					from p in cte
					join r in tb on p.Id equals r.Id + 1
					select new Issue3360Projection() { Id = p.Id, Str = "Str" }
					);
			});

			query.ToArray();

			if (db is TestDataConnection dc)
			{
				dc.LastQuery!.Should().NotContain("N'");
				dc.LastQuery!.ToUpperInvariant().Should().Contain("AS VARCHAR(MAX))", Exactly.Twice());
			}
		}

		[Test(Description = "Test that we don't need typing for non-sqlserver providers")]
		public void Issue3360_TypeByOtherQuery_DB2([CteContextSource(ProviderName.DB2)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<Issue3360Table>();

			var query = db.GetCte<Issue3360Projection>(cte =>
			{
				return tb.Select(p => new Issue3360Projection() { Id = p.Id, Str = p.Str })
				.Concat(
					from p in cte
					join r in tb on p.Id equals r.Id + 1
					select new Issue3360Projection() { Id = p.Id, Str = "Str" }
					);
			});

			query.ToArray();
		}

		[Test(Description = "Test that we don't need typing for non-sqlserver providers")]
		public void Issue3360_TypeByOtherQuery_AllProviders([IncludeDataSources(true, ProviderName.DB2)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<Issue3360Table>();

			var query = db.GetCte<Issue3360Projection>(cte =>
			{
				return tb.Select(p => new Issue3360Projection() { Id = p.Id, Str = p.Str })
				.Concat(
					from p in cte
					from r in tb
					where p.Id == r.Id + 1
					select new Issue3360Projection() { Id = p.Id, Str = "Str" }
					);
			});

			query.ToArray();
		}

		[Table]
		private class Issue3360WithEnum
		{
			[Column                                          ] public int     Id  { get; set; }
			[Column(DataType = DataType.VarChar, Length = 50)] public StrEnum Str { get; set; }
		}

		enum StrEnum
		{
			[MapValue("THIS_IS_ONE")]
			One = 1,
			[MapValue("THIS_IS_TWO")]
			Two
		}

		private class Issue3360WithEnumProjection
		{
			public int     Id  { get; set; }
			public StrEnum Str { get; set; }
		}

		[Test(Description = "Test that we type literal/parameter in set query column properly")]
		public void Issue3360_TypeStringEnum([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<Issue3360WithEnum>();

			var query = db.GetCte<Issue3360WithEnumProjection>(cte =>
			{
				return tb.Select(p => new Issue3360WithEnumProjection() { Id = p.Id, Str = p.Str })
				.Concat(
					from p in cte
					join r in tb on p.Id equals r.Id + 1
					select new Issue3360WithEnumProjection() { Id = p.Id, Str = StrEnum.Two }
					);
			});

			query.ToArray();

			if (db is TestDataConnection dc)
			{
				dc.LastQuery!.Should().NotContain("N'");
				dc.LastQuery!.Should().Contain("'THIS_IS_TWO'");
				dc.LastQuery!.ToUpperInvariant().Should().Contain("AS VARCHAR(MAX))", Exactly.Twice());
			}
		}

		[Test(Description = "Test that we don't need typing for non-sqlserver providers")]
		public void Issue3360_TypeStringEnum_AllProviders([CteContextSource(ProviderName.DB2)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<Issue3360WithEnum>();

			var query = db.GetCte<Issue3360WithEnumProjection>(cte =>
			{
				return tb.Select(p => new Issue3360WithEnumProjection() { Id = p.Id, Str = p.Str })
				.Concat(
					from p in cte
					join r in tb on p.Id equals r.Id + 1
					select new Issue3360WithEnumProjection() { Id = p.Id, Str = StrEnum.Two }
					);
			});

			query.ToArray();
		}

		[Test(Description = "Test that we don't need typing for non-sqlserver providers")]
		public void Issue3360_TypeStringEnum_DB2([IncludeDataSources(true, ProviderName.DB2)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<Issue3360WithEnum>();

			var query = db.GetCte<Issue3360WithEnumProjection>(cte =>
			{
				return tb.Select(p => new Issue3360WithEnumProjection() { Id = p.Id, Str = p.Str })
				.Concat(
					from p in cte
					from r in tb
					where p.Id == r.Id + 1
					select new Issue3360WithEnumProjection() { Id = p.Id, Str = StrEnum.Two }
					);
			});

			query.ToArray();
		}

		[Test(Description = "Test that we type literal/parameter in set query column properly")]
		public void Issue3360_TypeByProjectionProperty([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<Issue3360Table>();

			var query = db.GetCte<Issue3360Table>(cte =>
			{
				return tb.Select(p => new Issue3360Table() { Id = p.Id, Str = "Str1" })
				.Concat(
					from p in cte
					join r in tb on p.Id equals r.Id + 1
					select new Issue3360Table() { Id = p.Id, Str = "Str2" }
					);
			});

			query.ToArray();
			if (db is TestDataConnection dc)
			{
				dc.LastQuery!.Should().NotContain("N'");
				dc.LastQuery!.ToUpperInvariant().Should().Contain("AS VARCHAR(MAX))", Exactly.Twice());
			}
		}

		[Test(Description = "Test that we don't need typing for non-sqlserver providers")]
		public void Issue3360_TypeByProjectionProperty_AllProviders([CteContextSource(ProviderName.DB2)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<Issue3360Table>();

			var query = db.GetCte<Issue3360Table>(cte =>
			{
				return tb.Select(p => new Issue3360Table() { Id = p.Id, Str = "Str1" })
				.Concat(
					from p in cte
					join r in tb on p.Id equals r.Id + 1
					select new Issue3360Table() { Id = p.Id, Str = "Str2" }
					);
			});

			query.ToArray();
		}

		[Test(Description = "Test that we don't need typing for non-sqlserver providers")]
		public void Issue3360_TypeByProjectionProperty_DB2([IncludeDataSources(true, ProviderName.DB2)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<Issue3360Table>();

			var query = db.GetCte<Issue3360Table>(cte =>
			{
				return tb.Select(p => new Issue3360Table() { Id = p.Id, Str = "Str1" })
				.Concat(
					from p in cte
					from r in tb
					where p.Id == r.Id + 1
					select new Issue3360Table() { Id = p.Id, Str = "Str2" }
					);
			});

			query.ToArray();
		}

		[Table]
		private class Issue3360NullInAnchor
		{
			[Column                                          ] public int       Id    { get; set; }
			[NotColumn(Configuration = ProviderName.Firebird)]
			[NotColumn(Configuration = ProviderName.DB2)     ]
			[Column                                          ] public Guid?     Guid  { get; set; }
			[Column(DataType = DataType.VarChar, Length = 50)] public StrEnum?  Enum1 { get; set; }
		}

		[Test(Description = "Test CTE columns typing")]
		public void Issue3360_NullGuidInAnchor([CteContextSource(TestProvName.AllFirebird, ProviderName.DB2)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<Issue3360NullInAnchor>();

			var query = db.GetCte<Issue3360NullInAnchor>(cte =>
			{
				return tb.Select(p => new Issue3360NullInAnchor() { Id = p.Id, Guid = null })
				.Concat(
					from p in cte
					join r in tb on p.Id equals r.Id + 100
					select new Issue3360NullInAnchor() { Id = p.Id, Guid = r.Guid }
					);
			});

			query.ToArray();
		}

		[Test(Description = "Test CTE columns typing")]
		public void Issue3360_NullEnumInAnchor([CteContextSource(ProviderName.DB2)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<Issue3360NullInAnchor>();

			var query = db.GetCte<Issue3360NullInAnchor>(cte =>
			{
				return tb.Select(p => new Issue3360NullInAnchor() { Id = p.Id, Enum1 = null })
				.Concat(
					from p in cte
					join r in tb on p.Id equals r.Id + 100
					select new Issue3360NullInAnchor() { Id = p.Id, Enum1 = StrEnum.One }
					);
			});

			query.ToArray();
		}

		[Test(Description = "Test CTE columns typing")]
		public void Issue3360_NullEnumInAnchor_DB2([IncludeDataSources(true, ProviderName.DB2)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<Issue3360NullInAnchor>();

			var query = db.GetCte<Issue3360NullInAnchor>(cte =>
			{
				return tb.Select(p => new Issue3360NullInAnchor() { Id = p.Id, Enum1 = null })
				.Concat(
					from p in cte
					from r in tb
					where p.Id == r.Id + 100
					select new Issue3360NullInAnchor() { Id = p.Id, Enum1 = StrEnum.One }
					);
			});

			query.ToArray();
		}

		#region InvalidColumnIndexMapping issue
		public enum InvalidColumnIndexMappingEnum1
		{
			[MapValue("ENUM1_VALUE")]
			Value
		}

		public enum InvalidColumnIndexMappingEnum2
		{
			[MapValue("ENUM2_VALUE")]
			Value
		}

		[Table]
		public class InvalidColumnIndexMappingTable1
		{
			[PrimaryKey] public Guid Id      { get; set; }
			[Column    ] public Guid ChildId { get; set; }
		}

		[Table]
		public class InvalidColumnIndexMappingTable2
		{
			[PrimaryKey                         ] public Guid                           Id    { get; set; }
			[Column(DataType = DataType.VarChar)] public InvalidColumnIndexMappingEnum2 Enum2 { get; set; }
		}

		[Table]
		public class InvalidColumnIndexMappingTable3
		{
			[PrimaryKey                         ] public Guid                           Id    { get; set; }
			[Column(DataType = DataType.VarChar)] public InvalidColumnIndexMappingEnum1 Enum1 { get; set; }
		}

		[Table]
		public class InvalidColumnIndexMappingTable4
		{
			[PrimaryKey] public Guid Id { get; set; }
		}

		private record InvalidColumnIndexMappingRecord(Guid Id, Guid? ChildId, InvalidColumnIndexMappingEnum1? Enum1, InvalidColumnIndexMappingEnum2? Enum2);

		[Test(Description = "LinqToDBConvertException : Cannot convert value 'ENUM1_VALUE: System.String' to type 'Tests.Linq.CteTests+InvalidColumnIndexMappingEnum2'")]
		public void Issue3360_InvalidColumnIndexMapping([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = GetDataContext(context);

			using var table1 = db.CreateLocalTable<InvalidColumnIndexMappingTable1>();
			using var table2 = db.CreateLocalTable<InvalidColumnIndexMappingTable2>();
			using var table3 = db.CreateLocalTable<InvalidColumnIndexMappingTable3>();
			using var table4 = db.CreateLocalTable<InvalidColumnIndexMappingTable4>();

			db.Insert(new InvalidColumnIndexMappingTable1() { Id = TestData.Guid1, ChildId = TestData.Guid2 });
			db.Insert(new InvalidColumnIndexMappingTable4() { Id = TestData.Guid2 });

			var query = from node in db.GetCte<InvalidColumnIndexMappingRecord>(cte =>
			{
				return table1
							.Select(s => new InvalidColumnIndexMappingRecord(s.Id, s.ChildId, null, null))
							.Concat(
								from t1 in table2
								join t3 in table3 on t1.Id equals t3.Id
								join parent in cte on t1.Id equals parent.ChildId
								select new InvalidColumnIndexMappingRecord(t1.Id, null, t3.Enum1, t1.Enum2))
							.Concat(
								from t4 in table4
								join parent in cte on t4.Id equals parent.ChildId
								select new InvalidColumnIndexMappingRecord(t4.Id, null, InvalidColumnIndexMappingEnum1.Value, null))
							;
			})
				join t2 in table2 on node.Id equals t2.Id into t2records
				from table in t2records.DefaultIfEmpty()
				select new InvalidColumnIndexMappingRecord(node.Id, node.ChildId, node.Enum1, node.Enum2);

			var res = query.ToArray();
		}

		[Table]
		public class Issue3360Table1
		{
			[PrimaryKey] public int                             Id    { get; set; }
			[Column    ] public byte                            Byte  { get; set; }
			[Column    ] public byte?                           ByteN { get; set; }
			[Column    ] public Guid                            Guid  { get; set; }
			[Column    ] public Guid?                           GuidN { get; set; }
			[Column    ] public InvalidColumnIndexMappingEnum1  Enum  { get; set; }
			[Column    ] public InvalidColumnIndexMappingEnum2? EnumN { get; set; }
			[Column    ] public bool                            Bool  { get; set; }
			[Column    ] public bool?                           BoolN { get; set; }

			public static Issue3360Table1[] Items = new[]
			{
				new Issue3360Table1() { Id = 1 },
				new Issue3360Table1() { Id = 2, Byte = 1, ByteN = 2, Guid = TestData.Guid1, GuidN = TestData.Guid2, Enum = InvalidColumnIndexMappingEnum1.Value, EnumN = InvalidColumnIndexMappingEnum2.Value, Bool = true, BoolN = false },
				new Issue3360Table1() { Id = 4, Byte = 3, ByteN = 4, Guid = TestData.Guid3, GuidN = TestData.Guid1, Enum = InvalidColumnIndexMappingEnum1.Value, EnumN = InvalidColumnIndexMappingEnum2.Value, Bool = false, BoolN = true },
			};
		}

		private record Issue3360NullsRecord(int Id, byte? Byte, byte? ByteN, Guid? Guid, Guid? GuidN, InvalidColumnIndexMappingEnum1? Enum, InvalidColumnIndexMappingEnum2? EnumN, bool? Bool, bool? BoolN);

		[Test(Description = "null literals in anchor query (for known problematic types)")]
		public void Issue3360_NullsInAnchor([CteContextSource] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Issue3360Table1.Items);

			var query = from record in db.GetCte<Issue3360NullsRecord>(cte =>
			{
				return table.Where(r => r.Id == 1)
							.Select(r => new Issue3360NullsRecord(r.Id, null, null, null, null, null, null, null, null))
							.Concat(
								from r in table
								join parent in cte on r.Id equals parent.Id + 1
								select new Issue3360NullsRecord(r.Id, r.Byte, r.ByteN, r.Guid, r.GuidN, r.Enum, r.EnumN, r.Bool, r.BoolN));
			})
						orderby record.Id
						select record;

			var data = query.ToArray();

			Assert.AreEqual(2, data.Length);

			Assert.AreEqual(1, data[0].Id);
			Assert.IsNull(data[0].Byte);
			Assert.IsNull(data[0].ByteN);
			Assert.IsNull(data[0].Guid);
			Assert.IsNull(data[0].GuidN);
			Assert.IsNull(data[0].Enum);
			Assert.IsNull(data[0].EnumN);
			Assert.IsNull(data[0].Bool);
			Assert.IsNull(data[0].BoolN);

			Assert.AreEqual(2, data[1].Id);
			Assert.AreEqual(1, data[1].Byte);
			Assert.AreEqual(2, data[1].ByteN);
			Assert.AreEqual(TestData.Guid1, data[1].Guid);
			Assert.AreEqual(TestData.Guid2, data[1].GuidN);
			Assert.AreEqual(InvalidColumnIndexMappingEnum1.Value, data[1].Enum);
			Assert.AreEqual(InvalidColumnIndexMappingEnum2.Value, data[1].EnumN);
			Assert.AreEqual(true, data[1].Bool);
			Assert.AreEqual(false, data[1].BoolN);
		}

		[Test(Description = "double columns in anchor query")]
		public void Issue3360_DoubleColumnSelection([CteContextSource] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Issue3360Table1.Items);

			var query = from record in db.GetCte<Issue3360NullsRecord>(cte =>
			{
				return table.Where(r => r.Id == 2)
							.Select(r => new Issue3360NullsRecord(r.Id, r.Byte, r.Byte, r.Guid, r.Guid, null, null, r.Bool, r.Bool))
							.Concat(
								from r in table
								join parent in cte on r.Id equals parent.Id + 2
								select new Issue3360NullsRecord(r.Id, r.Byte, r.ByteN, r.Guid, r.GuidN, r.Enum, r.EnumN, r.Bool, r.BoolN));
			})
						orderby record.Id
						select record;

			var data = query.ToArray();

			Assert.AreEqual(2, data.Length);

			Assert.AreEqual(2, data[0].Id);
			Assert.AreEqual(1, data[0].Byte);
			Assert.AreEqual(1, data[0].ByteN);
			Assert.AreEqual(TestData.Guid1, data[0].Guid);
			Assert.AreEqual(TestData.Guid1, data[0].GuidN);
			Assert.IsNull(data[0].Enum);
			Assert.IsNull(data[0].EnumN);
			Assert.AreEqual(true, data[0].Bool);
			Assert.AreEqual(true, data[0].BoolN);

			Assert.AreEqual(4, data[1].Id);
			Assert.AreEqual(3, data[1].Byte);
			Assert.AreEqual(4, data[1].ByteN);
			Assert.AreEqual(TestData.Guid3, data[1].Guid);
			Assert.AreEqual(TestData.Guid1, data[1].GuidN);
			Assert.AreEqual(InvalidColumnIndexMappingEnum1.Value, data[1].Enum);
			Assert.AreEqual(InvalidColumnIndexMappingEnum2.Value, data[1].EnumN);
			Assert.AreEqual(false, data[1].Bool);
			Assert.AreEqual(true, data[1].BoolN);
		}

		[Test(Description = "literals in anchor query")]
		public void Issue3360_LiteralsInAnchor([CteContextSource] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Issue3360Table1.Items);

			var query = from record in db.GetCte<Issue3360NullsRecord>(cte =>
			{
				return table.Where(r => r.Id == 2)
							.Select(r => new Issue3360NullsRecord(r.Id, 5, 5, new Guid("0B8AFE27-481C-442E-B8CF-729DDFEECE29"), new Guid("0B8AFE27-481C-442E-B8CF-729DDFEECE30"), InvalidColumnIndexMappingEnum1.Value, InvalidColumnIndexMappingEnum2.Value, true, false))
							.Concat(
								from r in table
								join parent in cte on r.Id equals parent.Id + 2
								select new Issue3360NullsRecord(r.Id, r.Byte, r.ByteN, r.Guid, r.GuidN, r.Enum, r.EnumN, r.Bool, r.BoolN));
			})
						orderby record.Id
						select record;

			var data = query.ToArray();

			Assert.AreEqual(2, data.Length);

			Assert.AreEqual(2, data[0].Id);
			Assert.AreEqual(5, data[0].Byte);
			Assert.AreEqual(5, data[0].ByteN);
			Assert.AreEqual(new Guid("0B8AFE27-481C-442E-B8CF-729DDFEECE29"), data[0].Guid);
			Assert.AreEqual(new Guid("0B8AFE27-481C-442E-B8CF-729DDFEECE30"), data[0].GuidN);
			Assert.AreEqual(InvalidColumnIndexMappingEnum1.Value, data[0].Enum);
			Assert.AreEqual(InvalidColumnIndexMappingEnum2.Value, data[0].EnumN);
			Assert.AreEqual(true, data[0].Bool);
			Assert.AreEqual(false, data[0].BoolN);

			Assert.AreEqual(4, data[1].Id);
			Assert.AreEqual(3, data[1].Byte);
			Assert.AreEqual(4, data[1].ByteN);
			Assert.AreEqual(TestData.Guid3, data[1].Guid);
			Assert.AreEqual(TestData.Guid1, data[1].GuidN);
			Assert.AreEqual(InvalidColumnIndexMappingEnum1.Value, data[1].Enum);
			Assert.AreEqual(InvalidColumnIndexMappingEnum2.Value, data[1].EnumN);
			Assert.AreEqual(false, data[1].Bool);
			Assert.AreEqual(true, data[1].BoolN);
		}

		#endregion

		[Test(Description = "Test that we type non-field union column properly")]
		public void Issue2451_ComplexColumn([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = GetDataContext(context);

			var query = db.GetCte<Person>(cte =>
			{
				return db.Person.Select(p => new Person() { FirstName = p.FirstName })
				.Concat(
					from p in cte
					join r in db.Person on p.FirstName equals r.LastName
					select new Person() { FirstName = r.FirstName + '/' + r.LastName }
					);
			});

			query.ToArray();

			if (db is TestDataConnection dc)
			{
				dc.LastQuery!.Should().NotContain("Convert(VarChar");
				dc.LastQuery!.ToUpperInvariant().Should().Contain("AS NVARCHAR(MAX))", Exactly.Twice());
			}
		}

		[Test(Description = "Test that other providers work")]
		public void Issue2451_ComplexColumn_All([CteContextSource(ProviderName.DB2)] string context)
		{
			using var db = GetDataContext(context);

			var query = db.GetCte<Person>(cte =>
			{
				return db.Person.Select(p => new Person() { FirstName = p.FirstName })
				.Concat(
					from p in cte
					join r in db.Person on p.FirstName equals r.LastName
					select new Person() { FirstName = r.FirstName + '/' + r.LastName }
					);
			});

			query.ToArray();
		}

		[Test(Description = "Test that other providers work")]
		public void Issue2451_ComplexColumn_DB2([IncludeDataSources(true, ProviderName.DB2)] string context)
		{
			using var db = GetDataContext(context);

			var query = db.GetCte<Person>(cte =>
			{
				return db.Person.Select(p => new Person() { FirstName = p.FirstName })
				.Concat(
					from p in cte
					from r in db.Person
					where p.FirstName == r.LastName
					select new Person() { FirstName = r.FirstName + '/' + r.LastName }
					);
			});

			query.ToArray();
		}

		class CteEntity<TEntity> where TEntity : class
		{
			public TEntity Entity   { get; set; } = null!;
			public Guid    Id       { get; set; }
			public Guid?   ParentId { get; set; }
			public int     Level    { get; set; }
			public string? Label    { get; set; }
		}

		[Table]
		class TestFolder
		{
			[Column] public Guid        Id       { get; set; }
			[Column] public string?     Label    { get; set; }
			[Column] public Guid?       ParentId { get; set; }

			[Association(ThisKey = nameof(ParentId), OtherKey = nameof(Id))]
			public TestFolder? Parent { get; set; }
		}

		[ActiveIssue(2264)]
		[Test(Description = "Recursive common table expression 'CTE' does not contain a top-level UNION ALL operator.")]
		public void Issue2264([CteContextSource] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<TestFolder>();

			var query = db.GetCte<CteEntity<TestFolder>>("CTE", cte =>
			{
				return (tb
					.Where(c => c.ParentId == null)
					.Select(c =>
						new CteEntity<TestFolder>()
						{
							Level     = 0,
							Id        = c.Id,
							ParentId  = c.ParentId,
							Label     = c.Label,
							Entity    = c
						}))
				.Concat(tb
					.SelectMany(c => cte.InnerJoin(r => c.ParentId == r.Id),
						(c, r)    => new CteEntity<TestFolder>
						{
							Level    = r.Level + 1,
							Id       = c.Id,
							ParentId = c.ParentId,
							Label    = r.Label + '/' + c.Label,
							Entity   = c
						}));
			});

			query.ToArray();
		}
	}
}
