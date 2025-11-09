using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Internal.Common;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

using Tests.Model;

namespace Tests.Linq
{
	public class CteTests : TestBase
	{
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
		public void Test2([CteContextSource(TestProvName.AllClickHouse)] string context)
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

		// MariaDB allows CTE ordering but do not respect it
		[Test]
		public void WithOrderBy([CteContextSource(TestProvName.AllMariaDB)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var query = db.Parent
					.OrderByDescending(p => p.ParentID)
					.AsCte();

				if (!db.SqlProviderFlags.IsCTESupportsOrdering)
				{
					var act = () => query.ToArray();
					act.ShouldNotThrow();
				}
				else
				{
					var result = query.ToList();

					var expected = Parent
						.OrderByDescending(p => p.ParentID)
						.ToList();

					AreSame(expected, result);
				}
			}
		}

		[Test]
		public void WithLimitedOrderBy([CteContextSource] string context)
		{
			using (var db = GetDataContext(context))
			{
				var query = db.Parent
					.OrderByDescending(p => p.ParentID)
					.Take(3)
					.AsCte();

				var result = query.ToList();

				var expected = Parent
					.OrderByDescending(p => p.ParentID)
					.Take(3)
					.ToList();

				AreSame(expected, result);
			}
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
						NumberOfSubordinates = db.Employee.Where(e2 => e2.ReportsTo == e.ReportsTo && e2.ReportsTo != null).Count(),
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

		sealed class EmployeeHierarchyCTE
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

				Assert.That(query.Count(), Is.EqualTo(expected.Count()));
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

				Assert.That(actual, Is.EqualTo(expected));
			}
		}

		private sealed class CteDMLTests
		{
			private bool Equals(CteDMLTests other)
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
				return HashCode.Combine(ChildID, ParentID);
			}

			[PrimaryKey]
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

				Assert.That(cnt1, Is.EqualTo(expected));

				var query = db.GetTable<Child>().Select(c => new { C = new { c.ChildID }});
				var cte2 = query.AsCte("CTE1_");
				var cnt2 = cte2.Count();

				Assert.That(cnt2, Is.EqualTo(expected));

				var any  = cte2.Any();

				Assert.That(any, Is.True);
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
				var str = query.ToSqlQuery().Sql;

				query.ToArray();

