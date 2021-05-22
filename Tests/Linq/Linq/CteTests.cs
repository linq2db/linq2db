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

		[ActiveIssue("Scalar recursive CTE are not working")]
		[Test]
		public void TestRecursiveScalar([IncludeDataSources(TestProvName.AllSQLite)] string context)
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

		[ActiveIssue(1644)]
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

	}
}