				if (context.IsAnyOf(ProviderName.Ydb))
					Assert.That(str, Does.Contain("$CTE"));
				else
					Assert.That(str, Does.Contain("WITH"));
			}
		}

		[ActiveIssue(3015, Configurations = [TestProvName.AllSapHana, ProviderName.InformixDB2])]
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
		[ActiveIssue(3015, Configurations = [TestProvName.AllSapHana, ProviderName.InformixDB2])]
		[Test]
		public void TestDelete([CteContextSource(TestProvName.AllFirebird, ProviderName.DB2, TestProvName.AllMariaDB, TestProvName.AllClickHouse)] string context)
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
				Assert.That(recordsAffected, Is.EqualTo(5));
			}
		}

		// MariaDB support expected in v10.6 : https://jira.mariadb.org/browse/MDEV-18511
		[ActiveIssue(3015, Configurations = [TestProvName.AllOracle, TestProvName.AllSapHana, ProviderName.InformixDB2], Details = "Oracle needs special syntax for CTE + UPDATE")]
		[Test]
		public void TestUpdate(
			[CteContextSource(TestProvName.AllFirebird, ProviderName.DB2, TestProvName.AllClickHouse, TestProvName.AllOracle, TestProvName.AllMariaDB)]
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

		sealed class RecursiveCTE
		{
			public int? ParentID     { get; set; }
			public int? ChildID      { get; set; }
			public int? GrandChildID { get; set; }
		}

		[Test]
		public void RecursiveTest([RecursiveCteContextSource(true, ProviderName.DB2, TestProvName.AllInformix)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var cteRecursive = db.GetCte<RecursiveCTE>(cte =>
						(
							from gc1 in db.GrandChild
							select new RecursiveCTE
							{
								ChildID      = gc1.ChildID,
								ParentID     = gc1.GrandChildID,
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
								ChildID      = ct.ChildID,
								ParentID     = ct.ParentID,
								GrandChildID = ct.ChildID + 1
							}
						)
					, "MY_CTE");

				var result = cteRecursive.ToArray();
			}
		}

		public class HierarchyTree
		{
			[PrimaryKey]
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

		sealed class HierarchyData
		{
			[PrimaryKey]
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
		public void RecursiveTest2([RecursiveCteContextSource(true, ProviderName.DB2)] string context)
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
		public void TestDoubleRecursion([RecursiveCteContextSource(true, ProviderName.DB2)] string context)
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

				Assert.That(count, Is.GreaterThan(0));
			}
		}

		[Test]
		public void RecursiveCount([RecursiveCteContextSource(true, ProviderName.DB2)] string context)
		{
			var hierarchyData = GeHirarchyData();

			using (var db = GetDataContext(context))
			using (var tree = db.CreateLocalTable(hierarchyData))
			{
				var hierarchy = GetHierarchyDown(tree, db);
				var expected = EnumerateDown(hierarchyData, 0, null);

				Assert.That(hierarchy.Count(), Is.EqualTo(expected.Count()));
			}
		}

		[ActiveIssue(3015, Configurations = [ProviderName.InformixDB2])]
		[Test]
		public void RecursiveInsertInto([RecursiveCteContextSource(true, ProviderName.DB2)] string context)
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
		public void RecursiveDeepNesting([RecursiveCteContextSource(true, TestProvName.AllDB2)] string context)
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

				query.ToArray();
			}
		}

		private sealed class TestWrapper
		{
			public Child? Child { get; set; }

			private bool Equals(TestWrapper other)
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

		private sealed class TestWrapper2
		{
			public Child?  Child   { get; set; }
			public Parent? Parent { get; set; }

			private bool Equals(TestWrapper2 other)
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
				return HashCode.Combine(Child, Parent);
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

				Assert.That(result.OrderBy(_ => _.p.ParentID).ThenBy(_ => _.c.Child?.ChildID), Is.EqualTo(expected.ToList().OrderBy(_ => _.p.ParentID).ThenBy(_ => _.c.Child?.ChildID)));
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

				query.ToArray();
				var sql = query.ToSqlQuery().Sql;

				Assert.That(sql, Is.Not.Contains("WITH"));
			}
		}

		[Test]
		public void TestRecursiveScalar([RecursiveCteContextSource] string context)
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

		sealed class OrgGroupDepthWrapper
		{
			public OrgGroup? OrgGroup { get; set; }
			public int Depth { get; set; }
		}

		sealed class OrgGroup
		{
			[PrimaryKey]
			public int Id { get; set; }
			public int ParentId { get; set; }
			public string? GroupName { get; set; }
		}

		[Test]
		public void TestRecursiveObjects([IncludeDataSources(TestProvName.AllPostgreSQL, TestProvName.AllSQLite, TestProvName.AllMySqlWithCTE)] string context)
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
			[PrimaryKey] public int Id { get; set; }
			public string? Property1 { get; set; }
		}

		class NestingB : NestingA
		{
			public string? Property2 { get; set; }
		}

		sealed class NestingC : NestingB
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
		public void Issue2029Test([CteContextSource(TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context, o => o.UseGenerateFinalAliases(true)))
			using (db.CreateLocalTable<NcCode>())
			using (db.CreateLocalTable<NcGroupMember>())
			{
				var wipCte = new WipCte(db);

				var ncCodeBo = "NCCodeBO:8110,SETUP_OSCILLOSCO";

				var result = from item in wipCte.AllowedNcCode() where item.NcCodeBo == ncCodeBo select item;
				result.ToArray();

				var sql = result.ToSqlQuery().Sql;

				Assert.That(sql.Replace("\"", "").Replace("`", "").Replace("[", "").Replace("]", "").ToLowerInvariant(), Does.Contain("WITH AllowedNcCode (NcCodeBo, NcCode, NcCodeDescription)".ToLowerInvariant()));
			}
		}

		internal sealed class WipCte
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

			internal sealed class AllowedNcCodeOutput
			{
				internal string? NcCodeBo          { get; set; }
				internal string? NcCode            { get; set; }
				internal string? NcCodeDescription { get; set; }
			}
		}

		[Table(Name = "NC_CODE")]
		public partial class NcCode
		{
			[PrimaryKey] public int Id { get; set; }
			[Column("HANDLE"), NotNull             ] public string    Handle           { get; set; } = null!; // NVARCHAR2(1236)
			[Column("CHANGE_STAMP"), Nullable      ] public decimal?  ChangeStamp      { get; set; } // NUMBER (38,0)
			[Column("SITE", Length = 18),          ] public string?   Site             { get; set; } // NVARCHAR2(18)
			[Column("NC_CODE", Length = 48),       ] public string?   NcCodeColumn     { get; set; } // NVARCHAR2(48)
			[Column("DESCRIPTION", Length = 120)   ] public string?   Description      { get; set; } // NVARCHAR2(120)
			[Column("STATUS_BO")                   ] public string?   StatusBo         { get; set; } // NVARCHAR2(1236)
			[Column("CREATED_DATE_TIME"), Nullable ] public DateTime? CreatedDateTime  { get; set; } // DATE
			[Column("MODIFIED_DATE_TIME"), Nullable] public DateTime? ModifiedDateTime { get; set; } // DATE
			[Column("NC_CATEGORY"), Nullable       ] public string?   NcCategory       { get; set; } // NVARCHAR2(60)
			[Column("DPMO_CATEGORY_BO"), Nullable  ] public string?   DpmoCategoryBo   { get; set; } // NVARCHAR2(1236)
		}
		[Table(Name = "NC_GROUP_MEMBER")]
		public partial class NcGroupMember
		{
			[PrimaryKey] public int Id { get; set; }
			[Column("HANDLE"), NotNull               ] public string   Handle           { get; set; } = null!; // NVARCHAR2(1236)
			[Column("NC_GROUP_BO"), Nullable         ] public string?  NcGroupBo        { get; set; } // NVARCHAR2(1236)
			[Column("NC_CODE_OR_GROUP_GBO"), Nullable] public string?  NcCodeOrGroupGbo { get; set; } // NVARCHAR2(1236)
			[Column("SEQUENCE"), Nullable            ] public decimal? Sequence         { get; set; } // NUMBER (38,0)
		}
		#endregion

		private sealed class Issue3359Projection
		{
			public string FirstName { get; set; } = null!;
			public string LastName  { get; set; } = null!;
		}

		[Test(Description = "Test that we generate plain UNION without sub-queries (or query will be invalid)")]
		public void Issue3359_MultipleSets([RecursiveCteContextSource(
			TestProvName.AllInformix,
			TestProvName.AllOracle, // too many unions (ORA-32041: UNION ALL operation in recursive WITH clause must have only two branches)
			TestProvName.AllPostgreSQL, // too many joins? (42P19: recursive reference to query "cte" must not appear within its non-recursive term)
			ProviderName.DB2 // joins (SQL0345N  The fullselect of the recursive common table expression "cte" must be the UNION of two or more fullselects and cannot include column functions, GROUP BY clause, HAVING clause, ORDER BY clause, or an explicit join including an ON clause.)
			)] string context)
		{
			if (context.IsAnyOf(TestProvName.AllSQLite))
			{
				using var dc = (TestDataConnection)GetDataContext(context.StripRemote());
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
				cn.LastQuery!.ShouldContain("SELECT", Exactly.Times(4));
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
		public void Issue3357_RecordClass([RecursiveCteContextSource] string context)
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
		public void Issue3357_RecordLikeClass([RecursiveCteContextSource] string context)
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
				dc.LastQuery!.ShouldNotContain("N'");
				dc.LastQuery!.ToUpperInvariant().ShouldContain("AS VARCHAR(MAX))");
				dc.LastQuery!.ToUpperInvariant().ShouldContain("AS VARCHAR(MAX))", Exactly.Once());
			}
		}

		[ActiveIssue]
		[Test(Description = "Test that we don't need typing for non-sqlserver providers")]
		public void Issue3360_TypeByOtherQuery_DB2([IncludeDataSources(true, ProviderName.DB2)] string context)
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
		public void Issue3360_TypeByOtherQuery_AllProviders([RecursiveCteContextSource(ProviderName.DB2)] string context)
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
			[PrimaryKey                                      ] public int     Id  { get; set; }
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
				dc.LastQuery!.ShouldNotContain("N'");
				dc.LastQuery!.ShouldContain("'THIS_IS_TWO'");
				dc.LastQuery!.ToUpperInvariant().ShouldContain("AS VARCHAR(50))", Exactly.Once());
			}
		}

		[Test(Description = "Test that we don't need typing for non-sqlserver providers")]
		public void Issue3360_TypeStringEnum_AllProviders([RecursiveCteContextSource(ProviderName.DB2)] string context)
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
				dc.LastQuery!.ShouldNotContain("N'");
				dc.LastQuery!.ToUpperInvariant().ShouldContain("AS VARCHAR(MAX))", Exactly.Twice());
			}
		}

		[Test(Description = "Test that we don't need typing for non-sqlserver providers")]
		public void Issue3360_TypeByProjectionProperty_AllProviders([RecursiveCteContextSource(ProviderName.DB2)] string context)
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

		[ActiveIssue]
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
			[PrimaryKey                                      ] public int       Id    { get; set; }
			[NotColumn(Configuration = ProviderName.Firebird)]
			[NotColumn(Configuration = ProviderName.DB2)     ]
			[Column                                          ] public Guid?     Guid  { get; set; }
			[Column(DataType = DataType.VarChar, Length = 50)] public StrEnum?  Enum1 { get; set; }
		}

		[ActiveIssue(Configurations = [TestProvName.AllSqlServer])]
		[Test(Description = "Test CTE columns typing")]
		public void Issue3360_NullGuidInAnchor([RecursiveCteContextSource(TestProvName.AllFirebird, ProviderName.DB2)] string context)
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

		[ActiveIssue(Configurations = [TestProvName.AllSqlServer])]
		[Test(Description = "Test CTE columns typing")]
		public void Issue3360_NullEnumInAnchor([RecursiveCteContextSource(ProviderName.DB2)] string context)
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

		[Sql.Expression("CAST({0} AS VARCHAR({1}))", ServerSideOnly = true, IsNullable = Sql.IsNullableType.Nullable, IgnoreGenericParameters = true)]
		static T VarChar<T>(T value, int size)
		{
			throw new ServerSideOnlyException(nameof(VarChar));
		}

		private sealed record Issue3360_TypedNullEnumInAnchorProjection(int Id, StrEnum? Value);

		[Test(Description = "Test CTE columns typing")]
		public void Issue3360_TypedNullEnumInAnchor([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<Issue3360NullInAnchor>();

			var query = from node in db.GetCte<Issue3360_TypedNullEnumInAnchorProjection>(cte =>
			{
				return db.GetTable<Issue3360NullInAnchor>().Select(p => new Issue3360_TypedNullEnumInAnchorProjection(p.Id, VarChar<StrEnum?>(null, 10)))
				.Concat(
					from p in db.GetTable<Issue3360NullInAnchor>()
					select new Issue3360_TypedNullEnumInAnchorProjection(p.Id, VarChar(StrEnum.One, 10))
					);
			})
				select new Issue3360_TypedNullEnumInAnchorProjection(node.Id, node.Value);
			;

			query.ToArray();
		}

		[ActiveIssue]
		[Test]
		public void Issue3360_TypedNullEnumInAnchor2([IncludeDataSources(false, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataConnection(context);

			using var tb = db.CreateLocalTable<Issue3360NullInAnchor>();

			var query = from node in db.GetCte<Issue3360_TypedNullEnumInAnchorProjection>(
				cte =>
				{
					return db.GetTable<Issue3360NullInAnchor>().Select(p => new Issue3360_TypedNullEnumInAnchorProjection(p.Id, VarChar<StrEnum?>(null, 50)))
						.Concat(
						from p in db.GetTable<Issue3360NullInAnchor>()
						join parent in cte on p.Id equals parent.Id + 1
						select new Issue3360_TypedNullEnumInAnchorProjection(p.Id, StrEnum.One))
						.Concat(
						from p in db.GetTable<Issue3360NullInAnchor>()
						select new Issue3360_TypedNullEnumInAnchorProjection(p.Id, p.Enum1))
						.Concat(
						from p in db.GetTable<Issue3360NullInAnchor>()
						select new Issue3360_TypedNullEnumInAnchorProjection(p.Id, null));
				})
						select new Issue3360_TypedNullEnumInAnchorProjection(node.Id, node.Value);

			query.ToArray();

			Assert.That(db.LastQuery!.ToUpper(), Does.Not.Contain("SQL_VARIANT"));
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

		[ActiveIssue]
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

		[ActiveIssue(3015, Configurations = [TestProvName.AllClickHouse, TestProvName.AllFirebird, TestProvName.AllMySql, TestProvName.AllSqlServer])]
		[Test(Description = "null literals in anchor query (for known problematic types)")]
		public void Issue3360_NullsInAnchor([RecursiveCteContextSource] string context)
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

			Assert.That(data, Has.Length.EqualTo(2));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(data[0].Id, Is.EqualTo(1));
				Assert.That(data[0].Byte, Is.Null);
				Assert.That(data[0].ByteN, Is.Null);
				Assert.That(data[0].Guid, Is.Null);
				Assert.That(data[0].GuidN, Is.Null);
				Assert.That(data[0].Enum, Is.Null);
				Assert.That(data[0].EnumN, Is.Null);
				Assert.That(data[0].Bool, Is.Null);
				Assert.That(data[0].BoolN, Is.Null);

				Assert.That(data[1].Id, Is.EqualTo(2));
				Assert.That(data[1].Byte, Is.EqualTo(1));
				Assert.That(data[1].ByteN, Is.EqualTo(2));
				Assert.That(data[1].Guid, Is.EqualTo(TestData.Guid1));
				Assert.That(data[1].GuidN, Is.EqualTo(TestData.Guid2));
				Assert.That(data[1].Enum, Is.EqualTo(InvalidColumnIndexMappingEnum1.Value));
				Assert.That(data[1].EnumN, Is.EqualTo(InvalidColumnIndexMappingEnum2.Value));
				Assert.That(data[1].Bool, Is.True);
				Assert.That(data[1].BoolN, Is.False);
			}
		}

		[ActiveIssue(3015, Configurations = [TestProvName.AllClickHouse, TestProvName.AllFirebird, TestProvName.AllMySql, TestProvName.AllSqlServer])]
		[Test(Description = "double columns in anchor query")]
		public void Issue3360_DoubleColumnSelection([RecursiveCteContextSource] string context)
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

			Assert.That(data, Has.Length.EqualTo(2));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(data[0].Id, Is.EqualTo(2));
				Assert.That(data[0].Byte, Is.EqualTo(1));
				Assert.That(data[0].ByteN, Is.EqualTo(1));
				Assert.That(data[0].Guid, Is.EqualTo(TestData.Guid1));
				Assert.That(data[0].GuidN, Is.EqualTo(TestData.Guid1));
				Assert.That(data[0].Enum, Is.Null);
				Assert.That(data[0].EnumN, Is.Null);
				Assert.That(data[0].Bool, Is.True);
				Assert.That(data[0].BoolN, Is.True);

				Assert.That(data[1].Id, Is.EqualTo(4));
				Assert.That(data[1].Byte, Is.EqualTo(3));
				Assert.That(data[1].ByteN, Is.EqualTo(4));
				Assert.That(data[1].Guid, Is.EqualTo(TestData.Guid3));
				Assert.That(data[1].GuidN, Is.EqualTo(TestData.Guid1));
				Assert.That(data[1].Enum, Is.EqualTo(InvalidColumnIndexMappingEnum1.Value));
				Assert.That(data[1].EnumN, Is.EqualTo(InvalidColumnIndexMappingEnum2.Value));
				Assert.That(data[1].Bool, Is.False);
				Assert.That(data[1].BoolN, Is.True);
			}
		}

		[ActiveIssue(Configurations = [TestProvName.AllSQLite])]
		[Test(Description = "literals in anchor query")]
		public void Issue3360_LiteralsInAnchor([RecursiveCteContextSource(TestProvName.AllInformix)] string context)
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

			Assert.That(data, Has.Length.EqualTo(2));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(data[0].Id, Is.EqualTo(2));
				Assert.That(data[0].Byte, Is.EqualTo(5));
				Assert.That(data[0].ByteN, Is.EqualTo(5));
				Assert.That(data[0].Guid, Is.EqualTo(new Guid("0B8AFE27-481C-442E-B8CF-729DDFEECE29")));
				Assert.That(data[0].GuidN, Is.EqualTo(new Guid("0B8AFE27-481C-442E-B8CF-729DDFEECE30")));
				Assert.That(data[0].Enum, Is.EqualTo(InvalidColumnIndexMappingEnum1.Value));
				Assert.That(data[0].EnumN, Is.EqualTo(InvalidColumnIndexMappingEnum2.Value));
				Assert.That(data[0].Bool, Is.True);
				Assert.That(data[0].BoolN, Is.False);

				Assert.That(data[1].Id, Is.EqualTo(4));
				Assert.That(data[1].Byte, Is.EqualTo(3));
				Assert.That(data[1].ByteN, Is.EqualTo(4));
				Assert.That(data[1].Guid, Is.EqualTo(TestData.Guid3));
				Assert.That(data[1].GuidN, Is.EqualTo(TestData.Guid1));
				Assert.That(data[1].Enum, Is.EqualTo(InvalidColumnIndexMappingEnum1.Value));
				Assert.That(data[1].EnumN, Is.EqualTo(InvalidColumnIndexMappingEnum2.Value));
				Assert.That(data[1].Bool, Is.False);
				Assert.That(data[1].BoolN, Is.True);
			}
		}

		#endregion

		[ActiveIssue]
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
				dc.LastQuery!.ShouldNotContain("Convert(VarChar");
				dc.LastQuery!.ToUpperInvariant().ShouldContain("AS NVARCHAR(MAX))", Exactly.Twice());
			}
		}

		[ActiveIssue(Configurations = [TestProvName.AllPostgreSQL, TestProvName.AllSqlServer])]
		[Test(Description = "Test that other providers work")]
		public void Issue2451_ComplexColumn_All([RecursiveCteContextSource(ProviderName.DB2)] string context)
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

		sealed class CteEntity<TEntity> where TEntity : class
		{
			public TEntity Entity   { get; set; } = null!;
			public Guid    Id       { get; set; }
			public Guid?   ParentId { get; set; }
			public int     Level    { get; set; }
			public string? Label    { get; set; }
		}

		[Table]
		sealed class TestFolder
		{
			[PrimaryKey] public Guid    Id       { get; set; }
			[Column] public string?     Label    { get; set; }
			[Column] public Guid?       ParentId { get; set; }

			[Association(ThisKey = nameof(ParentId), OtherKey = nameof(Id))]
			public TestFolder? Parent { get; set; }
		}

		[Test(Description = "Recursive common table expression 'CTE' does not contain a top-level UNION ALL operator.")]
		public void Issue2264([RecursiveCteContextSource] string context)
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

		[ActiveIssue(3015, Configurations = [TestProvName.AllSapHana, ProviderName.InformixDB2])]
		[Test]
		public void Issue3945([CteContextSource] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<TestFolder>();

			var cte = db.GetCte<TestFolder>("CTE", cte => tb.Where(c => c.ParentId != null));
			var join = from child in cte
					   join parent in tb on child.ParentId equals parent.Id
					   select new TestFolder
					   {
						   Id = TestData.Guid1,
						   Label = parent.Label + "/" + child.Label,
					   };
			join.Insert(tb, x => x);
		}

		[Table]
		private class Issue4167Table
		{
			[PrimaryKey] public int      ID        { get; set; }
			[Column    ] public string?  Value     { get; set; }
			[Column    ] public TaxType? EnumValue { get; set; }

			public enum TaxType
			{
				NoTax       = 0,
				NonResident = 3,
			}

			public static readonly Issue4167Table[] Data = new []
			{
				new Issue4167Table() { ID = 1, Value = "000001", EnumValue = TaxType.NoTax },
				new Issue4167Table() { ID = 2, Value = "000001", EnumValue = TaxType.NonResident },
				new Issue4167Table() { ID = 3, Value = "000001", EnumValue = null },
				new Issue4167Table() { ID = 4, Value = "000002", EnumValue = TaxType.NoTax },
			};
		}

		[Test]
		public void Issue4167([CteContextSource] string context, [Values] bool withCte)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(Issue4167Table.Data);

			var query = (
				from t in tb
				where t.Value == "000001"
				group t by new { t.Value, t.EnumValue } into g
				select new
				{
					EnumValue = g.Key.EnumValue.GetValueOrDefault(),
				});

			if (withCte)
				query = query.AsCte();

			var result = (
				from r in query
				select new
				{
					r.EnumValue
				}).OrderBy(r => r.EnumValue)
				.ToList();

			Assert.That(result, Has.Count.EqualTo(3));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(result[0].EnumValue, Is.EqualTo(Issue4167Table.TaxType.NoTax));
				Assert.That(result[1].EnumValue, Is.EqualTo(Issue4167Table.TaxType.NoTax));
				Assert.That(result[2].EnumValue, Is.EqualTo(Issue4167Table.TaxType.NonResident));
			}
		}

		[Test]
		public void Issue2145Test1([CteContextSource] string context)
		{
			using var db = GetDataContext(context);

			var persons = new Person[]
			{
				new Person() { ID = 10, FirstName = "FN1", LastName = "LN1", Gender = Gender.Male },
				new Person() { ID = 11, FirstName = "FN2", Gender = Gender.Female },
			};

			var cte = persons.AsQueryable().AsCte();

			var query = from p in cte
						where p.ID == 11
						select p;

			Assert.That(() => query.ToArray(), Throws.InvalidOperationException);
		}

		[Test]
		public void Issue2145Test2([CteContextSource] string context)
		{
			using var db = GetDataContext(context);

			var persons = new Person[]
			{
				new Person() { ID = 10, FirstName = "FN1", LastName = "LN1", Gender = Gender.Male },
				new Person() { ID = 11, FirstName = "FN2", Gender = Gender.Female },
			};

			var cte = persons.AsQueryable(db).AsCte();

			var query = from p in cte
						where p.ID == 11
						select p;

			var result = query.ToArray();

			Assert.That(result, Has.Length.EqualTo(1));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(result[0].ID, Is.EqualTo(11));
				Assert.That(result[0].FirstName, Is.EqualTo("FN2"));
				Assert.That(result[0].MiddleName, Is.Null);
				Assert.That(result[0].Gender, Is.EqualTo(Gender.Female));
			}

			if (db is DataConnection dc)
			{
				Assert.That(dc.LastQuery, Contains.Substring("WITH"));
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3407")]
		public void Issue3407Test([CteContextSource] string context)
		{
			using var db = GetDataContext(context);

			var cte = db.Person.LoadWith(p => p.Patient).AsCte();

			var data = cte.Where(r => r.ID == 2).ToList();

			var count = cte.Count();

			Assert.That(data, Has.Count.EqualTo(1));
			Assert.That(data[0].Patient, Is.Not.Null);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(data[0].Patient!.Diagnosis, Is.Not.Null);
				Assert.That(count, Is.EqualTo(4));
			}
		}

		[Test]
		public void IssueCteDuplicateColumn([CteContextSource] string context)
		{
			using var db = GetDataContext(context);

			var cte1 = db.Parent
				.Where(r => r.Value1 != null)
				.Select(c => new DuplicateColumnRecord(c.ParentID, c.Value1!.Value))
				.AsCte();

			var cte2 = db.Parent
				.Where(r => r.Value1 != null)
				.Select(p => new DuplicateColumnRecord(p.ParentID, p.Value1!.Value))
				.AsCte();

			var cte3 = cte2
				.Concat(
				from record1 in cte2
				join record2 in cte1 on record1.Id2 equals record2.Id1
				select new DuplicateColumnRecord(record1.Id1, record2.Id2))
				.AsCte();

			cte3.ToArray();
		}

		private sealed record DuplicateColumnRecord(int Id1, int Id2);

		#region Issue 4366
		sealed class Dto
		{
			[PrimaryKey] public int id { get; set; }
			public string name { get; set; } = null!;
			public int? parent_id { get; set; }
			public string? FullName;
		}

		class DtoMapped
		{
			public Dto Dto { get; set; } = null!;
			public string? FullName;
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4366")]
		public void Issue4366Test1([RecursiveCteContextSource] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<Dto>();

			db.GetCte<Dto>(d =>
			(from a in tb
			 where a.parent_id == null
			 select new Dto
			 {
				 id = a.id,
				 parent_id = a.parent_id,
				 name = a.name,
				 FullName = a.name
			 })
			 .Concat(
				from b in tb
				from recur in d.InnerJoin(dd => dd.id == b.parent_id)
				select new Dto
				{
					id = b.id,
					parent_id = b.parent_id,
					name = b.name,
					FullName = recur.FullName + " > " + b.name
				})).ToList();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4366")]
		public void Issue4366Test2([RecursiveCteContextSource] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<Dto>();

			db.GetCte<DtoMapped>(d =>
			(from a in tb
			 where a.parent_id == null
			 select new DtoMapped
			 {
				 Dto = a,
				 FullName = a.name
			 })
			 .Concat(
				from b in tb
				from recur in d.InnerJoin(dd => dd.Dto.id == b.parent_id)
				select new DtoMapped
				{
					Dto = b,
					FullName = recur.FullName + " > " + b.name
				})).ToList();
		}
		#endregion

		#region Issue 1845
		[Test(Description = "https://github.com/linq2db/linq2db/issues/1845")]
		public void Issue1845Test([CteContextSource] string context)
		{
			using var db = GetDataContext(context);

			var someCte = db.Person.Select(o => new CustomObject()
			{
				Value1 = o.FirstName,
				Value2 = o.LastName,
			}).AsCte();

			var defaultValue1 = "Somebody";
			var defaultValue2 = "Unimportant";
			var defaultValue = new List<CustomObject>()
			{
				new CustomObject()
				{
					Value1 = defaultValue1,
					Value2 = defaultValue2,
				}
			};

			var query = someCte.Union(defaultValue).AsCte();

			query.ToList();
		}

		sealed class CustomObject
		{
			public string? Value1 { get; set; }
			public string? Value2 { get; set; }
		}
		#endregion

		// CH: probably this https://github.com/ClickHouse/ClickHouse/issues/64794
		[ActiveIssue(Details = "Investigate expected SQL", Configuration = TestProvName.AllClickHouse)]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4012")]
		public void Issue4012Test([RecursiveCteContextSource] string context)
		{
			using var db = GetDataContext(context);

			var cte1 = db.GetCte<Child>(cte =>
			{
				return db.Child
				.Concat
				(
					from c in db.Child
					from c2 in cte.InnerJoin(eh => c.ChildID == eh.ParentID)
					select c
				);
			});
			var cte2 = db.GetCte<Child>(cte =>
			{
				return db.Child
				.Concat
				(
					from c in db.Child
					from c2 in cte.InnerJoin(eh => c.ParentID == eh.ChildID)
					select c
				);
			});

			var result = cte1.Union(cte2);

			var resultList = result.LoadWith(c => c.GrandChildren).ToList();
		}

		#region Issue 4717
		[Table]
		public record Issue4717Address
		{
			[PrimaryKey] public int Id { get; set; }
			[Column] public string? Address1 { get; set; }
			[Column] public string? City { get; set; }
			[Column] public string? State { get; set; }
			[Column] public string? Zip { get; set; }
		}

		[Table]
		public class Issue4717Warehouse
		{
			[PrimaryKey] public int Id { get; set; }
			[Column] public string? Name { get; set; }
			[Column] public int AddressId { get; set; }
		}

		[Table]
		public record Issue4717UnitOfMeasure
		{
			[PrimaryKey] public int Id { get; set; }
			[Column] public string? Name { get; set; }
			[Column] public string? Abbreviation { get; set; }
		}

		[Table]
		public record Issue4717Product
		{
			[PrimaryKey] public int Id { get; set; }
			[Column] public string? Description { get; set; }
			[Column] public string? Sku { get; set; }
			[Column] public int UnitOfMeasureId { get; set; }
		}

		[Table]
		[Table("Issue4717ProductIncludedProduc", Configuration = ProviderName.Oracle11Native)]
		[Table("Issue4717ProductIncludedProduc", Configuration = ProviderName.Oracle11Managed)]
		[Table("Issue4717ProductIncludedProduc", Configuration = TestProvName.Oracle11DevartOCI)]
		[Table("Issue4717ProductIncludedProduc", Configuration = TestProvName.Oracle11DevartDirect)]
		[Table("Issue4717ProductIncludedProduct", Configuration = ProviderName.Firebird25)]
		[Table("Issue4717ProductIncludedProduct", Configuration = ProviderName.Firebird3)]
		public record Issue4717ProductIncludedProductMapping
		{
			[PrimaryKey] public int ProductId { get; set; }
			[PrimaryKey] public int IncludedProductId { get; set; }
			[Column] public decimal Quantity { get; set; }
		}

		[Table]
		[Table("Issue4717WarehouseProductMappi", Configuration = ProviderName.Oracle11Native)]
		[Table("Issue4717WarehouseProductMappi", Configuration = ProviderName.Oracle11Managed)]
		[Table("Issue4717WarehouseProductMappi", Configuration = TestProvName.Oracle11DevartOCI)]
		[Table("Issue4717WarehouseProductMappi", Configuration = TestProvName.Oracle11DevartDirect)]
		[Table("Issue4717WarehouseProductMappin", Configuration = ProviderName.Firebird25)]
		[Table("Issue4717WarehouseProductMappin", Configuration = ProviderName.Firebird3)]
		public record Issue4717WarehouseProductMapping
		{
			[PrimaryKey] public int WarehouseId { get; set; }
			[PrimaryKey] public int ProductId { get; set; }
			[Column(Precision = 10, Scale = 0)] public decimal StockOnHand { get; set; }
		}

		[RequiresCorrelatedSubquery]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4717")]
		public void Issue4717Test([CteContextSource] string context)
		{
			using var db = GetDataContext(context);

			using var addressTable = db.CreateLocalTable<Issue4717Address>();
			using var warehouseTable = db.CreateLocalTable<Issue4717Warehouse>();
			using var unitOfMeasureTable = db.CreateLocalTable<Issue4717UnitOfMeasure>();
			using var productTable = db.CreateLocalTable<Issue4717Product>();
			using var productIncludedProductMappingTable = db.CreateLocalTable<Issue4717ProductIncludedProductMapping>();
			using var warehouseProductMappingTable = db.CreateLocalTable<Issue4717WarehouseProductMapping>();

			addressTable.Insert(() => new Issue4717Address()
			{
				Id = 1,
				Address1 = "123 Test St",
				City = "Test City",
				State = "TS",
				Zip = "12345"
			});

			warehouseTable.Insert(() => new Issue4717Warehouse()
			{
				Id = 1,
				Name = "Test Warehouse",
				AddressId = 1
			});

			unitOfMeasureTable.Insert(() => new Issue4717UnitOfMeasure()
			{
				Id = 1,
				Name = "Test Warehouse",
				Abbreviation = "ea"
			});

			var productId = 1;
			productTable.Insert(() => new Issue4717Product()
			{
				Id = productId,
				Sku = "123-SKU",
				Description = "Test 123 Sku",
				UnitOfMeasureId = 1
			});

			var includedProductId = 2;
			productTable.Insert(() => new Issue4717Product()
			{
				Id = includedProductId,
				Sku = "ABC-SKU",
				Description = "Test ABC Sku",
				UnitOfMeasureId = 1
			});

			productIncludedProductMappingTable.Insert(() => new Issue4717ProductIncludedProductMapping()
			{
				ProductId = productId,
				IncludedProductId = includedProductId,
				Quantity = 10
			});

			warehouseProductMappingTable.Insert(() => new Issue4717WarehouseProductMapping
			{
				WarehouseId = 1,
				ProductId = productId,
				StockOnHand = 10
			});

			var sourceQuery = from w in warehouseTable
							  select new
							  {
								  ProductId = productId,
								  WarehouseId = w.Id,
							  };

			sourceQuery = sourceQuery.AsCte();
			var query = from source in sourceQuery
						join includedProductMapping in productIncludedProductMappingTable on source.ProductId equals includedProductMapping.ProductId
						select new
						{
							source.ProductId,
							first = (from wp in warehouseProductMappingTable
									 where wp.WarehouseId == source.WarehouseId
									 select (decimal?)wp.StockOnHand
									 ).FirstOrDefault() ?? 0,
							sum = (from wp in warehouseProductMappingTable
								   where wp.WarehouseId == source.WarehouseId
								   select (decimal?)wp.StockOnHand
								   ).Sum() ?? 0,
						};

			var result = query.ToList();

			Assert.That(result, Has.Count.EqualTo(1));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(result[0].ProductId, Is.EqualTo(1));
				Assert.That(result[0].first, Is.EqualTo(10));
				Assert.That(result[0].sum, Is.EqualTo(10));
			}
		}
		#endregion

		[Table(Name = "Authors")]
		public class Author
		{
			[PrimaryKey]
			public int Id { get; set; }

			[Column(Name = "Name"), NotNull]
			public string Name { get; set; } = null!;

			// 1:1 relationship to Book
			[Association(ThisKey = "Id", OtherKey = "AuthorId", CanBeNull = true)]
			public Book? Book { get; set; }

			public static Author[] Data =
			[
				new Author() { Id = 1, Name = "John" },
				new Author() { Id = 2, Name = "Steven" },
				new Author() { Id = 3, Name = "Smith" },
			];
		}

		[Table(Name = "Books")]
		public class Book
		{
			[PrimaryKey]
			public int Id { get; set; }

			[Column(Name = "Title"), NotNull]
			public string Title { get; set; } = null!;

			[Column(Name = "AuthorId"), NotNull]
			public int AuthorId { get; set; }

			// 1:1 relationship to Author
			[Association(ThisKey = "AuthorId", OtherKey = "Id", CanBeNull = false)]
			public Author Author { get; set; } = null!;

			public static Book[] Data =
			[
				new Book() { Id = 1, AuthorId = 1, Title = "Something" },
				new Book() { Id = 2, AuthorId = 2, Title = "Book" },
				new Book() { Id = 3, AuthorId = 3, Title = "Boring" },
			];
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4012")]
		public void TestAssociations([CteContextSource(TestProvName.AllSapHana)] string context)
		{
			using var db      = GetDataContext(context);
			using var books   = db.CreateLocalTable<Book>();
			using var authors = db.CreateLocalTable<Author>();

			var booksQuery = db.GetTable<Book>().AsCte("BooksCte");

			booksQuery = booksQuery.Where(b => b.Author.Name == "Steven");
			var result = booksQuery.Select(b => b.Title).ToArray();
		}

		[Test]
		public void UnionCteWithFilter([CteContextSource] string context)
		{
			using var db      = GetDataContext(context);
			using var books   = db.CreateLocalTable(Book.Data);
			using var authors = db.CreateLocalTable(Author.Data);

			var booksQuery = db.GetTable<Book>()
				.Select(b => new
				{
					Book = b,
					b.Author
				})
				.AsCte("BooksCte");

			var query1 = booksQuery.Select(r => new
			{
				Book = (Book?)r.Book,
				Author = (Author?)null
			});

			var query2 = booksQuery.Select(r => new
			{
				Book = (Book?)null,
				Author = (Author?)r.Author
			});

			var query = query1
				.Concat(query2)
				.Where(r => r.Author!.Name == "Steven" || r.Book!.Title == "Something");

			var result = query.Select(b => Sql.ToNullable(b.Book!.Id)).ToArray();

			Assert.That(result, Has.Length.EqualTo(2));
			Assert.That(result, Contains.Item(1));
			Assert.That(result, Contains.Item(null));
		}

		sealed record SequenceBuildFailedRecord(int Id);

		[Test]
		public void Issue_SequenceBuildFailed_1([RecursiveCteContextSource(TestProvName.AllInformix)] string context)
		{
			using var db = GetDataContext(context);

			var cte = db.GetCte<SequenceBuildFailedRecord>(cte =>
			{
				return db.Person.Select(s => new SequenceBuildFailedRecord(s.Patient!.PersonID))
					.Concat(
						from r in cte
						join p in db.Patient on r.Id equals p.PersonID + 1
						select new SequenceBuildFailedRecord(p.PersonID));
			});

			var query =
				from r in cte
				join p in db.Patient on r.Id equals p.PersonID
				select new
				{
					Values = db.Person.Where(a => a.ID == r.Id).ToArray()
				};

			var result = query.ToArray();
		}

		[Test]
		public void Issue_SequenceBuildFailed_2([CteContextSource(TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);

			var cte = db.Person.Select(s => new { s.Patient!.PersonID }).AsCte();

			var query =
				from r in cte
				join p in db.Patient on r.PersonID equals p.PersonID
				select new
				{
					Values = db.Person.Where(a => a.ID == r.PersonID).ToArray()
				};

			var result = query.ToArray();
		}

		sealed class Issue4968Menu
		{
			[PrimaryKey] public int Id { get; set; }
		}

		sealed class Issue4968Item
		{
			[PrimaryKey] public int Id { get; set; }
			public int MenuId { get; set; }
			public int? ParentItemId { get; set; }
		}

		sealed class Issue4968Projection
		{ 
			public int Id1 { get; set; }
			public int? Id2 { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4968")]
		public void Issue4968Test_Tuple_Select([RecursiveCteContextSource(TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			using var tm = db.CreateLocalTable<Issue4968Menu>();
			using var ti = db.CreateLocalTable<Issue4968Item>();

			var menuId = 1;

			var cte = db.GetCte<Tuple<int, int?>>(
				cteQueryable => ti
					.Where(item => item.MenuId == menuId)
					.Select(item => Tuple.Create(item.Id, item.ParentItemId))
					.UnionAll(ti.InnerJoin(
						cteQueryable,
						(item, cte) => item.ParentItemId == cte.Item1,
						(item, cte) => Tuple.Create(item.Id, item.ParentItemId))));

			ti.Where(i => cte.Any(t => t.Item1 == i.Id)).ToList();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4968")]
		public void Issue4968Test_Tuple_Delete([RecursiveCteContextSource(TestProvName.AllDB2, TestProvName.AllFirebird, TestProvName.AllInformix, TestProvName.AllMySqlWithCTE, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			using var tm = db.CreateLocalTable<Issue4968Menu>();
			using var ti = db.CreateLocalTable<Issue4968Item>();

			var menuId = 1;

			var cte = db.GetCte<Tuple<int, int?>>(
				cteQueryable => ti
					.Where(item => item.MenuId == menuId)
					.Select(item => Tuple.Create(item.Id, item.ParentItemId))
					.UnionAll(ti.InnerJoin(
						cteQueryable,
						(item, cte) => item.ParentItemId == cte.Item1,
						(item, cte) => Tuple.Create(item.Id, item.ParentItemId))));

			ti.Where(i => cte.Any(tuple => tuple.Item1 == i.Id)).Delete();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4968")]
		public void Issue4968Test_Class_Select([RecursiveCteContextSource(TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			using var tm = db.CreateLocalTable<Issue4968Menu>();
			using var ti = db.CreateLocalTable<Issue4968Item>();

			var menuId = 1;

			var cte = db.GetCte<Issue4968Projection>(
				cteQueryable => ti
					.Where(item => item.MenuId == menuId)
					.Select(item => new Issue4968Projection() {Id1 = item.Id, Id2 = item.ParentItemId })
					.UnionAll(ti.InnerJoin(
						cteQueryable,
						(item, cte) => item.ParentItemId == cte.Id1,
						(item, cte) => new Issue4968Projection() {Id1 = item.Id, Id2 = item.ParentItemId })));

			ti.Where(i => cte.Any(t => t.Id1 == i.Id)).ToList();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4968")]
		public void Issue4968Test_Class_Delete([RecursiveCteContextSource(TestProvName.AllDB2, TestProvName.AllFirebird, TestProvName.AllInformix, TestProvName.AllMySqlWithCTE, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			using var tm = db.CreateLocalTable<Issue4968Menu>();
			using var ti = db.CreateLocalTable<Issue4968Item>();

			var menuId = 1;

			var cte = db.GetCte<Issue4968Projection>(
				cteQueryable => ti
					.Where(item => item.MenuId == menuId)
					.Select(item => new Issue4968Projection() {Id1 = item.Id, Id2 = item.ParentItemId })
					.UnionAll(ti.InnerJoin(
						cteQueryable,
						(item, cte) => item.ParentItemId == cte.Id1,
						(item, cte) => new Issue4968Projection() {Id1 = item.Id, Id2 = item.ParentItemId })));

			ti.Where(i => cte.Any(tuple => tuple.Id1 == i.Id)).Delete();
		}

		public class DateRangeHelper
		{
			public int      Counter { get; set; }
			public DateTime Date    { get; set; }
		}

		[ActiveIssue("Wrong Date manipulations", Configurations = [TestProvName.AllOracle11])]
		[Test]
		public void SelectQueryTest([RecursiveCteContextSource] string context, bool inlineParams)
		{
			using var db = GetDataContext(context);

			var dateFrom = TestData.DateTime.Date;
			var dateTo   = dateFrom.AddDays(10);

			var cte = db.GetCte<DateRangeHelper>(
				x =>
				   db
					   .SelectQuery(() => new DateRangeHelper { Counter = 1, Date = dateFrom.Date })
					   .Concat
					   (
						   x
							   .Select(_ => new DateRangeHelper { Counter = _.Counter + 1, Date = _.Date.AddDays(1) })
							   .Where(_ => _.Date < dateTo)
					   ));

			if (inlineParams)
				cte = cte.InlineParameters();

			var result = cte.ToList();

			AreEqual(
				Enumerable.Range(0, 10).Select(i => dateFrom.AddDays(i)),
				result.Select(_ => _.Date)
				);
			AreEqual(
				Enumerable.Range(1, 10),
				result.Select(_ => _.Counter)
				);

		}
	}
}
