using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Data;
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
			using var db = GetDataContext(context);

			var cte1 = db.GetTable<Child>().Where(c => c.ParentID > 1).AsCte();
			var query =
				from p in db.Parent
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

		[Test]
		[ThrowsRequiresCorrelatedSubquery(simple: true)]
		public void Test2([CteContextSource(TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);

			var cte1 = db.GetTable<Child>().Where(c => c.ParentID > 1).AsCte("CTE1_");
			var cte2 = db.Parent.Where(p => cte1.Any(c => c.ParentID == p.ParentID)).AsCte("CTE2_");
			var cte3 = db.Parent.Where(p => cte2.Any(c => c.ParentID == p.ParentID)).AsCte("CTE3_");
			var result =
				from p in cte2
				join c in cte1 on p.ParentID equals c.ParentID
				join c2 in cte2 on p.ParentID equals c2.ParentID
				join c3 in cte3 on p.ParentID equals c3.ParentID
				from c4 in db.Child.Where(c4 => c4.ParentID % 2 == 0).AsCte("LATEST").InnerJoin(c4 => c4.ParentID == c3.ParentID)
				select c3;

			var ncte1 = db.GetTable<Child>().Where(c => c.ParentID > 1);
			var ncte2 = db.Parent.Where(p => ncte1.Any(c => c.ParentID == p.ParentID));
			var ncte3 = db.Parent.Where(p => ncte2.Any(c => c.ParentID == p.ParentID));
			var expected =
				from p in ncte2
				join c in ncte1 on p.ParentID equals c.ParentID
				join c2 in ncte2 on p.ParentID equals c2.ParentID
				join c3 in ncte3 on p.ParentID equals c3.ParentID
				from c4 in db.Child.Where(c4 => c4.ParentID % 2 == 0).InnerJoin(c4 => c4.ParentID == c3.ParentID)
				select c3;

			// Looks like we do not populate needed field for CTE. It is aproblem that needs to be solved
			AreEqual(expected, result);
		}

		[Test]
		public void TestAsTable([CteContextSource] string context)
		{
			using var db = GetDataContext(context);

			var cte1 = db.GetTable<Child>().AsCte("CTE1_");
			var expected = db.GetTable<Child>();

			AreEqual(expected, cte1);
		}

		// MariaDB allows CTE ordering but do not respect it
		[Test]
		public void WithOrderBy([CteContextSource(TestProvName.AllMariaDB, TestProvName.AllYdb)] string context)
		{
			using var db = GetDataContext(context);

			var query = db.Parent
				.OrderByDescending(p => p.ParentID)
				.AsCte();

			if (!db.SqlProviderFlags.IsCTESupportsOrdering)
			{
				query.ToArray();
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

		[ActiveIssue(5596, Configuration = TestProvName.AllYdb, ErrorMessage = "Assert.That(b, Is.True)",
			Details = "YDB does not preserve a CTE's inner ORDER BY in the outer SELECT (the ORDER BY there only bounds LIMIT). Proper fix is to propagate the CTE's ORDER BY into the referencing query.")]
		[Test]
		public void WithLimitedOrderBy([CteContextSource] string context)
		{
			using var db = GetDataContext(context);

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

		[Test]
		public void ProductAndCategoryNamesOverTenDollars([NorthwindDataContext] string context)
		{
			using var db = new NorthwindDB(context);

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

		[Test]
		public void ProductAndCategoryNamesOverTenDollars2([NorthwindDataContext] string context)
		{
			using var db = new NorthwindDB(context);

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

		[Test]
		public void ProductsOverTenDollars([NorthwindDataContext] string context)
		{
			using var db = new NorthwindDB(context);

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

		[Test]
		public void EmployeeSubordinatesReport([NorthwindDataContext] string context)
		{
			using var db = new NorthwindDB(context);

			var employeeSubordinatesReport =
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
			using var db = new NorthwindDB(context);

			// just create another CTE
			var employeeCte = db.Employee.Where(e => e.EmployeeID > 0).AsCte();

			var employeeHierarchyCte = db.GetCte<EmployeeHierarchyCTE>(
				employeeHierarchy =>
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
				}
			);

			var result =
					from eh in employeeHierarchyCte
					orderby eh.HierarchyLevel, eh.LastName, eh.FirstName
					select eh;

			var data = result.ToArray();
		}

		[Test]
		public void Test4([CteContextSource] string context)
		{
			using var db = GetDataContext(context);

			var cte1 = db.GetTable<Child>().Where(c => c.ParentID > 1).AsCte("CTE1_");
			var result =
				from p in cte1
				from c4 in db.Child.Where(c4 => c4.ParentID % 2 == 0).AsCte("LAST").InnerJoin(c4 => c4.ParentID == p.ParentID)
				select c4;

			var _cte1 = db.GetTable<Child>().Where(c => c.ParentID > 1);
			var expected =
				from p in _cte1
				from c4 in db.Child.Where(c4 => c4.ParentID % 2 == 0).InnerJoin(c4 => c4.ParentID == p.ParentID)
				select c4;

			AreEqual(expected, result);
		}

		[Test]
		public void Test5([CteContextSource] string context)
		{
			using var db = GetDataContext(context);

			var cte1 = db.GetTable<Child>()
					.Where(c => c.ParentID > 1)
					.Select(child => new
					{
						child.ParentID,
						child.ChildID
					}).Distinct()
					.AsCte();

			var query =
				from p in db.Parent
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

		[Test]
		public void TestCustomCount([CteContextSource] string context)
		{
			using var db = GetDataContext(context);

			var cte1 = db.GetTable<Child>()
					.Where(c => c.ParentID > 1)
					.Select(child => new
					{
						child.ParentID,
						child.ChildID
					}).Distinct()
					.AsCte();

			var query =
				from c in cte1
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

		private sealed class CteDmlTests
		{
			private bool Equals(CteDmlTests other)
			{
				return ChildID == other.ChildID && ParentID == other.ParentID;
			}

			public override bool Equals(object? obj)
			{
				if (ReferenceEquals(null, obj)) return false;
				if (ReferenceEquals(this, obj)) return true;
				if (obj.GetType() != typeof(CteDmlTests)) return false;
				return Equals((CteDmlTests)obj);
			}

			public override int GetHashCode()
			{
				return HashCode.Combine(ChildID, ParentID);
			}

			[PrimaryKey] public int ChildID  { get; set; }
			[PrimaryKey] public int ParentID { get; set; }
		}

		[Test]
		public void TestNoColumns([CteContextSource(true, ProviderName.DB2)] string context)
		{
			using var db = GetDataContext(context);

			//using (var testTable = db.CreateLocalTable<CteDmlTests>("CteChild"))
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

		[Test]
		public void TestCondition([CteContextSource(true)] string context)
		{
			using var db = GetDataContext(context);

			int? var3 = 1;
			var cte = db.GetTable<Child>().AsCte();

			var query = cte.Where(t => t.ChildID == var3 || var3 == null);
			var str = query.ToSqlQuery().Sql;

			query.ToArray();

			if (context.IsAnyOf(TestProvName.AllYdb))
				Assert.That(str, Does.Contain("$CTE"));
			else
				Assert.That(str, Does.Contain("WITH"));
		}

		// The HANA half declares no type: the ODBC and native drivers raise their own, and only the server's own
		// text is shared. Truncated before the line/col/pos, which differ per driver.
		[ActiveIssue(3015, Configuration = TestProvName.AllSapHana, ErrorMessage = "sql syntax error: incorrect syntax near \"INSERT\"",
			Details = "HANA will not take an INSERT whose source is a CTE.")]
		[ActiveIssue(3015, Configuration = ProviderName.InformixDB2, ErrorTypeName = "IBM.Data.Db2.DB2Exception",
			ErrorMessage = "Cursor must be declared on an INSERT statement with a VALUES clause.",
			Details = "Informix will not take an INSERT ... SELECT whose source is a CTE.")]
		[Test]
		public void TestInsert([CteContextSource(true, ProviderName.DB2)] string context)
		{
			using var db = GetDataContext(context);

			using var testTable = db.CreateLocalTable<CteDmlTests>("CteChild");
			var cte1 = db.GetTable<Child>().Where(c => c.ParentID > 1).AsCte("CTE1_");
			var toInsert =
				from p in cte1
				from c4 in db.Child.Where(c4 => c4.ParentID % 2 == 0).AsCte("LAST").InnerJoin(c4 => c4.ParentID == p.ParentID)
				select new CteDmlTests
				{
					ChildID  = c4.ChildID,
					ParentID = c4.ParentID
				};

			var affected = toInsert.Distinct().Insert(testTable, c => c);

			var _cte1 = db.GetTable<Child>().Where(c => c.ParentID > 1);
			var expected = (
				from p in _cte1
				from c4 in db.Child.Where(c4 => c4.ParentID % 2 == 0).InnerJoin(c4 => c4.ParentID == p.ParentID)
				select new CteDmlTests
				{
					ChildID  = c4.ChildID,
					ParentID = c4.ParentID
				}).Distinct();

			var result = testTable.OrderBy(c => c.ChildID).ThenBy(c => c.ParentID);
			expected = expected.OrderBy(c => c.ChildID).ThenBy(c => c.ParentID);

			AreEqual(expected, result);
		}

		// MariaDB support expected in v10.6 : https://jira.mariadb.org/browse/MDEV-18511
		[ActiveIssue(3015, Configuration = TestProvName.AllSapHana, ErrorMessage = "sql syntax error: incorrect syntax near \"DELETE\"",
			Details = "HANA will not take a DELETE driven by a CTE join.")]
		[ActiveIssue(3015, Configuration = ProviderName.InformixDB2, ErrorTypeName = "IBM.Data.Db2.DB2Exception",
			ErrorMessage = "CURSOR not on SELECT statement.",
			Details = "Informix will not take a DELETE driven by a CTE join.")]
		[Test]
		[ThrowsRequiresCorrelatedSubquery(simple: true)]
		public void TestDelete([CteContextSource(TestProvName.AllFirebird, ProviderName.DB2, TestProvName.AllMariaDB, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			using var tmp = db.CreateLocalTable(
				"CteChild",
				Enumerable.Range(0, 10).Select(i => new CteDmlTests { ParentID = i, ChildID = 1000 + i })
			);

			var cte = tmp.Where(c => c.ParentID % 2 == 0).AsCte();

			var toDelete =
					from c in tmp
					from ct in cte.InnerJoin(ct => ct.ParentID == c.ParentID)
					select c;

			var recordsAffected = toDelete.Delete();
			Assert.That(recordsAffected, Is.EqualTo(5));
		}

		// MariaDB support expected in v10.6 : https://jira.mariadb.org/browse/MDEV-18511
		// AllOracle dropped from the gate: the data source below already excludes Oracle, so no Oracle case was ever
		// built and the "needs special syntax for CTE + UPDATE" note described a provider this test never reaches.
		[ActiveIssue(3015, Configuration = TestProvName.AllSapHana, ErrorMessage = "sql syntax error: incorrect syntax near \"UPDATE\"",
			Details = "HANA will not take an UPDATE driven by a CTE join.")]
		[ActiveIssue(3015, Configuration = ProviderName.InformixDB2, ErrorTypeName = "IBM.Data.Db2.DB2Exception",
			ErrorMessage = "CURSOR not on SELECT statement.",
			Details = "Informix will not take an UPDATE driven by a CTE join.")]
		[Test]
		[ThrowsRequiresCorrelatedSubquery(simple: true)]
		public void TestUpdate(
			[CteContextSource(TestProvName.AllFirebird, ProviderName.DB2, TestProvName.AllClickHouse, TestProvName.AllOracle, TestProvName.AllMariaDB)]
			string context)
		{
			using var db = GetDataContext(context);
			using var testTable = db.CreateLocalTable(
				"CteChild",
				Enumerable.Range(0, 10).Select(i => new CteDmlTests { ParentID = i, ChildID = 1000 + i })
			);

			var cte = testTable.Where(c => c.ParentID % 2 == 0).AsCte();
			var toUpdate =
					from c in testTable
					from ct in cte.InnerJoin(ct => ct.ParentID == c.ParentID)
					select c;

			toUpdate.Update(prev => new CteDmlTests { ParentID = prev.ChildID });

			var expected = testTable.Where(c => c.ParentID % 2 == 0)
					.Select(c => new CteDmlTests { ParentID = c.ChildID, ChildID = c.ChildID });

			var result = testTable.Where(c => c.ParentID % 2 == 0);

			AreEqual(expected, result);
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
			using var db = GetDataContext(context);

			var cteRecursive = db.GetCte<RecursiveCTE>(
				cte =>
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
					),
				"MY_CTE"
			);

			var result = cteRecursive.ToArray();
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

			using var db = GetDataContext(context);
			using var tree = db.CreateLocalTable(hierarchyData);
			var hierarchy = GetHierarchyDown(tree, db);

			var result = hierarchy.OrderBy(h => h.Id);
			var expected = EnumerateDown(hierarchyData, 0, null).OrderBy(h => h.Id);

			AreEqualWithComparer(expected, result);
		}

		[Test]
		public void TestDoubleRecursion([RecursiveCteContextSource(true, ProviderName.DB2)] string context)
		{
			var hierarchyData = GeHirarchyData();

			using var db = GetDataContext(context);
			using var tree = db.CreateLocalTable(hierarchyData);
			var hierarchy1 = GetHierarchyDown(tree, db);
			var hierarchy2 = GetHierarchyDown(tree, db);

			var query =
				from h1 in hierarchy1
				from h2 in hierarchy2.InnerJoin(h2 => h2.Id == h1.Id)
				select new
				{
					h1.Id,
					LevelSum = h2.Level + h1.Level
				};

			var count = query.Count();

			Assert.That(count, Is.GreaterThan(0));
		}

		[Test]
		public void RecursiveCount([RecursiveCteContextSource(true, ProviderName.DB2)] string context)
		{
			var hierarchyData = GeHirarchyData();

			using var db = GetDataContext(context);
			using var tree = db.CreateLocalTable(hierarchyData);
			var hierarchy = GetHierarchyDown(tree, db);
			var expected = EnumerateDown(hierarchyData, 0, null);

			Assert.That(hierarchy.Count(), Is.EqualTo(expected.Count()));
		}

		[ActiveIssue(3015, Configurations = [ProviderName.InformixDB2], ErrorTypeName = "IBM.Data.Db2.DB2Exception",
			ErrorMessage = "Cursor must be declared on an INSERT statement with a VALUES clause.",
			Details = "Informix will not take an INSERT ... SELECT whose source is a recursive CTE.")]
		[Test]
		public void RecursiveInsertInto([RecursiveCteContextSource(true, ProviderName.DB2)] string context)
		{
			var hierarchyData = GeHirarchyData();

			using var db = GetDataContext(context);
			using var tree = db.CreateLocalTable(hierarchyData);
			using var resultTable = db.CreateLocalTable<HierarchyData>();
			var hierarchy = GetHierarchyDown(tree, db);
			hierarchy.Insert(resultTable, r => r);

			var result = resultTable.OrderBy(h => h.Id);
			var expected = EnumerateDown(hierarchyData, 0, null).OrderBy(h => h.Id);

			AreEqualWithComparer(expected, result);
		}

		[Test]
		public void RecursiveDeepNesting([RecursiveCteContextSource(true, TestProvName.AllDB2)] string context)
		{
			using var db = GetDataContext(context);
			using var tree = db.CreateLocalTable<HierarchyTree>();
			var hierarchy = GetHierarchyDown(tree, db);

			var query =
				from q in hierarchy
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
				if (obj.GetType() != typeof(TestWrapper)) return false;
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
			public Child?  Child  { get; set; }
			public Parent? Parent { get; set; }

			private bool Equals(TestWrapper2 other)
			{
				return Equals(Child, other.Child) && Equals(Parent, other.Parent);
			}

			public override bool Equals(object? obj)
			{
				if (ReferenceEquals(null, obj)) return false;
				if (ReferenceEquals(this, obj)) return true;
				if (obj.GetType() != typeof(TestWrapper2)) return false;
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
			using var db = GetDataContext(context);
			var cteQuery = db.GetTable<Child>()
				.Select(child => new TestWrapper()
				{
					Child = child
				});

			var cte1 = cteQuery.AsCte();

			var query =
				from p in db.Parent
				join c in cte1 on p.ParentID equals c.Child!.ParentID
				select new { p, c };

			var result = query.ToArray();

			var expected =
				from p in db.Parent
				join c in cteQuery on p.ParentID equals c.Child!.ParentID
				select new { p, c };

			Assert.That(result.OrderBy(_ => _.p.ParentID).ThenBy(_ => _.c.Child?.ChildID), Is.EqualTo(expected.ToList().OrderBy(_ => _.p.ParentID).ThenBy(_ => _.c.Child?.ChildID)));
		}

		[Test]
		public void TestWithWrapperUnion([CteContextSource] string context)
		{
			using var db = GetDataContext(context);
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

		[Test]
		public void TestEmbedded([CteContextSource] string context)
		{
			using var db = GetDataContext(context);
			var cte1 = db.GetTable<Child>().Select(c => c.ChildID).AsCte("CTE_1");
			var cte2 = cte1.Distinct().AsCte("CTE_2");
			var cte3 = cte2.Distinct().AsCte("CTE_3");
			var cte4 = cte3.Distinct().AsCte("CTE_3");

			var qCte = db.Child.Where(w => w.ChildID.NotIn(cte4)).ToList();
		}

		[Test]
		public void TestCteOptimization([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			var children = db.Child.Where(c => c.ChildID > 1).AsCte().HasUniqueKey(ct => ct.ChildID);

			var query =
				from c in db.Child
				from ct in children.LeftJoin(ct => c.ChildID == ct.ChildID)
				select c;

			query.ToArray();
			var sql = query.ToSqlQuery().Sql;

			Assert.That(sql, Is.Not.Contains("WITH"));
		}

		[Test]
		public void TestRecursiveScalar([RecursiveCteContextSource] string context)
		{
			using var db = GetDataContext(context);

			var cteRecursive = db.GetCte<int>(
				cte =>
					(
						from c in db.Child.Take(1)
						select c.ChildID
					)
					.Concat
					(
						from c in db.Child
						from ct in cte.InnerJoin(ct => ct == c.ChildID + 1)
						select c.ChildID + 1
					),
				"MY_CTE"
			);

			var result = cteRecursive.ToArray();
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
				var cte = db
					.GetCte<OrgGroupDepthWrapper>(
						previous =>
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
						}
					)
					.Select(wrapper => wrapper.OrgGroup);

				var result = cte.ToList();

			}
		}

		sealed class PartNode
		{
			[PrimaryKey] public int     Id   { get; set; }
			public              string? Name { get; set; }
		}

		sealed class PartReference
		{
			[PrimaryKey] public int Id          { get; set; }
			public              int ParentId    { get; set; }
			public              int ReferenceId { get; set; }
		}

		sealed class PartHierarchyCte
		{
			public object? RootPartSortField { get; set; }
			public int     RootPartId        { get; set; }
			public int     PartId            { get; set; }
			public int     HierarchyLevel    { get; set; }
		}

		// https://github.com/linq2db/linq2db/issues/5457 - recursive CTE drops columns used only in recursive/outer joins
		[Test(Description = "https://github.com/linq2db/linq2db/issues/5457")]
		public void RecursiveCteKeepsColumnsUsedOnlyInJoins([RecursiveCteContextSource] string context)
		{
			using var db    = GetDataContext(context);
			using var parts = db.CreateLocalTable(new[]
			{
				new PartNode { Id = 1, Name = "A" },
				new PartNode { Id = 2, Name = "B" },
				new PartNode { Id = 3, Name = "C" },
			});
			using var refs = db.CreateLocalTable(new[]
			{
				new PartReference { Id = 1, ParentId = 1, ReferenceId = 2 },
				new PartReference { Id = 2, ParentId = 2, ReferenceId = 3 },
			});

			// Anchor term paginated with OrderBy/Skip/Take (wraps it in a subquery).
			var anchor = parts
				.Select(x => new PartHierarchyCte
				{
					RootPartSortField = x.Name,
					RootPartId        = x.Id,
					HierarchyLevel    = 0,
					PartId            = x.Id
				})
				.OrderBy(x => x.RootPartSortField)
				.Skip(0)
				.Take(20);

			var partCte = db.GetCte<PartHierarchyCte>(partHierarchy =>
				anchor.Concat(
					partHierarchy.InnerJoin(
						refs,
						(cte, reference) => reference.ParentId == cte.PartId,
						(cte, reference) => new PartHierarchyCte
						{
							RootPartSortField = cte.RootPartSortField,
							RootPartId        = cte.RootPartId,
							PartId            = reference.ReferenceId,
							HierarchyLevel    = cte.HierarchyLevel + 1
						})));

			// Outer query joins on cte.PartId but only projects RootPartId/RootPartSortField.
			var allRelevant = parts
				.InnerJoin(
					partCte,
					(me, cte) => me.Id == cte.PartId,
					(me, id) => new { id.RootPartId, id.RootPartSortField, me });

			var result = allRelevant.ToList();

			// CTE columns PartId/RootPartId/RootPartSortField must survive so the recursion and the outer
			// join resolve correctly. RootPartSortField is the object-typed member that triggers #5457, so
			// its round-tripped value is asserted too (each row carries its root Part's Name).
			// Expected (RootPartId, joined Part.Id, RootPartSortField) tuples for the hierarchy:
			//   roots: (1,1,"A") (2,2,"B") (3,3,"C"); 1->2: (1,2,"A"); 2->3: (2,3,"B"); 1->2->3: (1,3,"A")
			result
				.Select(x => (x.RootPartId, x.me.Id, SortField: (string)x.RootPartSortField!))
				.OrderBy(x => (x.RootPartId, x.Id))
				.ShouldBe(new[] { (1, 1, "A"), (1, 2, "A"), (1, 3, "A"), (2, 2, "B"), (2, 3, "B"), (3, 3, "C") });
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

		[Test(Description = "https://github.com/linq2db/linq2db/issues/2029")]
		public void CteColumnNamesMatchAliasesWithFinalAliases([CteContextSource(TestProvName.AllClickHouse, TestProvName.AllYdb)] string context)
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
				internal string? NcCodeBo { get; set; }
				internal string? NcCode { get; set; }
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

		private sealed class MultiPartCteProjection
		{
			public string FirstName { get; set; } = null!;
			public string LastName  { get; set; } = null!;
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3359")]
		public void RecursiveCteWithMultipleRecursivePartsIsPlainUnion([RecursiveCteContextSource(
			TestProvName.AllInformix,
			TestProvName.AllOracle, // too many unions (ORA-32041: UNION ALL operation in recursive WITH clause must have only two branches)
			TestProvName.AllPostgreSQL, // too many joins? (42P19: recursive reference to query "cte" must not appear within its non-recursive term)
			ProviderName.DB2, // joins (SQL0345N  The fullselect of the recursive common table expression "cte" must be the UNION of two or more fullselects and cannot include column functions, GROUP BY clause, HAVING clause, ORDER BY clause, or an explicit join including an ON clause.)
			TestProvName.AllDuckDB // multiple recursive references in different UNION ALL branches not supported (Binder Error: Circular reference to CTE)
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

			var query = db.GetCte<MultiPartCteProjection>(cte =>
			{
				return db.Person.Select(p => new MultiPartCteProjection() { FirstName = p.FirstName, LastName = p.LastName })
				.Concat(
					from p in cte
					join d in db.Doctor on p.FirstName equals d.Taxonomy
					select new MultiPartCteProjection() { FirstName = p.FirstName, LastName = p.LastName }
					)
				.Concat(
					from p in cte
					join pat in db.Patient on p.FirstName equals pat.Diagnosis
					select new MultiPartCteProjection() { FirstName = p.FirstName, LastName = p.LastName }
					);
			});

			query.ToArray();

			if (db is TestDataConnection cn)
				cn.LastQuery!.ShouldContain("SELECT", Exactly.Times(4));
		}

		public record class CteRecordClass(int Id, string FirstName, string LastName);
		public class CteRecordLikeClass
		{
			public CteRecordLikeClass(int Id, string FirstName, string LastName)
			{
				this.Id        = Id;
				this.FirstName = FirstName;
				this.LastName  = LastName;
			}

			public int    Id        { get; }
			public string FirstName { get; }
			public string LastName  { get; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3357")]
		public void RecursiveCteOfRecordClass([RecursiveCteContextSource] string context)
		{
			using var db = GetDataContext(context);

			var query = db.GetCte<CteRecordClass>(cte =>
			{
				return db.Person.Select(p => new CteRecordClass(p.ID, p.FirstName, p.LastName))
				.Concat(
					from p in cte
					join r in db.Person on p.FirstName equals r.LastName
					select new CteRecordClass(r.ID, r.FirstName, r.LastName)
					);
			});

			AreEqual(
				Person.Select(p => new CteRecordClass(p.ID, p.FirstName, p.LastName)),
				query.ToArray());
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3357")]
		public void RecursiveCteOfRecordLikeClass([RecursiveCteContextSource] string context)
		{
			using var db = GetDataContext(context);

			var query = db.GetCte<CteRecordLikeClass>(cte =>
			{
				return db.Person.Select(p => new CteRecordLikeClass(p.ID, p.FirstName, p.LastName))
				.Concat(
					from p in cte
					join r in db.Person on p.FirstName equals r.LastName
					select new CteRecordLikeClass(r.ID, r.FirstName, r.LastName)
					);
			});

			AreEqualWithComparer(
				Person.Select(p => new CteRecordLikeClass(p.ID, p.FirstName, p.LastName)),
				query.ToArray());
		}

		[Table]
		private class CteLiteralTypingTable
		{
			[PrimaryKey] public int Id { get; set; }
			// by default we generate N-literal, which is not compatible with (var)char
			[Column(DataType = DataType.VarChar)] public string? Str { get; set; }
		}

		private class CteLiteralTypingProjection
		{
			public int     Id   { get; set; }
			public string? Str  { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3360")]
		public void RecursiveCteLiteralTypedByAnchorColumn([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<CteLiteralTypingTable>();

			var query = db.GetCte<CteLiteralTypingProjection>(cte =>
			{
				return tb.Select(p => new CteLiteralTypingProjection() { Id = p.Id, Str = p.Str })
				.Concat(
					from p in cte
					join r in tb on p.Id equals r.Id + 1
					select new CteLiteralTypingProjection() { Id = p.Id, Str = "Str" }
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

		[ActiveIssue(3360, ErrorTypeName = "IBM.Data.Db2.DB2Exception", ErrorMessage = "SQL0604N")]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/3360")]
		public void RecursiveCteLiteralTypedByAnchorColumnDb2([IncludeDataSources(true, ProviderName.DB2)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<CteLiteralTypingTable>();

			var query = db.GetCte<CteLiteralTypingProjection>(cte =>
			{
				return tb.Select(p => new CteLiteralTypingProjection() { Id = p.Id, Str = p.Str })
				.Concat(
					from p in cte
					join r in tb on p.Id equals r.Id + 1
					select new CteLiteralTypingProjection() { Id = p.Id, Str = "Str" }
					);
			});

			query.ToArray();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3360")]
		public void RecursiveCteLiteralTypedByAnchorColumnAllProviders([RecursiveCteContextSource(ProviderName.DB2)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<CteLiteralTypingTable>();

			var query = db.GetCte<CteLiteralTypingProjection>(cte =>
			{
				return tb.Select(p => new CteLiteralTypingProjection() { Id = p.Id, Str = p.Str })
				.Concat(
					from p in cte
					from r in tb
					where p.Id == r.Id + 1
					select new CteLiteralTypingProjection() { Id = p.Id, Str = "Str" }
					);
			});

			query.ToArray();
		}

		[Table]
		private class CteStringEnumTable
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

		private class CteStringEnumProjection
		{
			public int     Id  { get; set; }
			public StrEnum Str { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3360")]
		public void RecursiveCteStringEnumLiteralTyping([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<CteStringEnumTable>();

			var query = db.GetCte<CteStringEnumProjection>(cte =>
			{
				return tb.Select(p => new CteStringEnumProjection() { Id = p.Id, Str = p.Str })
				.Concat(
					from p in cte
					join r in tb on p.Id equals r.Id + 1
					select new CteStringEnumProjection() { Id = p.Id, Str = StrEnum.Two }
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

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3360")]
		public void RecursiveCteStringEnumLiteralTypingAllProviders([RecursiveCteContextSource(ProviderName.DB2)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<CteStringEnumTable>();

			var query = db.GetCte<CteStringEnumProjection>(cte =>
			{
				return tb.Select(p => new CteStringEnumProjection() { Id = p.Id, Str = p.Str })
				.Concat(
					from p in cte
					join r in tb on p.Id equals r.Id + 1
					select new CteStringEnumProjection() { Id = p.Id, Str = StrEnum.Two }
					);
			});

			query.ToArray();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3360")]
		public void RecursiveCteStringEnumLiteralTypingDb2([IncludeDataSources(true, ProviderName.DB2)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<CteStringEnumTable>();

			var query = db.GetCte<CteStringEnumProjection>(cte =>
			{
				return tb.Select(p => new CteStringEnumProjection() { Id = p.Id, Str = p.Str })
				.Concat(
					from p in cte
					from r in tb
					where p.Id == r.Id + 1
					select new CteStringEnumProjection() { Id = p.Id, Str = StrEnum.Two }
					);
			});

			query.ToArray();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3360")]
		public void RecursiveCteLiteralTypedByProjectionProperty([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<CteLiteralTypingTable>();

			var query = db.GetCte<CteLiteralTypingTable>(cte =>
			{
				return tb.Select(p => new CteLiteralTypingTable() { Id = p.Id, Str = "Str1" })
				.Concat(
					from p in cte
					join r in tb on p.Id equals r.Id + 1
					select new CteLiteralTypingTable() { Id = p.Id, Str = "Str2" }
					);
			});

			query.ToArray();
			if (db is TestDataConnection dc)
			{
				dc.LastQuery!.ShouldNotContain("N'");
				dc.LastQuery!.ToUpperInvariant().ShouldContain("AS VARCHAR(MAX))", Exactly.Twice());
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3360")]
		public void RecursiveCteLiteralTypedByProjectionPropertyAllProviders([RecursiveCteContextSource(ProviderName.DB2)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<CteLiteralTypingTable>();

			var query = db.GetCte<CteLiteralTypingTable>(cte =>
			{
				return tb.Select(p => new CteLiteralTypingTable() { Id = p.Id, Str = "Str1" })
				.Concat(
					from p in cte
					join r in tb on p.Id equals r.Id + 1
					select new CteLiteralTypingTable() { Id = p.Id, Str = "Str2" }
					);
			});

			query.ToArray();
		}

		[ActiveIssue(3360, ErrorTypeName = "IBM.Data.Db2.DB2Exception", ErrorMessage = "SQL0604N")]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/3360")]
		public void RecursiveCteLiteralTypedByProjectionPropertyDb2([IncludeDataSources(true, ProviderName.DB2)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<CteLiteralTypingTable>();

			var query = db.GetCte<CteLiteralTypingTable>(cte =>
			{
				return tb.Select(p => new CteLiteralTypingTable() { Id = p.Id, Str = "Str1" })
				.Concat(
					from p in cte
					from r in tb
					where p.Id == r.Id + 1
					select new CteLiteralTypingTable() { Id = p.Id, Str = "Str2" }
					);
			});

			query.ToArray();
		}

		[Table]
		private class CteNullInAnchorTable
		{
			[PrimaryKey                                      ] public int       Id    { get; set; }
			[NotColumn(Configuration = ProviderName.Firebird)]
			[NotColumn(Configuration = ProviderName.DB2)     ]
			[Column                                          ] public Guid?     Guid  { get; set; }
			[Column(DataType = DataType.VarChar, Length = 50)] public StrEnum?  Enum1 { get; set; }
		}

		// No ErrorTypeName: the two SqlClient packages raise their own SqlException and share only the message.
		[ActiveIssue(3360, Configurations = [TestProvName.AllSqlServer],
			ErrorMessage = "Types don't match between the anchor and the recursive part in column \"Guid\" of recursive query \"cte\".",
			Details = "The untyped NULL in the anchor settles the column on a type the recursive part cannot match - the same defect #3360 reports for unions.")]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/3360")]
		public void RecursiveCteNullGuidInAnchor([RecursiveCteContextSource(TestProvName.AllFirebird, ProviderName.DB2)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<CteNullInAnchorTable>();

			var query = db.GetCte<CteNullInAnchorTable>(cte =>
			{
				return tb.Select(p => new CteNullInAnchorTable() { Id = p.Id, Guid = null })
				.Concat(
					from p in cte
					join r in tb on p.Id equals r.Id + 100
					select new CteNullInAnchorTable() { Id = p.Id, Guid = r.Guid }
					);
			});

			query.ToArray();
		}

		[ActiveIssue(3360, Configurations = [TestProvName.AllSqlServer],
			ErrorMessage = "Types don't match between the anchor and the recursive part in column \"Enum1\" of recursive query \"cte\".",
			Details = "as RecursiveCteNullGuidInAnchor, on the enum column.")]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/3360")]
		public void RecursiveCteNullEnumInAnchor([RecursiveCteContextSource(ProviderName.DB2)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<CteNullInAnchorTable>();

			var query = db.GetCte<CteNullInAnchorTable>(cte =>
			{
				return tb.Select(p => new CteNullInAnchorTable() { Id = p.Id, Enum1 = null })
				.Concat(
					from p in cte
					join r in tb on p.Id equals r.Id + 100
					select new CteNullInAnchorTable() { Id = p.Id, Enum1 = StrEnum.One }
					);
			});

			query.ToArray();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3360")]
		public void RecursiveCteNullEnumInAnchorDb2([IncludeDataSources(true, ProviderName.DB2)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<CteNullInAnchorTable>();

			var query = db.GetCte<CteNullInAnchorTable>(cte =>
			{
				return tb.Select(p => new CteNullInAnchorTable() { Id = p.Id, Enum1 = null })
				.Concat(
					from p in cte
					from r in tb
					where p.Id == r.Id + 100
					select new CteNullInAnchorTable() { Id = p.Id, Enum1 = StrEnum.One }
					);
			});

			query.ToArray();
		}

		[Sql.Expression("CAST({0} AS VARCHAR({1}))", ServerSideOnly = true, IsNullable = Sql.IsNullableType.Nullable, IgnoreGenericParameters = true)]
		static T VarChar<T>(T value, int size)
		{
			throw new ServerSideOnlyException(nameof(VarChar));
		}

		private sealed record TypedNullEnumProjection(int Id, StrEnum? Value);

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3360")]
		public void CteTypedNullEnumInFirstUnionBranch([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<CteNullInAnchorTable>();

			var query =
				from node in db.GetCte<TypedNullEnumProjection>(
					cte =>
					{
						return db.GetTable<CteNullInAnchorTable>().Select(p => new TypedNullEnumProjection(p.Id, VarChar<StrEnum?>(null, 10)))
						.Concat(
							from p in db.GetTable<CteNullInAnchorTable>()
							select new TypedNullEnumProjection(p.Id, VarChar(StrEnum.One, 10))
							);
					}
				)
				select new TypedNullEnumProjection(node.Id, node.Value);

			query.ToArray();
		}

		[ActiveIssue(3360, ErrorTypeName = "LinqToDB.LinqToDBException", ErrorMessage = "Database column type cannot be determined automatically and must be specified explicitly for system type",
			Details = "a typed null enum in the anchor cannot be typed from the other branch - #3360's subject. The fragment stops before the system type, which renders as an assembly-qualified name carrying the assembly version.")]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/3360")]
		public void RecursiveCteTypedNullEnumInAnchorWithMultipleBranches([IncludeDataSources(false, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataConnection(context);

			using var tb = db.CreateLocalTable<CteNullInAnchorTable>();

			var query =
				from node in db.GetCte<TypedNullEnumProjection>(
					cte => db.GetTable<CteNullInAnchorTable>()
						.Select(p => new TypedNullEnumProjection(p.Id, VarChar<StrEnum?>(null, 50)))
						.Concat(
							from p in db.GetTable<CteNullInAnchorTable>()
							join parent in cte on p.Id equals parent.Id + 1
							select new TypedNullEnumProjection(p.Id, StrEnum.One)
						)
						.Concat(
							from p in db.GetTable<CteNullInAnchorTable>()
							select new TypedNullEnumProjection(p.Id, p.Enum1)
						)
						.Concat(
							from p in db.GetTable<CteNullInAnchorTable>()
							select new TypedNullEnumProjection(p.Id, null)
						)
				)
				select new TypedNullEnumProjection(node.Id, node.Value);

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
			[PrimaryKey] public Guid Id { get; set; }
			[Column] public Guid ChildId { get; set; }
		}

		[Table]
		public class InvalidColumnIndexMappingTable2
		{
			[PrimaryKey] public Guid Id { get; set; }
			[Column(DataType = DataType.VarChar)] public InvalidColumnIndexMappingEnum2 Enum2 { get; set; }
		}

		[Table]
		public class InvalidColumnIndexMappingTable3
		{
			[PrimaryKey] public Guid Id { get; set; }
			[Column(DataType = DataType.VarChar)] public InvalidColumnIndexMappingEnum1 Enum1 { get; set; }
		}

		[Table]
		public class InvalidColumnIndexMappingTable4
		{
			[PrimaryKey] public Guid Id { get; set; }
		}

		private record InvalidColumnIndexMappingRecord(Guid Id, Guid? ChildId, InvalidColumnIndexMappingEnum1? Enum1, InvalidColumnIndexMappingEnum2? Enum2);

		// Message-only: the two SqlClient packages raise their own SqlException, and the remote wrapper carries
		// whichever of them the server leg used.
		[ActiveIssue(3360, ErrorMessage = "Types don't match between the anchor and the recursive part in column \"Enum1\" of recursive query \"cte\".",
			Details = "The older symptom, a convert exception (LinqToDBConvertException : Cannot convert value 'ENUM1_VALUE: System.String' to type 'Tests.Linq.CteTests+InvalidColumnIndexMappingEnum2'), was not reproduced by the sweep; what happens now is the server refusing the recursive query outright.")]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/3360")]
		public void RecursiveCteInvalidColumnIndexMapping([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
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
		public class CteColumnTypesTable
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

			public static CteColumnTypesTable[] Items = new[]
			{
				new CteColumnTypesTable() { Id = 1 },
				new CteColumnTypesTable() { Id = 2, Byte = 1, ByteN = 2, Guid = TestData.Guid1, GuidN = TestData.Guid2, Enum = InvalidColumnIndexMappingEnum1.Value, EnumN = InvalidColumnIndexMappingEnum2.Value, Bool = true, BoolN = false },
				new CteColumnTypesTable() { Id = 4, Byte = 3, ByteN = 4, Guid = TestData.Guid3, GuidN = TestData.Guid1, Enum = InvalidColumnIndexMappingEnum1.Value, EnumN = InvalidColumnIndexMappingEnum2.Value, Bool = false, BoolN = true },
			};
		}

		private record CteColumnTypesRecord(int Id, byte? Byte, byte? ByteN, Guid? Guid, Guid? GuidN, InvalidColumnIndexMappingEnum1? Enum, InvalidColumnIndexMappingEnum2? EnumN, bool? Bool, bool? BoolN);

		// ClickHouse and MySQL dropped: both ran in the sweep and both pass. What is left fails two ways - SQL
		// Server refuses the recursive query, Firebird overflows while widening the anchor's literal.
		[ActiveIssue(3015, Configuration = TestProvName.AllSqlServer,
			ErrorMessage = "Types don't match between the anchor and the recursive part in column \"Byte\" of recursive query \"cte\".",
			Details = "Message-only: the two SqlClient packages raise their own SqlException.")]
		[ActiveIssue(3015, Configuration = TestProvName.AllFirebird, ErrorTypeName = "FirebirdSql.Data.FirebirdClient.FbException",
			ErrorMessage = "arithmetic exception, numeric overflow, or string truncation",
			Details = "Firebird types the anchor's null literal too narrowly and overflows instead of refusing.")]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/3360")]
		public void RecursiveCteNullLiteralsInAnchor([RecursiveCteContextSource] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(CteColumnTypesTable.Items);

			var query = from record in db.GetCte<CteColumnTypesRecord>(cte =>
			{
				return table.Where(r => r.Id == 1)
							.Select(r => new CteColumnTypesRecord(r.Id, null, null, null, null, null, null, null, null))
							.Concat(
								from r in table
								join parent in cte on r.Id equals parent.Id + 1
								select new CteColumnTypesRecord(r.Id, r.Byte, r.ByteN, r.Guid, r.GuidN, r.Enum, r.EnumN, r.Bool, r.BoolN));
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

		[ActiveIssue(3015, Configuration = TestProvName.AllSqlServer,
			ErrorMessage = "Types don't match between the anchor and the recursive part in column \"Enum\" of recursive query \"cte\".",
			Details = "as RecursiveCteNullLiteralsInAnchor, on the enum column.")]
		[ActiveIssue(3015, Configuration = TestProvName.AllFirebird, ErrorTypeName = "FirebirdSql.Data.FirebirdClient.FbException",
			ErrorMessage = "arithmetic exception, numeric overflow, or string truncation",
			Details = "as RecursiveCteNullLiteralsInAnchor. ClickHouse and MySQL dropped here too - both ran and both pass.")]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/3360")]
		public void RecursiveCteDoubleColumnSelectionInAnchor([RecursiveCteContextSource] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(CteColumnTypesTable.Items);

			var query = from record in db.GetCte<CteColumnTypesRecord>(cte =>
			{
				return table.Where(r => r.Id == 2)
							.Select(r => new CteColumnTypesRecord(r.Id, r.Byte, r.Byte, r.Guid, r.Guid, null, null, r.Bool, r.Bool))
							.Concat(
								from r in table
								join parent in cte on r.Id equals parent.Id + 2
								select new CteColumnTypesRecord(r.Id, r.Byte, r.ByteN, r.Guid, r.GuidN, r.Enum, r.EnumN, r.Bool, r.BoolN));
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

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3360")]
		public void RecursiveCteLiteralsInAnchor([RecursiveCteContextSource(TestProvName.AllInformix)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(CteColumnTypesTable.Items);

			var query = from record in db.GetCte<CteColumnTypesRecord>(cte =>
			{
				return table.Where(r => r.Id == 2)
							.Select(r => new CteColumnTypesRecord(r.Id, 5, 5, new Guid("0B8AFE27-481C-442E-B8CF-729DDFEECE29"), new Guid("0B8AFE27-481C-442E-B8CF-729DDFEECE30"), InvalidColumnIndexMappingEnum1.Value, InvalidColumnIndexMappingEnum2.Value, true, false))
							.Concat(
								from r in table
								join parent in cte on r.Id equals parent.Id + 2
								select new CteColumnTypesRecord(r.Id, r.Byte, r.ByteN, r.Guid, r.GuidN, r.Enum, r.EnumN, r.Bool, r.BoolN));
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

		[ActiveIssue(2451, ErrorMessage = "Types don't match between the anchor and the recursive part in column \"FirstName\" of recursive query \"cte\".",
			Details = "Message-only, as RecursiveCteInvalidColumnIndexMapping.")]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/2451")]
		public void RecursiveCteComputedColumnTyping([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
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

		// The two providers name the same defect differently: PostgreSQL reports the width mismatch, SQL Server
		// only that the types differ. The SQL Server half is message-only because its two client packages raise
		// their own SqlException.
		[ActiveIssue(2451, Configuration = TestProvName.AllPostgreSQL, ErrorTypeName = "Npgsql.PostgresException",
			ErrorMessage = "42804: recursive query \"cte\" column 1 has type character varying(50) in non-recursive term but type character varying overall",
			Details = "The anchor's column keeps its declared width where the recursive term has none.")]
		[ActiveIssue(2451, Configuration = TestProvName.AllSqlServer,
			ErrorMessage = "Types don't match between the anchor and the recursive part in column \"FirstName\" of recursive query \"cte\".",
			Details = "as the PostgreSQL half.")]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/2451")]
		public void RecursiveCteComputedColumnTypingAllProviders([RecursiveCteContextSource(ProviderName.DB2)] string context)
		{
			using var db = GetDataContext(context);

			var query = db
				.GetCte<Person>(cte =>
					db.Person
						.Select(p => new Person() { FirstName = p.FirstName })
						.Concat(
							from p in cte
							join r in db.Person on p.FirstName equals r.LastName
							select new Person() { FirstName = r.FirstName + '/' + r.LastName }
						)
				);

			query.ToArray();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/2451")]
		public void RecursiveCteComputedColumnTypingDb2([IncludeDataSources(true, ProviderName.DB2)] string context)
		{
			using var db = GetDataContext(context);

			var query = db
				.GetCte<Person>(cte =>
					db.Person
						.Select(p => new Person() { FirstName = p.FirstName })
						.Concat(
							from p in cte
							from r in db.Person
							where p.FirstName == r.LastName
							select new Person() { FirstName = r.FirstName + '/' + r.LastName }
						)
				);

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

		[Test(Description = "https://github.com/linq2db/linq2db/issues/2264")]
		public void RecursiveCteWithEntityAsProperty([RecursiveCteContextSource] string context)
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

		[ActiveIssue(3015, Configuration = TestProvName.AllSapHana, ErrorMessage = "sql syntax error: incorrect syntax near \"INSERT\"",
			Details = "as TestInsert - HANA will not take an INSERT whose source is a CTE.")]
		[ActiveIssue(3015, Configuration = ProviderName.InformixDB2, ErrorTypeName = "IBM.Data.Db2.DB2Exception",
			ErrorMessage = "Cursor must be declared on an INSERT statement with a VALUES clause.",
			Details = "as TestInsert - Informix will not take an INSERT ... SELECT whose source is a CTE.")]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/3945")]
		public void InsertFromCteJoinedWithTable([CteContextSource] string context)
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
		private class GroupedEnumTable
		{
			[PrimaryKey] public int      ID        { get; set; }
			[Column    ] public string?  Value     { get; set; }
			[Column    ] public TaxType? EnumValue { get; set; }

			public enum TaxType
			{
				NoTax       = 0,
				NonResident = 3,
			}

			public static readonly GroupedEnumTable[] Data = new []
			{
				new GroupedEnumTable() { ID = 1, Value = "000001", EnumValue = TaxType.NoTax },
				new GroupedEnumTable() { ID = 2, Value = "000001", EnumValue = TaxType.NonResident },
				new GroupedEnumTable() { ID = 3, Value = "000001", EnumValue = null },
				new GroupedEnumTable() { ID = 4, Value = "000002", EnumValue = TaxType.NoTax },
			};
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4167")]
		public void GroupedNullableEnumKeyDefaultThroughCte([CteContextSource] string context, [Values] bool withCte)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(GroupedEnumTable.Data);

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
				Assert.That(result[0].EnumValue, Is.EqualTo(GroupedEnumTable.TaxType.NoTax));
				Assert.That(result[1].EnumValue, Is.EqualTo(GroupedEnumTable.TaxType.NoTax));
				Assert.That(result[2].EnumValue, Is.EqualTo(GroupedEnumTable.TaxType.NonResident));
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/2145")]
		public void AsCteOverLocalCollectionThrows([CteContextSource] string context)
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

		[Test(Description = "https://github.com/linq2db/linq2db/issues/2145")]
		public void AsCteOverLocalCollectionOfDataContext([CteContextSource] string context)
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

			// YQL has no WITH; CTEs render as "$name = SELECT ...", so skip the WITH-shape check for YDB.
			if (db is DataConnection dc && !context.IsAnyOf(TestProvName.AllYdb))
			{
				Assert.That(dc.LastQuery, Contains.Substring("WITH"));
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3407")]
		public void LoadWithInsideCte([CteContextSource] string context)
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
		public void CteSelfJoinWithDuplicateColumns([CteContextSource] string context)
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
		public void RecursiveCteOfMappedEntity([RecursiveCteContextSource] string context)
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
		public void RecursiveCteOfEntityWrapper([RecursiveCteContextSource] string context)
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

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1845")]
		public void UnionOfCteWithLocalCollectionAsCte([CteContextSource] string context)
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

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4012")]
		public void UnionOfRecursiveCtesWithLoadWith([RecursiveCteContextSource] string context)
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

		[Table]
		public record StockAddress
		{
			[PrimaryKey] public int Id { get; set; }
			[Column] public string? Address1 { get; set; }
			[Column] public string? City { get; set; }
			[Column] public string? State { get; set; }
			[Column] public string? Zip { get; set; }
		}

		[Table]
		public class StockWarehouse
		{
			[PrimaryKey] public int Id { get; set; }
			[Column] public string? Name { get; set; }
			[Column] public int AddressId { get; set; }
		}

		[Table]
		public record StockUnitOfMeasure
		{
			[PrimaryKey] public int Id { get; set; }
			[Column] public string? Name { get; set; }
			[Column] public string? Abbreviation { get; set; }
		}

		[Table]
		public record StockProduct
		{
			[PrimaryKey] public int Id { get; set; }
			[Column] public string? Description { get; set; }
			[Column] public string? Sku { get; set; }
			[Column] public int UnitOfMeasureId { get; set; }
		}

		[Table]
		public record StockProductIncludedProduct
		{
			[PrimaryKey] public int ProductId { get; set; }
			[PrimaryKey] public int IncludedProductId { get; set; }
			[Column] public decimal Quantity { get; set; }
		}

		[Table]
		public record StockWarehouseProduct
		{
			[PrimaryKey] public int WarehouseId { get; set; }
			[PrimaryKey] public int ProductId { get; set; }
			[Column(Precision = 10, Scale = 0)] public decimal StockOnHand { get; set; }
		}

		[ThrowsRequiresCorrelatedSubquery]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4717")]
		public void SubqueriesCorrelatedToCteSourceInProjection([CteContextSource] string context)
		{
			using var db = GetDataContext(context);

			using var addressTable = db.CreateLocalTable<StockAddress>();
			using var warehouseTable = db.CreateLocalTable<StockWarehouse>();
			using var unitOfMeasureTable = db.CreateLocalTable<StockUnitOfMeasure>();
			using var productTable = db.CreateLocalTable<StockProduct>();
			using var productIncludedProductMappingTable = db.CreateLocalTable<StockProductIncludedProduct>();
			using var warehouseProductMappingTable = db.CreateLocalTable<StockWarehouseProduct>();

			addressTable.Insert(() => new StockAddress()
			{
				Id = 1,
				Address1 = "123 Test St",
				City = "Test City",
				State = "TS",
				Zip = "12345"
			});

			warehouseTable.Insert(() => new StockWarehouse()
			{
				Id = 1,
				Name = "Test Warehouse",
				AddressId = 1
			});

			unitOfMeasureTable.Insert(() => new StockUnitOfMeasure()
			{
				Id = 1,
				Name = "Test Warehouse",
				Abbreviation = "ea"
			});

			var productId = 1;
			productTable.Insert(() => new StockProduct()
			{
				Id = productId,
				Sku = "123-SKU",
				Description = "Test 123 Sku",
				UnitOfMeasureId = 1
			});

			var includedProductId = 2;
			productTable.Insert(() => new StockProduct()
			{
				Id = includedProductId,
				Sku = "ABC-SKU",
				Description = "Test ABC Sku",
				UnitOfMeasureId = 1
			});

			productIncludedProductMappingTable.Insert(() => new StockProductIncludedProduct()
			{
				ProductId = productId,
				IncludedProductId = includedProductId,
				Quantity = 10
			});

			warehouseProductMappingTable.Insert(() => new StockWarehouseProduct
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
		public void RecursiveCteWithAssociationThenEagerLoad([RecursiveCteContextSource(TestProvName.AllInformix)] string context)
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
		public void CteWithAssociationThenEagerLoad([CteContextSource(TestProvName.AllClickHouse)] string context)
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

		sealed class HierarchyMenu
		{
			[PrimaryKey] public int Id { get; set; }
		}

		sealed class HierarchyMenuItem
		{
			[PrimaryKey] public int Id { get; set; }
			public int MenuId { get; set; }
			public int? ParentItemId { get; set; }
		}

		sealed class HierarchyIdPair
		{
			public int Id1 { get; set; }
			public int? Id2 { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4968")]
		public void RecursiveCteOfTupleInAnyPredicate([RecursiveCteContextSource(TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			using var tm = db.CreateLocalTable<HierarchyMenu>();
			using var ti = db.CreateLocalTable<HierarchyMenuItem>();

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
		public void DeleteByRecursiveCteOfTuple([RecursiveCteContextSource(TestProvName.AllDB2, TestProvName.AllFirebird, TestProvName.AllInformix, TestProvName.AllMySqlWithCTE, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			using var tm = db.CreateLocalTable<HierarchyMenu>();
			using var ti = db.CreateLocalTable<HierarchyMenuItem>();

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
		public void RecursiveCteOfClassInAnyPredicate([RecursiveCteContextSource(TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			using var tm = db.CreateLocalTable<HierarchyMenu>();
			using var ti = db.CreateLocalTable<HierarchyMenuItem>();

			var menuId = 1;

			var cte = db.GetCte<HierarchyIdPair>(
				cteQueryable => ti
					.Where(item => item.MenuId == menuId)
					.Select(item => new HierarchyIdPair() {Id1 = item.Id, Id2 = item.ParentItemId })
					.UnionAll(ti.InnerJoin(
						cteQueryable,
						(item, cte) => item.ParentItemId == cte.Id1,
						(item, cte) => new HierarchyIdPair() {Id1 = item.Id, Id2 = item.ParentItemId })));

			ti.Where(i => cte.Any(t => t.Id1 == i.Id)).ToList();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4968")]
		public void DeleteByRecursiveCteOfClass([RecursiveCteContextSource(TestProvName.AllDB2, TestProvName.AllFirebird, TestProvName.AllInformix, TestProvName.AllMySqlWithCTE, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			using var tm = db.CreateLocalTable<HierarchyMenu>();
			using var ti = db.CreateLocalTable<HierarchyMenuItem>();

			var menuId = 1;

			var cte = db.GetCte<HierarchyIdPair>(
				cteQueryable => ti
					.Where(item => item.MenuId == menuId)
					.Select(item => new HierarchyIdPair() {Id1 = item.Id, Id2 = item.ParentItemId })
					.UnionAll(ti.InnerJoin(
						cteQueryable,
						(item, cte) => item.ParentItemId == cte.Id1,
						(item, cte) => new HierarchyIdPair() {Id1 = item.Id, Id2 = item.ParentItemId })));

			ti.Where(i => cte.Any(tuple => tuple.Id1 == i.Id)).Delete();
		}

		public class DateRangeHelper
		{
			public int      Counter { get; set; }
			public DateTime Date    { get; set; }
		}

		// inlineParams had no value source, so NUnit generated no cases and this test never ran anywhere - the
		// gate below was dead with it. [Values] makes it runnable; only Oracle 11 fails.
		[ActiveIssue(Configuration = TestProvName.AllOracle11, ErrorTypeName = "Oracle.ManagedDataAccess.Client.OracleException",
			Details = "no-issue: declared by type rather than message because the two arms fail differently and no attribute can target a test-case argument: inlined gives ORA-01841 (bad year, matching the old gate's 'Wrong Date manipulations'), parameterised gives ORA-01790 (UNION operand type mismatch). Both are the same driver exception, and LinqService wraps it in RpcException whose detail still names the type.")]
		[Test]
		public void SelectQueryTest([RecursiveCteContextSource] string context, [Values] bool inlineParams)
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

		// Split because the one gate covered two providers failing for unrelated reasons.
		[ActiveIssue(Configuration = TestProvName.AllDuckDB, ErrorTypeName = "DuckDB.NET.Data.DuckDBException",
			ErrorMessage = "Conversion Error: invalid timestamp field format",
			Details = "no-issue: DuckDB does not type a bare date parameter inside EXTRACT, so the value reaches the server as a locale-formatted string.")]
		[ActiveIssue(Configuration = TestProvName.AllOracle11, ErrorMessage = "ORA-01841: (full) year must be between -4713 and +9999, and not be 0",
			Details = "no-issue: wrong date manipulations on Oracle 11. The gate's own prose named ORA-01841 and the Azure Oracle leg confirms it.")]
		[Test]
		// PostgreSQL 9.4+ (make_timestamp)
		public void AggressiveCteOptimization([RecursiveCteContextSource(TestProvName.AllPostgreSQL93Minus)] string context)
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
							   .Select(r => new DateRangeHelper { Counter = r.Counter + 1, Date = r.Date.AddDays(1) })
							   .Where(r => r.Date < dateTo)
					   ));

			var subQuery = cte.Select(r => new
			{
				Date  = r.Date,
				Year  = Sql.MakeDateTime(r.Date.Year,            1, 1),
				Month = Sql.MakeDateTime(r.Date.Year, r.Date.Month, 1)
			});

			AreEqual(
				Enumerable.Range(0, 10)
					.Select(i => dateFrom.AddDays(i))
					.Select(d => new
					{
						Date  = d.Date,
						Year  = Sql.MakeDateTime(d.Date.Year,            1, 1),
						Month = Sql.MakeDateTime(d.Date.Year, d.Date.Month, 1)
					}),
				subQuery
				);

		}

		// AggressiveCteOptimization derives Year and Month from the same CTE column. This one reads two
		// distinct ones, so folding the wrapper into the union has to substitute both per leg.
		[ActiveIssue(Configuration = TestProvName.AllDuckDB, ErrorTypeName = "DuckDB.NET.Data.DuckDBException",
			ErrorMessage = "Conversion Error: invalid timestamp field format",
			Details = "no-issue: as AggressiveCteOptimization.")]
		[ActiveIssue(Configuration = TestProvName.AllOracle11, ErrorMessage = "ORA-01841: (full) year must be between -4713 and +9999, and not be 0",
			Details = "no-issue: as AggressiveCteOptimization.")]
		[Test]
		// PostgreSQL 9.4+ (make_timestamp)
		public void AggressiveCteOptimizationTwoColumns([RecursiveCteContextSource(TestProvName.AllPostgreSQL93Minus)] string context)
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
							   .Select(r => new DateRangeHelper { Counter = r.Counter + 1, Date = r.Date.AddDays(1) })
							   .Where(r => r.Date < dateTo)
					   ));

			var subQuery = cte.Select(r => new
			{
				Date  = r.Date,
				Mixed = Sql.MakeDateTime(r.Date.Year, r.Date.Month, r.Counter)
			});

			AreEqual(
				Enumerable.Range(0, 10)
					.Select(i => new { Date = dateFrom.AddDays(i), Counter = i + 1 })
					.Select(d => new
					{
						Date  = d.Date,
						Mixed = Sql.MakeDateTime(d.Date.Year, d.Date.Month, d.Counter)
					}),
				subQuery
				);
		}

		[Test]
		public void GroupByOverCteField([CteContextSource] string context)
		{
			using var db = GetDataContext(context, o => o.UseGuardGrouping(false));

			var cte = (from gc1 in db.GrandChild
					  select new
					  {
						  ChildID      = gc1.ChildID,
						  ParentID     = gc1.ParentID,
						  GrandChildID = gc1.GrandChildID,
					  }).AsCte();

			_ = cte.GroupBy(r => r.ParentID ?? -1).ToDictionary(r => r.Key);
		}

		[Test]
		public void GroupByOverRecursiveCteField([RecursiveCteContextSource(TestProvName.AllInformix)] string context)
		{
			using var db = GetDataContext(context, o => o.UseGuardGrouping(false));

			var cte = db.GetCte<RecursiveCTE>(cte =>
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
						));

			_ = cte.GroupBy(r => r.ParentID ?? -1).ToDictionary(r => r.Key);
		}

		sealed record class CteGroupByRecord(int Field1, int Field2, int? Field3);

		[Table]
		public class CteGroupByTable1
		{
			[PrimaryKey] public int Id { get; set; }
			[Column] public int Field1 { get; set; }
			[Column] public int? Field2 { get; set; }
		}

		[Table]
		public class CteGroupByTable2
		{
			[PrimaryKey] public int Id { get; set; }
		}

		[Test]
		public void GroupByOverRecursiveCteField2([RecursiveCteContextSource(TestProvName.AllInformix)] string context)
		{
			using var db = GetDataContext(context, o => o.UseGuardGrouping(false));
			using var t1 = db.CreateLocalTable<CteGroupByTable1>();
			using var t2 = db.CreateLocalTable<CteGroupByTable2>();

			var id = 1;

			_ = db.GetCte<CteGroupByRecord>(cte =>
			{
				return
					(from le in t1
						join l in t2 on le.Field1 equals l.Id
						where le.Field1 == id
						select new CteGroupByRecord(le.Id, le.Field1, le.Field2))
					.Concat(
						from le in cte
						join suble in t1 on le.Field3 equals suble.Field1
						join l in t2 on suble.Field1 equals l.Id
						where suble.Field2 != null
						select new CteGroupByRecord(suble.Id, suble.Field1, suble.Field2));
			}).GroupBy(le => le.Field2).ToDictionary(g => g.Key);
		}
		sealed class HierarchyLevelCte
		{
			public int  ParentID     { get; set; }
			public int? ChildID      { get; set; }
			public int? GrandChildID { get; set; }
			public int  Level        { get; set; }
		}

		// https://github.com/linq2db/linq2db/issues/5457
		[Test]
		public void RecursiveHierarchyWithJoinOnCteColumn([RecursiveCteContextSource(TestProvName.AllClickHouse, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var cte = db.GetCte<HierarchyLevelCte>(cte =>
				(
					from p in db.Parent
					select new HierarchyLevelCte
					{
						ParentID     = p.ParentID,
						ChildID      = null,
						GrandChildID = null,
						Level        = 0,
					}
				)
				.Concat
				(
					from c in db.Child
					from ct in cte.InnerJoin(ct => ct.ParentID == c.ParentID)
					where ct.Level < 2
					select new HierarchyLevelCte
					{
						ParentID     = c.ParentID,
						ChildID      = c.ChildID,
						GrandChildID = ct.GrandChildID,
						Level        = ct.Level + 1,
					}
				));

			var query =
				from h in cte
				from p in db.Parent.InnerJoin(p => p.ParentID == h.ChildID)
				select new
				{
					h.ParentID,
					h.ChildID,
					h.Level,
					ParentValue = p.Value1,
				};

			query.ToArray();
		}

		[Table]
		sealed class Projection1
		{
			[PrimaryKey, Column(Length = 50, CanBeNull = false)] public string S1 { get; set; } = null!;
		}

		[Table]
		sealed class Projection2
		{
			[PrimaryKey, Column(Length = 50, CanBeNull = false)] public string S1 { get; set; } = null!;
		}

		// https://github.com/linq2db/linq2db/issues/5359
		[Test]
		public void LetWithLeftJoinInsideCte([CteContextSource] string context)
		{
			using var db = GetDataContext(context);
			using var t1 = db.CreateLocalTable<Projection1>();
			using var t2 = db.CreateLocalTable<Projection2>();

			db.Insert(new Projection1 { S1 = "s1" });
			db.Insert(new Projection2 { S1 = "s1" });

			var query =
				from projection1 in t1
				join projection2 in t2 on projection1.S1 equals projection2.S1 into projection2Join
				from projection2 in projection2Join.DefaultIfEmpty()
				let S1 = projection2.S1
				select new
				{
					S1,
				};

			query.AsCte().ToArray();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1284")]
		public void CteExpressionIsNotATable([CteContextSource] string context)
		{
			using var db = GetDataContext(context);
			var query = db.Person.Select(person => new { entry = person });
			var cte   = query.AsCte();

			var result   = cte.Where(x => x.entry.ID == 1).ToList();
			var expected = query.Where(x => x.entry.ID == 1).ToList();

			AreEqual(expected, result);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1284")]
		public void CteNoFieldList([CteContextSource] string context)
		{
			using var db = GetDataContext(context);
			var query = db.Person
					.Select(person => new { entry = person, rn = 1 })
					.Where(x => x.rn == 1)
					.Select(x => x.entry);

			var cte = query
					.AsCte("cte");

			var expected = query;

			AreEqual(expected, cte);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1284")]
		public void CteInvalidMapping([CteContextSource] string context)
		{
			using var db = GetDataContext(context);
			var query = db.Person
					.Select(person => new { entry = person, rn = 1 })
					.Where(x => x.rn == 1);

			var cte = query
					.AsCte();

			var item = cte.First();

			var expected = query
					.First();

			Assert.That(item, Is.EqualTo(expected));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1284")]
		public void CteInvalidMappingUnion([CteContextSource] string context)
		{
			using var db = GetDataContext(context);
			var query = db.Person
					.Select(person => new { entry = person, rn = 1 })
					.Concat(db.Person
					.Select(person => new { entry = person, rn = 2 }))
					.Where(x => x.rn == 1);

			var cte = query
					.AsCte();

			var item = cte.First();

			var expected = query
					.First();

			Assert.That(item, Is.EqualTo(expected));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1284")]
		public void CteReservedWords([CteContextSource] string context)
		{
			using var db = GetDataContext(context);
			var query = db.Person
					.Select(person => new
					{
						x = new
						{
							Obj = new
							{
								Operator = person.LastName
							}
						},
					})
					.Select(x => x.x);

			var cte = query
					.AsCte();

			var item = cte.FirstOrDefault();
			var expected = query.FirstOrDefault();

			Assert.That(item, Is.EqualTo(expected));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1564")]
		// PostgreSQL 9.4+ (FILTER clause) - via StringAggregate, which routes through the ungated TranslateStringJoin
		public void RecursiveCteStringAggregateOrderedByLevel([IncludeDataSources(TestProvName.AllPostgreSQL95Plus)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<CteCategory>())
			{
				var pathQuery = GetPathQuery(db);

				var adminCategoriesQuery =
					from c in db.GetTable<CteCategory>()
					select new CategoryPathPreview
					{
						Id           = c.Id,
						IsVisible    = c.IsVisible,
						DisplayOrder = c.DisplayOrder,
						FullPath     = pathQuery
							.Where(c1 => c1.RootCategoryId == c.Id)
							.StringAggregate(" -> ", i => i.Name).OrderByDescending(z => z.Level)
							.ToValue()
					};

				pathQuery.ToList();
				adminCategoriesQuery.ToList();

				Assert.That(adminCategoriesQuery.ToSqlQuery().Sql, Does.Contain("ORDER BY"));
			}

			IQueryable<CategoryPathItemCte> GetPathQuery(IDataContext db)
			{
				var categoryPathCte = db.GetCte<CategoryPathItemCte>(categoryHierarchy =>
				{
					return
						(
							from innerC in db.GetTable<CteCategory>()
							select new CategoryPathItemCte
							{
								CategoryId = innerC.Id,
								ParentCategoryId = innerC.ParentId,
								Name = innerC.Name,
								RootCategoryId = innerC.Id,
								Level = 0
							}
						)
						.Concat
						(
							from c in db.GetTable<CteCategory>()
							from ch in categoryHierarchy.InnerJoin(ch => ch.ParentCategoryId == c.Id)
							select new CategoryPathItemCte
							{
								CategoryId = c.Id,
								ParentCategoryId = c.ParentId,
								Name = c.Name,
								RootCategoryId = ch.RootCategoryId,
								Level = ch.Level + 1
							}
						);
				});

				return categoryPathCte;
			}
		}

		[Table]
		sealed class CteCategory
		{
			[PrimaryKey] public int     Id           { get; set; }
			[Column]     public bool    IsVisible    { get; set; }
			[Column]     public int     DisplayOrder { get; set; }
			[Column]     public int     ParentId     { get; set; }
			[Column]     public string? Name         { get; set; }
		}

		sealed class CategoryPathPreview
		{
			public int     Id;
			public bool    IsVisible;
			public int     DisplayOrder;
			public string? FullPath;
		}

		sealed class CategoryPathItemCte
		{
			public int     CategoryId;
			public int     ParentCategoryId;
			public string? Name;
			public int     RootCategoryId;
			public int     Level;
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1584")]
		public void CteWithEntityGroupKeyOverLeftJoins([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<RateEntry>())
			using (db.CreateLocalTable<RateLines>())
			using (db.CreateLocalTable<RateLineItem>())
			{
				var cte = (from rateEntry in db.GetTable<RateEntry>()
						   from rateLine in db.GetTable<RateLines>().LeftJoin(lines => lines.TL_TI == rateEntry.TI_PK)
						   from rateLineItem in db.GetTable<RateLineItem>().LeftJoin(items =>
						items.TM_TL == rateLine.TL_PK)
						   where (rateEntry.TI_RateEndDate == null || rateEntry.TI_RateEndDate > Sql.CurrentTimestamp)
						  && rateLineItem.TM_Type.In("MIN", "FLT", "BAS", "UNT")
						   group rateLineItem by rateEntry
					into s
						   select new RateCharges
						   {
							   RateEntry = s.Key,
							   FlatRate = s.Sum(r => r.TM_Type.In("FLT", "BAS") ? r.TM_Value : 0),
							   MinRate = s.Sum(r => r.TM_Type == "MIN" ? r.TM_Value : 0),
							   VariableRate = s.Sum(r => r.TM_Type == "UNT" ? r.TM_Value : 0)
						   }).AsCte("rateCost");

				var query = from rateEntry in db.GetTable<RateEntry>()
							from c in cte.InnerJoin(c => c.RateEntry!.TI_PK == rateEntry.TI_PK)
							select new
							{
								RateEntry = rateEntry,
								CTE = c
							};
				var result = query.ToArray();
			}
		}

		public enum TransportType
		{
			Udp                = 1,
			Connectionless     = 1,
			Tcp                = 2,
			ConnectionOriented = 2,
			All                = 3
		}
		private sealed class RateCharges
		{
			public RateEntry? RateEntry    { get; set; }
			public decimal    FlatRate     { get; set; }
			public decimal    MinRate      { get; set; }
			public decimal    VariableRate { get; set; }
		}

		public class RateEntry
		{
			public const string RateCategoryOrigin      = "ORG";
			public const string RateCategoryDestination = "DST";
			public const string HazardoudsCommodityCode = "HAZ";

			public Guid      TI_PK                                { get; set; }
			public short     TI_LineOrder                         { get; set; }
			public DateTime  TI_RateStartDate                     { get; set; }
			public DateTime? TI_RateEndDate                       { get; set; }
			public string?   TI_RX_NKCurrency                     { get; set; }
			public int       TI_Frequency                         { get; set; }
			public Guid?     TI_OH_Supplier                       { get; set; }
			public Guid?     TI_OH_TransportProvider              { get; set; }
			public Guid?     TI_OH_Consignor                      { get; set; }
			public Guid?     TI_OH_Consignee                      { get; set; }
			public Guid?     TI_OA_CartagePickupAddressOverride   { get; set; }
			public Guid?     TI_OA_CartageDeliveryAddressOverride { get; set; }
			public string?   TI_CartagePickupAddressPostCode      { get; set; }
			public string?   TI_CartageDeliveryAddressPostCode    { get; set; }
			public string?   TI_RS_NKServiceLevel_NI              { get; set; }
			public string?   TI_OriginLRC                         { get; set; }
			public string?   TI_DestinationLRC                    { get; set; }
			public string?   TI_ViaLRC                            { get; set; }
			public string?   TI_PageHeading                       { get; set; }
			public string?   TI_PageOpeningText                   { get; set; }
			public string?   TI_PageClosingText                   { get; set; }
			public Guid?     TI_OH_AgentOverride                  { get; set; }
			public Guid?     TI_ParentID                          { get; set; }
			public string?   TI_QuotePageIncoTerm                 { get; set; }
			public string?   TI_BuyersConsolRateMode              { get; set; }
			public DateTime? TI_SystemCreateTimeUtc               { get; set; }
			public string?   TI_SystemCreateUser                  { get; set; }
			public DateTime? TI_SystemLastEditTimeUtc             { get; set; }
			public string?   TI_SystemLastEditUser                { get; set; }
			public Guid      TI_TH                                { get; set; }
			public Guid?     TI_RC                                { get; set; }
			public string?   TI_ContractNumber                    { get; set; }
			public string?   TI_PL_NKCarrierServiceLevel          { get; set; }
			public Guid?     TI_TZ_OriginZone                     { get; set; }
			public Guid?     TI_TZ_DestinationZone                { get; set; }
			public bool      TI_IsValid                           { get; set; }
			public bool      TI_IsCrossTrade                      { get; set; }
			public bool      TI_DataChecked                       { get; set; }
			public bool      TI_MatchContainerRateClass           { get; set; }
			public string?   TI_TransitTime                       { get; set; }
			public string?   TI_FrequencyUnit                     { get; set; }

			[ColumnAlias(nameof(TI_Mode)), Column(IsColumn = false)]
			public TransportType? TI_ModeTT { get; set; }

			public string? TI_Mode               { get; set; }
			public string? TI_RH_NKCommodityCode { get; set; }
			public string? TI_RateCategory       { get; set; }

			[ColumnAlias(nameof(TI_RateCategory)), Column(IsColumn = false)]
			public TransportType? TI_RateCategoryTT { get; set; }

			public Guid?   TI_FromID                 { get; set; }
			public string? TI_FromTableCode          { get; set; }
			public Guid?   TI_ToId                   { get; set; }
			public string? TI_ToTableCode            { get; set; }
			public Guid?   TI_OH_ControllingCustomer { get; set; }
			public Guid    TI_GC_Publisher           { get; set; }
			public bool    TI_IsTact                 { get; set; }
			public string? TI_ParentTableCode        { get; set; }
			public int     TI_RateKey                { get; set; }

			[ExpressionMethod(nameof(GetModeExpression))]
			public TransportType? GetMode()
			{
				return TI_RateCategory switch
				{
					RateCategoryDestination or RateCategoryOrigin => TI_ModeTT,
					_                                             => TI_RateCategoryTT,
				};
			}

			private static Expression<Func<RateEntry, TransportType?>> GetModeExpression()
			{
				return rateEntry => rateEntry.TI_RateCategory == RateCategoryDestination
									|| rateEntry.TI_RateCategory == RateCategoryOrigin
					? rateEntry.TI_ModeTT
					: rateEntry.TI_RateCategoryTT;
			}
		}

		public class RateLines
		{
			public Guid TL_PK { get; set; }
			//a bunch of properties removed
			public Guid TL_TI { get; set; }
		}

		public class RateLineItem
		{
			public Guid    TM_PK        { get; set; }
			public byte    TM_LineOrder { get; set; }
			public string? TM_Type      { get; set; }
			public decimal TM_Value     { get; set; }
			public Guid    TM_TL        { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1817")]
		public void CteWithLetOverUnionSubquery([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var accountIds = new long[] { 1, 2, 3, 4 };

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<CteLetAccount>())
			using (db.CreateLocalTable<CteLetTransaction>())
			{
				var accountsInfo =
					from a in db.GetTable<CteLetAccount>()
					where a.Id.In(accountIds)
					select new
					{
						a.Id,
						a.Number,
						a.Name,
						a.Type,
						incomeBalance = Sql.AsSql(1),
						outgoingBalance = Sql.AsSql(2)
					};

				var accountsCte = accountsInfo.AsCte("accountsInfo");

				var query = from cte in accountsCte
					let max = db.GetTable<CteLetTransaction>().Where(_ => _.DebitAccountId == cte.Id)
						.Select(_ => _.OperDate)
						.Union(db.GetTable<CteLetTransaction>().Where(_ => _.CreditAccountId == cte.Id)
							.Select(_ => _.OperDate))
						.Max()
					select new
					{
						AccountId = cte.Id,
						AccountNumber = cte.Number,
						AccountType = cte.Type,
						AccountName = cte.Name,
						LastOperDate = max,
						IncomeBalance = cte.incomeBalance,
						OutgoingBalance = cte.outgoingBalance
					};

				var accInfo = query.ToArray();
			}
		}

		public partial class CteLetAccount
		{
			#region Simple columns

			public long Id { get; set; } // bigint
			public string? Number { get; set; } // varchar(20)
			public string? Name { get; set; } // varchar(5000)
			public CteLetAccountType Type { get; set; } // smallint
			public CteLetAccountState State { get; set; } // smallint

			#endregion
		}

		public partial class CteLetTransaction
		{
			#region Simple columns

			public long Id { get; set; } // bigint
			public long RequestId { get; set; } // bigint
			public DateTime OperDate { get; set; } // date
			public long DebitAccountId { get; set; } // bigint
			public long CreditAccountId { get; set; } // bigint

			#endregion
		}

		public enum CteLetAccountState
		{
		}

		public enum CteLetAccountType
		{
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/2033")]
		public void ChainedCteColumnListMatchesSelectList([IncludeDataSources(false, TestProvName.AllOracle)] string context)
		{
			using (var db = new WipDB(context))
			using (db.CreateLocalTable<WipNcCode>())
			using (db.CreateLocalTable<WipNcGroupMember>())
			using (db.CreateLocalTable<SfcRouting>())
			using (db.CreateLocalTable<SfcRouter>())
			using (db.CreateLocalTable<RouterStep>())
			using (db.CreateLocalTable<RouterOperation>())
			using (db.CreateLocalTable<CustomFields>())
			using (db.CreateLocalTable<Router>())
			using (db.CreateLocalTable<SfcStep>())
			using (db.CreateLocalTable<Operation>())
			using (db.CreateLocalTable<Sfc>())
			using (db.CreateLocalTable<NcData>())
			using (db.CreateLocalTable<Site>())
			using (db.CreateLocalTable<Usr>())
			using (db.CreateLocalTable<ShopOrder>())
			using (db.CreateLocalTable<Resrce>())
			using (db.CreateLocalTable<WorkCenter>())
			using (db.CreateLocalTable<WorkCenterMember>())
			using (db.CreateLocalTable<Item>())
			using (db.CreateLocalTable<ItemGroupMember>())
			using (db.CreateLocalTable<ItemGroup>())
			{
				var sfcBo = "SFCBO:8110,C17C05016";

				var query =
					from item in GetAdditionalData(db,
						FilterByTestOperation(db, FindProductionFailedNcData(db, GetAllowedNcCode(db))))
					where item.Parent!.Parent!.SfcBo == sfcBo
					select item;

				query.ToArray();
			}
		}

		[Table(Name = "WIP_NC_CODE")]
		public partial class WipNcCode
		{
			[Column("HANDLE"), NotNull] public string Handle { get; set; } = null!; // NVARCHAR2(1236)
			[Column("CHANGE_STAMP"), Nullable] public decimal? ChangeStamp { get; set; } // NUMBER (38,0)
			[Column("SITE"), Nullable] public string? Site { get; set; } // NVARCHAR2(18)
			[Column("NC_CODE"), Nullable] public string? NcCodeColumn { get; set; } // NVARCHAR2(48)
			[Column("DESCRIPTION"), Nullable] public string? Description { get; set; } // NVARCHAR2(120)
			[Column("STATUS_BO"), Nullable] public string? StatusBo { get; set; } // NVARCHAR2(1236)
			[Column("CREATED_DATE_TIME"), Nullable] public DateTime? CreatedDateTime { get; set; } // DATE
			[Column("MODIFIED_DATE_TIME"), Nullable] public DateTime? ModifiedDateTime { get; set; } // DATE
			[Column("NC_CATEGORY"), Nullable] public string? NcCategory { get; set; } // NVARCHAR2(60)
			[Column("DPMO_CATEGORY_BO"), Nullable] public string? DpmoCategoryBo { get; set; } // NVARCHAR2(1236)
		}
		[Table(Name = "WIP_NC_GROUP_MEMBER")]
		public partial class WipNcGroupMember
		{
			[Column("HANDLE"), NotNull] public string Handle { get; set; } = null!; // NVARCHAR2(1236)
			[Column("NC_GROUP_BO"), Nullable] public string? NcGroupBo { get; set; } // NVARCHAR2(1236)
			[Column("NC_CODE_OR_GROUP_GBO"), Nullable] public string? NcCodeOrGroupGbo { get; set; } // NVARCHAR2(1236)
			[Column("SEQUENCE"), Nullable] public decimal? Sequence { get; set; } // NUMBER (38,0)
		}

		[Table(Name = "SFC_ROUTING")]
		public partial class SfcRouting
		{
			[Column("HANDLE"), NotNull] public string Handle { get; set; } = null!; // NVARCHAR2(1236)
			[Column("CHANGE_STAMP"), Nullable] public decimal? ChangeStamp { get; set; } // NUMBER (38,0)
			[Column("SFC_BO"), Nullable] public string? SfcBo { get; set; } // NVARCHAR2(1236)
			[Column("PARTITION_DATE"), NotNull] public DateTime PartitionDate { get; set; } // TIMESTAMP(6)
			[Column("CREATED_DATE_TIME"), Nullable] public DateTime? CreatedDateTime { get; set; } // DATE
			[Column("MODIFIED_DATE_TIME"), Nullable] public DateTime? ModifiedDateTime { get; set; } // DATE
		}
		[Table(Name = "SFC_ROUTER")]
		public partial class SfcRouter
		{
			[Column("HANDLE"), NotNull] public string Handle { get; set; } = null!; // NVARCHAR2(1236)
			[Column("SFC_ROUTING_BO"), Nullable] public string? SfcRoutingBo { get; set; } // NVARCHAR2(1236)
			[Column("ROUTER_BO"), Nullable] public string? RouterBo { get; set; } // NVARCHAR2(1236)
			[Column("SEQUENCE"), Nullable] public decimal? Sequence { get; set; } // NUMBER (38,0)
			[Column("IN_USE"), Nullable] public string? InUse { get; set; } // NVARCHAR2(15)
			[Column("COMPLETED"), Nullable] public string? Completed { get; set; } // NVARCHAR2(15)
			[Column("SUB_ROUTER"), Nullable] public string? SubRouter { get; set; } // NVARCHAR2(15)
			[Column("QTY"), Nullable] public decimal? Qty { get; set; } // NUMBER (38,6)
			[Column("REWORKED_FROM_SFC_STEP_BO"), Nullable] public string? ReworkedFromSfcStepBo { get; set; } // NVARCHAR2(1236)
			[Column("RETURN_TYPE"), Nullable] public string? ReturnType { get; set; } // NVARCHAR2(3)
			[Column("SUB_TYPE"), Nullable] public string? SubType { get; set; } // NVARCHAR2(3)
			[Column("PARTITION_DATE"), NotNull] public DateTime PartitionDate { get; set; } // TIMESTAMP(6)
		}
		[Table(Name = "SFC_STEP")]
		public partial class SfcStep
		{
			[Column("HANDLE"), NotNull] public string Handle { get; set; } = null!; // NVARCHAR2(1236)
			[Column("SFC_ROUTER_BO"), Nullable] public string? SfcRouterBo { get; set; } // NVARCHAR2(1236)
			[Column("STEP_ID"), Nullable] public string? StepId { get; set; } // NVARCHAR2(18)
			[Column("OPERATION_BO"), Nullable] public string? OperationBo { get; set; } // NVARCHAR2(1236)
			[Column("DONE"), Nullable] public string? Done { get; set; } // NVARCHAR2(15)
			[Column("BYPASSED"), Nullable] public string? Bypassed { get; set; } // NVARCHAR2(15)
			[Column("QTY_IN_QUEUE"), Nullable] public decimal? QtyInQueue { get; set; } // NUMBER (38,6)
			[Column("QTY_IN_WORK"), Nullable] public decimal? QtyInWork { get; set; } // NUMBER (38,6)
			[Column("QTY_COMPLETED"), Nullable] public decimal? QtyCompleted { get; set; } // NUMBER (38,6)
			[Column("QTY_COMPLETE_PENDING"), Nullable] public decimal? QtyCompletePending { get; set; } // NUMBER (38,6)
			[Column("QTY_REJECTED"), Nullable] public decimal? QtyRejected { get; set; } // NUMBER (38,6)
			[Column("TIMES_PROCESSED"), Nullable] public decimal? TimesProcessed { get; set; } // NUMBER (38,0)
			[Column("MAX_LOOP"), Nullable] public decimal? MaxLoop { get; set; } // NUMBER (38,0)
			[Column("USE_AS_REWORK"), Nullable] public string? UseAsRework { get; set; } // NVARCHAR2(15)
			[Column("PREVIOUSLY_STARTED"), Nullable] public string? PreviouslyStarted { get; set; } // NVARCHAR2(15)
			[Column("LAST_WORK_CENTER_BO"), Nullable] public string? LastWorkCenterBo { get; set; } // NVARCHAR2(1236)
			[Column("PREVIOUS_STEP_ID"), Nullable] public string? PreviousStepId { get; set; } // NVARCHAR2(18)
			[Column("DATE_QUEUED"), Nullable] public DateTime? DateQueued { get; set; } // DATE
			[Column("REPORTING_STEP"), Nullable] public string? ReportingStep { get; set; } // NVARCHAR2(108)
			[Column("STEP_SEQUENCE"), Nullable] public decimal? StepSequence { get; set; } // NUMBER (38,0)
			[Column("RESOURCE_OR_CENTER_GBO"), Nullable] public string? ResourceOrCenterGbo { get; set; } // NVARCHAR2(1236)
			[Column("STATE"), Nullable] public string? State { get; set; } // NVARCHAR2(3)
			[Column("REPORTING_CENTER_BO"), Nullable] public string? ReportingCenterBo { get; set; } // NVARCHAR2(1236)
			[Column("PREVIOUS_RESOURCE_BO"), Nullable] public string? PreviousResourceBo { get; set; } // NVARCHAR2(1236)
			[Column("MULTI_Q_SIGNOFF_NEEDED"), Nullable] public string? MultiQSignoffNeeded { get; set; } // NVARCHAR2(15)
			[Column("STEP_PULLED_INTO"), Nullable] public string? StepPulledInto { get; set; } // NVARCHAR2(18)
			[Column("PARTITION_DATE"), NotNull] public DateTime PartitionDate { get; set; } // TIMESTAMP(6)
			[Column("LOCAL_REWORK"), Nullable] public string? LocalRework { get; set; } // NVARCHAR2(3)
			[Column("SPECIAL_INSTRUCTION"), Nullable] public string? SpecialInstruction { get; set; } // NVARCHAR2(384)
			[Column("FUTURE_HOLD_COUNT"), Nullable] public decimal? FutureHoldCount { get; set; } // NUMBER (38,0)
			[Column("ERP_SENT"), Nullable] public string? ErpSent { get; set; } // NVARCHAR2(15)
			[Column("SCRAP_REPORTING_STEP"), Nullable] public string? ScrapReportingStep { get; set; } // NVARCHAR2(108)
			[Column("ERP_TB_SENT"), Nullable] public string? ErpTbSent { get; set; } // NVARCHAR2(15)
		}
		[Table(Name = "ROUTER_STEP")]
		public partial class RouterStep
		{
			[Column("HANDLE"), NotNull] public string Handle { get; set; } = null!; // NVARCHAR2(1236)
			[Column("ROUTER_BO"), Nullable] public string? RouterBo { get; set; } // NVARCHAR2(1236)
			[Column("STEP_ID"), Nullable] public string? StepId { get; set; } // NVARCHAR2(18)
			[Column("DESCRIPTION"), Nullable] public string? Description { get; set; } // NVARCHAR2(120)
			[Column("REWORK"), Nullable] public string? Rework { get; set; } // NVARCHAR2(15)
			[Column("QUEUE_DECISION_TYPE"), Nullable] public string? QueueDecisionType { get; set; } // NVARCHAR2(3)
			[Column("ROUTER_COMP_GBO"), Nullable] public string? RouterCompGbo { get; set; } // NVARCHAR2(1236)
			[Column("REPORTING_STEP"), Nullable] public string? ReportingStep { get; set; } // NVARCHAR2(108)
			[Column("SEQUENCE"), Nullable] public decimal? Sequence { get; set; } // NUMBER (38,0)
			[Column("REPORTING_CENTER_BO"), Nullable] public string? ReportingCenterBo { get; set; } // NVARCHAR2(1236)
			[Column("TABULAR_STEP_TYPE"), Nullable] public string? TabularStepType { get; set; } // NVARCHAR2(3)
			[Column("SCRAP_REPORTING_STEP"), Nullable] public string? ScrapReportingStep { get; set; } // NVARCHAR2(108)
			[Column("IS_LAST_REPORTING_STEP"), Nullable] public string? IsLastReportingStep { get; set; } // NVARCHAR2(15)
			[Column("ERP_SEQUENCE"), Nullable] public string? ErpSequence { get; set; } // NVARCHAR2(18)
			[Column("ERP_CONTROL_KEY_BO"), Nullable] public string? ErpControlKeyBo { get; set; } // NVARCHAR2(1236)
			[Column("ERP_WORK_CENTER_BO"), Nullable] public string? ErpWorkCenterBo { get; set; } // NVARCHAR2(1236)
			[Column("ERP_INSPECTION_COMPLETE"), Nullable] public string? ErpInspectionComplete { get; set; } // NVARCHAR2(15)
		}
		[Table(Name = "ROUTER_OPERATION")]
		public partial class RouterOperation
		{
			[Column("HANDLE"), NotNull] public string Handle { get; set; } = null!; // NVARCHAR2(1236)
			[Column("ROUTER_STEP_BO"), Nullable] public string? RouterStepBo { get; set; } // NVARCHAR2(1236)
			[Column("OPERATION_BO"), Nullable] public string? OperationBo { get; set; } // NVARCHAR2(1236)
			[Column("MAX_LOOP"), Nullable] public decimal? MaxLoop { get; set; } // NUMBER (38,0)
			[Column("REQUIRED_TIME_IN_PROCESS"), Nullable] public decimal? RequiredTimeInProcess { get; set; } // NUMBER (38,0)
			[Column("STEP_TYPE"), Nullable] public string? StepType { get; set; } // NVARCHAR2(3)
			[Column("SPECIAL_INSTRUCTION"), Nullable] public string? SpecialInstruction { get; set; } // NVARCHAR2(384)
		}
		[Table(Name = "ROUTER")]
		public partial class Router
		{
			[Column("HANDLE"), NotNull] public string Handle { get; set; } = null!; // NVARCHAR2(1236)
			[Column("CHANGE_STAMP"), Nullable] public decimal? ChangeStamp { get; set; } // NUMBER (38,0)
			[Column("SITE"), Nullable] public string? Site { get; set; } // NVARCHAR2(18)
			[Column("ROUTER"), Nullable] public string? RouterColumn { get; set; } // NVARCHAR2(384)
			[Column("ROUTER_TYPE"), Nullable] public string? RouterType { get; set; } // NVARCHAR2(3)
			[Column("DESCRIPTION"), Nullable] public string? Description { get; set; } // NVARCHAR2(120)
			[Column("TEMPORARY_ROUTER"), Nullable] public string? TemporaryRouter { get; set; } // NVARCHAR2(15)
			[Column("STATUS_BO"), Nullable] public string? StatusBo { get; set; } // NVARCHAR2(1236)
			[Column("ENTRY_ROUTER_STEP_BO"), Nullable] public string? EntryRouterStepBo { get; set; } // NVARCHAR2(1236)
			[Column("COPIED_FROM_ROUTER_BO"), Nullable] public string? CopiedFromRouterBo { get; set; } // NVARCHAR2(1236)
			[Column("REVISION"), Nullable] public string? Revision { get; set; } // NVARCHAR2(60)
			[Column("CURRENT_REVISION"), Nullable] public string? CurrentRevision { get; set; } // NVARCHAR2(15)
			[Column("HAS_BEEN_RELEASED"), Nullable] public string? HasBeenReleased { get; set; } // NVARCHAR2(15)
			[Column("EFF_START_DATE"), Nullable] public DateTime? EffStartDate { get; set; } // DATE
			[Column("EFF_END_DATE"), Nullable] public DateTime? EffEndDate { get; set; } // DATE
			[Column("CREATED_DATE_TIME"), Nullable] public DateTime? CreatedDateTime { get; set; } // DATE
			[Column("MODIFIED_DATE_TIME"), Nullable] public DateTime? ModifiedDateTime { get; set; } // DATE
			[Column("GUI_REPRESENTATION"), Nullable] public string? GuiRepresentation { get; set; } // NCLOB
			[Column("ORIGINAL_STATUS_BO"), Nullable] public string? OriginalStatusBo { get; set; } // NVARCHAR2(1236)
			[Column("DISPOSITION_GROUP_BO"), Nullable] public string? DispositionGroupBo { get; set; } // NVARCHAR2(1236)
			[Column("PREV_SITE"), Nullable] public string? PrevSite { get; set; } // NVARCHAR2(18)
			[Column("ORIGINAL_TRANSFER_KEY"), Nullable] public string? OriginalTransferKey { get; set; } // NVARCHAR2(1236)
			[Column("DISPLAY_TYPE"), Nullable] public string? DisplayType { get; set; } // NVARCHAR2(3)
			[Column("HOLD_ID"), Nullable] public decimal? HoldId { get; set; } // NUMBER (38,0)
			[Column("SEND_AS_SHARED"), Nullable] public string? SendAsShared { get; set; } // NVARCHAR2(15)
			[Column("SENT_TO_ERP"), Nullable] public string? SentToErp { get; set; } // NVARCHAR2(15)
			[Column("ERP_CHANGE_NUMBER"), Nullable] public string? ErpChangeNumber { get; set; } // NVARCHAR2(36)
			[Column("RELAXED_FLOW"), Nullable] public string? RelaxedFlow { get; set; } // NVARCHAR2(15)
			[Column("BOM_BO"), Nullable] public string? BomBo { get; set; } // NVARCHAR2(1236)
		}

		[Table(Name = "CUSTOM_FIELDS")]
		public partial class CustomFields
		{
			[Column("HANDLE"), Nullable] public string? Handle { get; set; } // NVARCHAR2(1236)
			[Column("ATTRIBUTE"), Nullable] public string? Attribute { get; set; } // NVARCHAR2(180)
			[Column("VALUE"), Nullable] public string? Value { get; set; } // NVARCHAR2(3072)
			[Column("CREATED_DATE_TIME"), Nullable] public DateTime? CreatedDateTime { get; set; } // DATE
			[Column("MODIFIED_DATE_TIME"), Nullable] public DateTime? ModifiedDateTime { get; set; } // DATE
		}
		[Table(Name = "NC_DATA")]
		public partial class NcData
		{
			[Column("HANDLE"), NotNull] public string Handle { get; set; } = null!; // NVARCHAR2(1236)
			[Column("CHANGE_STAMP"), Nullable] public decimal? ChangeStamp { get; set; } // NUMBER (38,0)
			[Column("NC_CONTEXT_GBO"), Nullable] public string? NcContextGbo { get; set; } // NVARCHAR2(1236)
			[Column("USER_BO"), Nullable] public string? UserBo { get; set; } // NVARCHAR2(1236)
			[Column("DATE_TIME"), Nullable] public DateTime? DateTime { get; set; } // DATE
			[Column("SEQUENCE"), Nullable] public decimal? Sequence { get; set; } // NUMBER (38,0)
			[Column("SITE"), Nullable] public string? Site { get; set; } // NVARCHAR2(18)
			[Column("PARENT_NC_DATA_BO"), Nullable] public string? ParentNcDataBo { get; set; } // NVARCHAR2(1236)
			[Column("NC_STATE"), Nullable] public string? NcState { get; set; } // NVARCHAR2(3)
			[Column("NC_CODE_BO"), Nullable] public string? NcCodeBo { get; set; } // NVARCHAR2(1236)
			[Column("NC_DATA_TYPE_BO"), Nullable] public string? NcDataTypeBo { get; set; } // NVARCHAR2(1236)
			[Column("QTY"), Nullable] public decimal? Qty { get; set; } // NUMBER (38,6)
			[Column("DEFECT_COUNT"), Nullable] public decimal? DefectCount { get; set; } // NUMBER (38,6)
			[Column("COMPONENT_BO"), Nullable] public string? ComponentBo { get; set; } // NVARCHAR2(1236)
			[Column("COMP_CONTEXT_GBO"), Nullable] public string? CompContextGbo { get; set; } // NVARCHAR2(1236)
			[Column("REF_DES"), Nullable] public string? RefDes { get; set; } // NVARCHAR2(108)
			[Column("COMMENTS"), Nullable] public string? Comments { get; set; } // NCLOB
			[Column("ROUTER_BO"), Nullable] public string? RouterBo { get; set; } // NVARCHAR2(1236)
			[Column("DISPOSITION_ROUTER_BO"), Nullable] public string? DispositionRouterBo { get; set; } // NVARCHAR2(1236)
			[Column("STEP_ID"), Nullable] public string? StepId { get; set; } // NVARCHAR2(18)
			[Column("OPERATION_BO"), Nullable] public string? OperationBo { get; set; } // NVARCHAR2(1236)
			[Column("TIMES_PROCESSED"), Nullable] public decimal? TimesProcessed { get; set; } // NUMBER (38,0)
			[Column("RESOURCE_BO"), Nullable] public string? ResourceBo { get; set; } // NVARCHAR2(1236)
			[Column("WORK_CENTER_BO"), Nullable] public string? WorkCenterBo { get; set; } // NVARCHAR2(1236)
			[Column("ITEM_BO"), Nullable] public string? ItemBo { get; set; } // NVARCHAR2(1236)
			[Column("CLOSURE_REQUIRED"), Nullable] public string? ClosureRequired { get; set; } // NVARCHAR2(15)
			[Column("CLOSED_USER_BO"), Nullable] public string? ClosedUserBo { get; set; } // NVARCHAR2(1236)
			[Column("CLOSED_DATE_TIME"), Nullable] public DateTime? ClosedDateTime { get; set; } // DATE
			[Column("CANCELLED_USER_BO"), Nullable] public string? CancelledUserBo { get; set; } // NVARCHAR2(1236)
			[Column("CANCELLED_DATE_TIME"), Nullable] public DateTime? CancelledDateTime { get; set; } // DATE
			[Column("INCIDENT_DATE_TIME"), Nullable] public DateTime? IncidentDateTime { get; set; } // DATE
			[Column("IDENTIFIER"), Nullable] public string? Identifier { get; set; } // NVARCHAR2(120)
			[Column("FAILURE_ID"), Nullable] public string? FailureId { get; set; } // NVARCHAR2(120)
			[Column("VERIFIED_STATE"), Nullable] public string? VerifiedState { get; set; } // NVARCHAR2(3)
			[Column("CREATED_DATE_TIME"), Nullable] public DateTime? CreatedDateTime { get; set; } // DATE
			[Column("MODIFIED_DATE_TIME"), Nullable] public DateTime? ModifiedDateTime { get; set; } // DATE
			[Column("NC_CATEGORY"), Nullable] public string? NcCategory { get; set; } // NVARCHAR2(60)
			[Column("VERIFIED_DATE_TIME"), Nullable] public DateTime? VerifiedDateTime { get; set; } // DATE
			[Column("LOCATION"), Nullable] public string? Location { get; set; } // NVARCHAR2(60)
			[Column("REPORTING_CENTER_BO"), Nullable] public string? ReportingCenterBo { get; set; } // NVARCHAR2(1236)
			[Column("INCIDENT_NUMBER_BO"), Nullable] public string? IncidentNumberBo { get; set; } // NVARCHAR2(1236)
			[Column("DISPOSITION_DONE"), Nullable] public string? DispositionDone { get; set; } // NVARCHAR2(15)
			[Column("ROOT_CAUSE_OPER_BO"), Nullable] public string? RootCauseOperBo { get; set; } // NVARCHAR2(1236)
			[Column("TRANSFERRED_TO_DPMO"), Nullable] public string? TransferredToDpmo { get; set; } // NVARCHAR2(3)
			[Column("COMPONENT_SFC_BO"), Nullable] public string? ComponentSfcBo { get; set; } // NVARCHAR2(1236)
			[Column("COMPONENT_SFC_ITEM_BO"), Nullable] public string? ComponentSfcItemBo { get; set; } // NVARCHAR2(1236)
			[Column("DISPOSITION_FUNCTION_BO"), Nullable] public string? DispositionFunctionBo { get; set; } // NVARCHAR2(1236)
			[Column("ASSEMBLY_INCIDENT_NUM"), Nullable] public string? AssemblyIncidentNum { get; set; } // NVARCHAR2(108)
			[Column("BATCH_INCIDENT_NUM"), Nullable] public string? BatchIncidentNum { get; set; } // NVARCHAR2(108)
			[Column("PREV_SITE"), Nullable] public string? PrevSite { get; set; } // NVARCHAR2(18)
			[Column("ORIGINAL_TRANSFER_KEY"), Nullable] public string? OriginalTransferKey { get; set; } // NVARCHAR2(1236)
			[Column("ACTION_CODE"), Nullable] public string? ActionCode { get; set; } // NVARCHAR2(60)
			[Column("PARTITION_DATE"), NotNull] public DateTime PartitionDate { get; set; } // TIMESTAMP(6)
			[Column("COPIED_FROM_NC_DATA_BO"), Nullable] public string? CopiedFromNcDataBo { get; set; } // NVARCHAR2(1236)
			[Column("TXN_ID"), Nullable] public string? TxnId { get; set; } // NVARCHAR2(150)
		}
		[Table(Name = "SFC")]
		public partial class Sfc
		{
			[Column("HANDLE"), NotNull] public string Handle { get; set; } = null!; // NVARCHAR2(1236)
			[Column("CHANGE_STAMP"), Nullable] public decimal? ChangeStamp { get; set; } // NUMBER (38,0)
			[Column("SITE"), Nullable] public string? Site { get; set; } // NVARCHAR2(18)
			[Column("SFC"), Nullable] public string? SfcColumn { get; set; } // NVARCHAR2(384)
			[Column("STATUS_BO"), Nullable] public string? StatusBo { get; set; } // NVARCHAR2(1236)
			[Column("SHOP_ORDER_BO"), Nullable] public string? ShopOrderBo { get; set; } // NVARCHAR2(1236)
			[Column("QTY"), Nullable] public decimal? Qty { get; set; } // NUMBER (38,6)
			[Column("QTY_DONE"), Nullable] public decimal? QtyDone { get; set; } // NUMBER (38,6)
			[Column("QTY_SCRAPPED"), Nullable] public decimal? QtyScrapped { get; set; } // NUMBER (38,6)
			[Column("QTY_HISTORICAL_MIN"), Nullable] public decimal? QtyHistoricalMin { get; set; } // NUMBER (38,6)
			[Column("QTY_HISTORICAL_MAX"), Nullable] public decimal? QtyHistoricalMax { get; set; } // NUMBER (38,6)
			[Column("ITEM_BO"), Nullable] public string? ItemBo { get; set; } // NVARCHAR2(1236)
			[Column("PRIORITY"), Nullable] public decimal? Priority { get; set; } // NUMBER (38,0)
			[Column("LOCATION"), Nullable] public string? Location { get; set; } // NVARCHAR2(60)
			[Column("RMA_MAX_TIMES_PROCESSED"), Nullable] public decimal? RmaMaxTimesProcessed { get; set; } // NUMBER (38,0)
			[Column("LCC_BO"), Nullable] public string? LccBo { get; set; } // NVARCHAR2(1236)
			[Column("ORIGINAL_STATUS_BO"), Nullable] public string? OriginalStatusBo { get; set; } // NVARCHAR2(1236)
			[Column("QTY_MULT_PERFORMED"), Nullable] public string? QtyMultPerformed { get; set; } // NVARCHAR2(15)
			[Column("ACTUAL_COMP_DATE"), Nullable] public DateTime? ActualCompDate { get; set; } // DATE
			[Column("PREV_SITE"), Nullable] public string? PrevSite { get; set; } // NVARCHAR2(18)
			[Column("ORIGINAL_TRANSFER_KEY"), Nullable] public string? OriginalTransferKey { get; set; } // NVARCHAR2(1236)
			[Column("IMMEDIATE_ARCHIVE"), Nullable] public string? ImmediateArchive { get; set; } // NVARCHAR2(15)
			[Column("TRANSFER_DATETIME"), Nullable] public DateTime? TransferDatetime { get; set; } // DATE
			[Column("TRANSFER_USER"), Nullable] public string? TransferUser { get; set; } // NVARCHAR2(90)
			[Column("SN_DONE"), Nullable] public string? SnDone { get; set; } // NVARCHAR2(15)
			[Column("CREATED_DATE_TIME"), Nullable] public DateTime? CreatedDateTime { get; set; } // DATE
			[Column("MODIFIED_DATE_TIME"), Nullable] public DateTime? ModifiedDateTime { get; set; } // DATE
			[Column("PARTITION_DATE"), NotNull] public DateTime PartitionDate { get; set; } // TIMESTAMP(6)
		}
		[Table(Name = "OPERATION")]
		public partial class Operation
		{
			[Column("HANDLE"), NotNull] public string Handle { get; set; } = null!; // NVARCHAR2(1236)
			[Column("CHANGE_STAMP"), Nullable] public decimal? ChangeStamp { get; set; } // NUMBER (38,0)
			[Column("SITE"), Nullable] public string? Site { get; set; } // NVARCHAR2(18)
			[Column("OPERATION"), Nullable] public string? OperationColumn { get; set; } // NVARCHAR2(108)
			[Column("DESCRIPTION"), Nullable] public string? Description { get; set; } // NVARCHAR2(120)
			[Column("TYPE"), Nullable] public string? Type { get; set; } // NVARCHAR2(3)
			[Column("SPECIAL_ROUTER_BO"), Nullable] public string? SpecialRouterBo { get; set; } // NVARCHAR2(1236)
			[Column("STATUS_BO"), Nullable] public string? StatusBo { get; set; } // NVARCHAR2(1236)
			[Column("RESOURCE_TYPE_BO"), Nullable] public string? ResourceTypeBo { get; set; } // NVARCHAR2(1236)
			[Column("REVISION"), Nullable] public string? Revision { get; set; } // NVARCHAR2(60)
			[Column("CURRENT_REVISION"), Nullable] public string? CurrentRevision { get; set; } // NVARCHAR2(15)
			[Column("EFF_START_DATE"), Nullable] public DateTime? EffStartDate { get; set; } // DATE
			[Column("EFF_END_DATE"), Nullable] public DateTime? EffEndDate { get; set; } // DATE
			[Column("CREATED_DATE_TIME"), Nullable] public DateTime? CreatedDateTime { get; set; } // DATE
			[Column("MODIFIED_DATE_TIME"), Nullable] public DateTime? ModifiedDateTime { get; set; } // DATE
			[Column("PCA_DASHBOARD_MODE"), Nullable] public string? PcaDashboardMode { get; set; } // NVARCHAR2(3)
			[Column("DEFAULT_NC_CODE_BO"), Nullable] public string? DefaultNcCodeBo { get; set; } // NVARCHAR2(1236)
			[Column("FAILURE_TRACKING_CONFIG_BO"), Nullable] public string? FailureTrackingConfigBo { get; set; } // NVARCHAR2(1236)
			[Column("RESOURCE_BO"), Nullable] public string? ResourceBo { get; set; } // NVARCHAR2(1236)
			[Column("MAX_LOOP"), Nullable] public decimal? MaxLoop { get; set; } // NUMBER (38,0)
			[Column("REQUIRED_TIME_IN_PROCESS"), Nullable] public decimal? RequiredTimeInProcess { get; set; } // NUMBER (38,0)
			[Column("REPORTING_STEP"), Nullable] public string? ReportingStep { get; set; } // NVARCHAR2(108)
			[Column("PREV_SITE"), Nullable] public string? PrevSite { get; set; } // NVARCHAR2(18)
			[Column("ORIGINAL_TRANSFER_KEY"), Nullable] public string? OriginalTransferKey { get; set; } // NVARCHAR2(1236)
			[Column("SPECIAL_INSTRUCTION"), Nullable] public string? SpecialInstruction { get; set; } // NVARCHAR2(384)
			[Column("REPORTING_CENTER_BO"), Nullable] public string? ReportingCenterBo { get; set; } // NVARCHAR2(1236)
			[Column("ERP_CONTROL_KEY_BO"), Nullable] public string? ErpControlKeyBo { get; set; } // NVARCHAR2(1236)
			[Column("ERP_WORK_CENTER_BO"), Nullable] public string? ErpWorkCenterBo { get; set; } // NVARCHAR2(1236)
		}

		[Table(Name = "SITE")]
		public partial class Site
		{
			[Column("HANDLE"), NotNull] public string Handle { get; set; } = null!; // NVARCHAR2(1236)
			[Column("CHANGE_STAMP"), Nullable] public decimal? ChangeStamp { get; set; } // NUMBER (38,0)
			[Column("SITE"), Nullable] public string? SiteColumn { get; set; } // NVARCHAR2(18)
			[Column("DESCRIPTION"), Nullable] public string? Description { get; set; } // NVARCHAR2(120)
			[Column("TYPE"), Nullable] public string? Type { get; set; } // NVARCHAR2(3)
			[Column("IS_LOCAL"), Nullable] public string? IsLocal { get; set; } // NVARCHAR2(15)
			[Column("URL"), Nullable] public string? Url { get; set; } // NVARCHAR2(3072)
			[Column("SERVER"), Nullable] public string? Server { get; set; } // NVARCHAR2(96)
			[Column("PORT"), Nullable] public decimal? Port { get; set; } // NUMBER (38,0)
			[Column("LOGON_ID"), Nullable] public string? LogonId { get; set; } // NVARCHAR2(90)
			[Column("PASSWORD"), Nullable] public string? Password { get; set; } // NVARCHAR2(90)
			[Column("TIME_ZONE"), Nullable] public string? TimeZone { get; set; } // NVARCHAR2(108)
			[Column("SITE_LOCALE"), Nullable] public string? SiteLocale { get; set; } // NVARCHAR2(120)
			[Column("CREATED_DATE_TIME"), Nullable] public DateTime? CreatedDateTime { get; set; } // DATE
			[Column("MODIFIED_DATE_TIME"), Nullable] public DateTime? ModifiedDateTime { get; set; } // DATE
		}
		[Table(Name = "USR")]
		public partial class Usr
		{
			[Column("HANDLE"), NotNull] public string Handle { get; set; } = null!; // NVARCHAR2(1236)
			[Column("CHANGE_STAMP"), Nullable] public decimal? ChangeStamp { get; set; } // NUMBER (38,0)
			[Column("SITE"), Nullable] public string? Site { get; set; } // NVARCHAR2(18)
			[Column("USER_ID"), Nullable] public string? UserId { get; set; } // NVARCHAR2(90)
			[Column("CURRENT_OPERATION_BO"), Nullable] public string? CurrentOperationBo { get; set; } // NVARCHAR2(1236)
			[Column("CURRENT_RESOURCE_BO"), Nullable] public string? CurrentResourceBo { get; set; } // NVARCHAR2(1236)
			[Column("CREATED_DATE_TIME"), Nullable] public DateTime? CreatedDateTime { get; set; } // DATE
			[Column("MODIFIED_DATE_TIME"), Nullable] public DateTime? ModifiedDateTime { get; set; } // DATE
			[Column("BADGE_NUMBER"), Nullable] public string? BadgeNumber { get; set; } // NVARCHAR2(120)
			[Column("EMPLOYEE_NUMBER"), Nullable] public string? EmployeeNumber { get; set; } // NVARCHAR2(120)
			[Column("HIRE_DATE"), Nullable] public string? HireDate { get; set; } // NVARCHAR2(24)
			[Column("TERMINATION_DATE"), Nullable] public string? TerminationDate { get; set; } // NVARCHAR2(24)
			[Column("ALLOW_CLOCK_IN_NON_PROD"), Nullable] public string? AllowClockInNonProd { get; set; } // NVARCHAR2(15)
			[Column("ACTION_CLOCK_OUT_SFC"), Nullable] public string? ActionClockOutSfc { get; set; } // NVARCHAR2(3)
			[Column("CLOCK_IN_OUT_RANGE"), Nullable] public string? ClockInOutRange { get; set; } // NVARCHAR2(3)
			[Column("ALLOW_SUP_TIME_EDIT_APPR"), Nullable] public string? AllowSupTimeEditAppr { get; set; } // NVARCHAR2(15)
			[Column("APPR_REQ_FOR_EXPORT"), Nullable] public string? ApprReqForExport { get; set; } // NVARCHAR2(15)
			[Column("AUTO_CLOCK_OUT"), Nullable] public string? AutoClockOut { get; set; } // NVARCHAR2(15)
			[Column("CLOCK_IN_CONTROL"), Nullable] public string? ClockInControl { get; set; } // NVARCHAR2(3)
			[Column("DEFAULT_WORK_CENTER_BO"), Nullable] public string? DefaultWorkCenterBo { get; set; } // NVARCHAR2(1236)
			[Column("ERP_PERSONNEL_NUMBER"), Nullable] public string? ErpPersonnelNumber { get; set; } // NVARCHAR2(24)
			[Column("ERP_USER"), Nullable] public string? ErpUser { get; set; } // NVARCHAR2(36)
		}
		[Table(Name = "SHOP_ORDER")]
		public partial class ShopOrder
		{
			[Column("HANDLE"), NotNull] public string Handle { get; set; } = null!; // NVARCHAR2(1236)
			[Column("CHANGE_STAMP"), Nullable] public decimal? ChangeStamp { get; set; } // NUMBER (38,0)
			[Column("SITE"), Nullable] public string? Site { get; set; } // NVARCHAR2(18)
			[Column("SHOP_ORDER"), Nullable] public string? ShopOrderColumn { get; set; } // NVARCHAR2(108)
			[Column("STATUS_BO"), Nullable] public string? StatusBo { get; set; } // NVARCHAR2(1236)
			[Column("PRIORITY"), Nullable] public decimal? Priority { get; set; } // NUMBER (38,0)
			[Column("PLANNED_WORK_CENTER_BO"), Nullable] public string? PlannedWorkCenterBo { get; set; } // NVARCHAR2(1236)
			[Column("PLANNED_ITEM_BO"), Nullable] public string? PlannedItemBo { get; set; } // NVARCHAR2(1236)
			[Column("PLANNED_BOM_BO"), Nullable] public string? PlannedBomBo { get; set; } // NVARCHAR2(1236)
			[Column("PLANNED_ROUTER_BO"), Nullable] public string? PlannedRouterBo { get; set; } // NVARCHAR2(1236)
			[Column("ITEM_BO"), Nullable] public string? ItemBo { get; set; } // NVARCHAR2(1236)
			[Column("BOM_BO"), Nullable] public string? BomBo { get; set; } // NVARCHAR2(1236)
			[Column("ROUTER_BO"), Nullable] public string? RouterBo { get; set; } // NVARCHAR2(1236)
			[Column("QTY_TO_BUILD"), Nullable] public decimal? QtyToBuild { get; set; } // NUMBER (38,6)
			[Column("QTY_ORDERED"), Nullable] public decimal? QtyOrdered { get; set; } // NUMBER (38,6)
			[Column("QTY_RELEASED"), Nullable] public decimal? QtyReleased { get; set; } // NUMBER (38,6)
			[Column("RELEASED_DATE"), Nullable] public DateTime? ReleasedDate { get; set; } // DATE
			[Column("PLANNED_START_DATE"), Nullable] public DateTime? PlannedStartDate { get; set; } // DATE
			[Column("PLANNED_COMP_DATE"), Nullable] public DateTime? PlannedCompDate { get; set; } // DATE
			[Column("SCHEDULED_START_DATE"), Nullable] public DateTime? ScheduledStartDate { get; set; } // DATE
			[Column("SCHEDULED_COMP_DATE"), Nullable] public DateTime? ScheduledCompDate { get; set; } // DATE
			[Column("ACTUAL_START_DATE"), Nullable] public DateTime? ActualStartDate { get; set; } // DATE
			[Column("ACTUAL_COMP_DATE"), Nullable] public DateTime? ActualCompDate { get; set; } // DATE
			[Column("QTY_DONE"), Nullable] public decimal? QtyDone { get; set; } // NUMBER (38,6)
			[Column("QTY_SCRAPPED"), Nullable] public decimal? QtyScrapped { get; set; } // NUMBER (38,6)
			[Column("CREATED_DATE_TIME"), Nullable] public DateTime? CreatedDateTime { get; set; } // DATE
			[Column("MODIFIED_DATE_TIME"), Nullable] public DateTime? ModifiedDateTime { get; set; } // DATE
			[Column("CUSTOMER"), Nullable] public string? Customer { get; set; } // NVARCHAR2(120)
			[Column("CUSTOMER_ORDER"), Nullable] public string? CustomerOrder { get; set; } // NVARCHAR2(120)
			[Column("RMA_SFC_DATA_TYPE_BO"), Nullable] public string? RmaSfcDataTypeBo { get; set; } // NVARCHAR2(1236)
			[Column("RMA_SHOP_ORDER_DATA_TYPE_BO"), Nullable] public string? RmaShopOrderDataTypeBo { get; set; } // NVARCHAR2(1236)
			[Column("ORIGINAL_STATUS_BO"), Nullable] public string? OriginalStatusBo { get; set; } // NVARCHAR2(1236)
			[Column("TRANSFER_SITE"), Nullable] public string? TransferSite { get; set; } // NVARCHAR2(18)
			[Column("TRANSFER_TYPE"), Nullable] public string? TransferType { get; set; } // NVARCHAR2(3)
			[Column("LCC_BO"), Nullable] public string? LccBo { get; set; } // NVARCHAR2(1236)
			[Column("SHOP_ORDER_TYPE_BO"), Nullable] public string? ShopOrderTypeBo { get; set; } // NVARCHAR2(1236)
			[Column("HOLD_ID"), Nullable] public decimal? HoldId { get; set; } // NUMBER (38,0)
			[Column("END_UNIT_NUMBER"), Nullable] public string? EndUnitNumber { get; set; } // NVARCHAR2(108)
			[Column("REQ_SERIAL_CHANGE"), Nullable] public string? ReqSerialChange { get; set; } // NVARCHAR2(15)
			[Column("COLLECT_PARENT_SERIAL"), Nullable] public string? CollectParentSerial { get; set; } // NVARCHAR2(15)
			[Column("BATCH_NUMBER"), Nullable] public string? BatchNumber { get; set; } // NVARCHAR2(60)
			[Column("ERP_ORDER"), Nullable] public string? ErpOrder { get; set; } // NVARCHAR2(15)
			[Column("ERP_PRODUCTION_VERSION"), Nullable] public string? ErpProductionVersion { get; set; } // NVARCHAR2(12)
			[Column("ERP_UNIT_OF_MEASURE"), Nullable] public string? ErpUnitOfMeasure { get; set; } // NVARCHAR2(9)
			[Column("PARTITION_DATE"), NotNull] public DateTime PartitionDate { get; set; } // TIMESTAMP(6)
			[Column("INSPECTION_LOT"), Nullable] public string? InspectionLot { get; set; } // NVARCHAR2(60)
			[Column("INSPECTION_GROUP_SIZE"), Nullable] public decimal? InspectionGroupSize { get; set; } // NUMBER (38,0)
			[Column("ERP_PUTAWAY_STORLOC"), Nullable] public string? ErpPutawayStorloc { get; set; } // NVARCHAR2(12)
			[Column("WAREHOUSE_NUMBER"), Nullable] public string? WarehouseNumber { get; set; } // NVARCHAR2(9)
		}
		[Table(Name = "RESRCE")]
		public partial class Resrce
		{
			[Column("HANDLE"), NotNull] public string Handle { get; set; } = null!; // NVARCHAR2(1236)
			[Column("CHANGE_STAMP"), Nullable] public decimal? ChangeStamp { get; set; } // NUMBER (38,0)
			[Column("SITE"), Nullable] public string? Site { get; set; } // NVARCHAR2(18)
			[Column("RESRCE"), Nullable] public string? ResrceColumn { get; set; } // NVARCHAR2(108)
			[Column("DESCRIPTION"), Nullable] public string? Description { get; set; } // NVARCHAR2(120)
			[Column("STATUS_BO"), Nullable] public string? StatusBo { get; set; } // NVARCHAR2(1236)
			[Column("PROCESS_RESOURCE"), Nullable] public string? ProcessResource { get; set; } // NVARCHAR2(15)
			[Column("OPERATION_BO"), Nullable] public string? OperationBo { get; set; } // NVARCHAR2(1236)
			[Column("VALID_FROM"), Nullable] public DateTime? ValidFrom { get; set; } // DATE
			[Column("VALID_TO"), Nullable] public DateTime? ValidTo { get; set; } // DATE
			[Column("SETUP_STATE"), Nullable] public string? SetupState { get; set; } // NVARCHAR2(3)
			[Column("SETUP_DESCRIPTION"), Nullable] public string? SetupDescription { get; set; } // NVARCHAR2(120)
			[Column("CNC_MACHINE"), Nullable] public string? CncMachine { get; set; } // NVARCHAR2(1236)
			[Column("PENDING_STATUS_BO"), Nullable] public string? PendingStatusBo { get; set; } // NVARCHAR2(1236)
			[Column("PENDING_REASON_CODE_BO"), Nullable] public string? PendingReasonCodeBo { get; set; } // NVARCHAR2(1236)
			[Column("PENDING_COMMENTS"), Nullable] public string? PendingComments { get; set; } // NCLOB
			[Column("CREATED_DATE_TIME"), Nullable] public DateTime? CreatedDateTime { get; set; } // DATE
			[Column("MODIFIED_DATE_TIME"), Nullable] public DateTime? ModifiedDateTime { get; set; } // DATE
			[Column("ERP_PLANT_MAINT_ORDER"), Nullable] public string? ErpPlantMaintOrder { get; set; } // NVARCHAR2(36)
			[Column("ERP_EQUIPMENT_NUMBER"), Nullable] public string? ErpEquipmentNumber { get; set; } // NVARCHAR2(54)
			[Column("ERP_INTERNAL_ID"), Nullable] public decimal? ErpInternalId { get; set; } // NUMBER (38,0)
			[Column("ERP_CAPACITY_CATEGORY"), Nullable] public string? ErpCapacityCategory { get; set; } // NVARCHAR2(9)
		}
		[Table(Name = "WORK_CENTER")]
		public partial class WorkCenter
		{
			[Column("HANDLE"), NotNull] public string Handle { get; set; } = null!; // NVARCHAR2(1236)
			[Column("CHANGE_STAMP"), Nullable] public decimal? ChangeStamp { get; set; } // NUMBER (38,0)
			[Column("SITE"), Nullable] public string? Site { get; set; } // NVARCHAR2(18)
			[Column("WORK_CENTER"), Nullable] public string? WorkCenterColumn { get; set; } // NVARCHAR2(108)
			[Column("DESCRIPTION"), Nullable] public string? Description { get; set; } // NVARCHAR2(120)
			[Column("ROUTER_BO"), Nullable] public string? RouterBo { get; set; } // NVARCHAR2(1236)
			[Column("CAN_BE_RELEASED_TO"), Nullable] public string? CanBeReleasedTo { get; set; } // NVARCHAR2(15)
			[Column("WC_CATEGORY"), Nullable] public string? WcCategory { get; set; } // NVARCHAR2(60)
			[Column("STATUS_BO"), Nullable] public string? StatusBo { get; set; } // NVARCHAR2(1236)
			[Column("WC_TYPE"), Nullable] public string? WcType { get; set; } // NVARCHAR2(60)
			[Column("ASSIGNMENT_ENFORCEMENT"), Nullable] public string? AssignmentEnforcement { get; set; } // NVARCHAR2(60)
			[Column("CREATED_DATE_TIME"), Nullable] public DateTime? CreatedDateTime { get; set; } // DATE
			[Column("MODIFIED_DATE_TIME"), Nullable] public DateTime? ModifiedDateTime { get; set; } // DATE
			[Column("ERP_INTERNAL_ID"), Nullable] public decimal? ErpInternalId { get; set; } // NUMBER (38,0)
			[Column("IS_ERP_WORK_CENTER"), Nullable] public string? IsErpWorkCenter { get; set; } // NVARCHAR2(15)
			[Column("ERP_WORK_CENTER"), Nullable] public string? ErpWorkCenter { get; set; } // NVARCHAR2(24)
			[Column("ERP_CAPACITY_CATEGORY"), Nullable] public string? ErpCapacityCategory { get; set; } // NVARCHAR2(9)
			[Column("STANDARD_VALUE_KEY_BO"), Nullable] public string? StandardValueKeyBo { get; set; } // NVARCHAR2(1236)
		}
		[Table(Name = "WORK_CENTER_MEMBER")]
		public partial class WorkCenterMember
		{
			[Column("HANDLE"), NotNull] public string Handle { get; set; } = null!; // NVARCHAR2(1236)
			[Column("WORK_CENTER_BO"), Nullable] public string? WorkCenterBo { get; set; } // NVARCHAR2(1236)
			[Column("WORK_CENTER_OR_RESOURCE_GBO"), Nullable] public string? WorkCenterOrResourceGbo { get; set; } // NVARCHAR2(1236)
			[Column("PRIMARY_WORK_CENTER"), Nullable] public string? PrimaryWorkCenter { get; set; } // NVARCHAR2(15)
			[Column("SEQUENCE"), Nullable] public decimal? Sequence { get; set; } // NUMBER (38,0)
		}

		[Table(Name = "ITEM_GROUP")]
		public partial class ItemGroup
		{
			[Column("HANDLE"), NotNull] public string Handle { get; set; } = null!; // NVARCHAR2(1236)
			[Column("CHANGE_STAMP"), Nullable] public decimal? ChangeStamp { get; set; } // NUMBER (38,0)
			[Column("SITE"), Nullable] public string? Site { get; set; } // NVARCHAR2(18)
			[Column("ITEM_GROUP"), Nullable] public string? ItemGroupColumn { get; set; } // NVARCHAR2(96)
			[Column("DESCRIPTION"), Nullable] public string? Description { get; set; } // NVARCHAR2(120)
			[Column("ROUTER_BO"), Nullable] public string? RouterBo { get; set; } // NVARCHAR2(1236)
			[Column("BOM_BO"), Nullable] public string? BomBo { get; set; } // NVARCHAR2(1236)
			[Column("MASK_GROUP_BO"), Nullable] public string? MaskGroupBo { get; set; } // NVARCHAR2(1236)
			[Column("CREATED_DATE_TIME"), Nullable] public DateTime? CreatedDateTime { get; set; } // DATE
			[Column("MODIFIED_DATE_TIME"), Nullable] public DateTime? ModifiedDateTime { get; set; } // DATE
		}
		[Table(Name = "ITEM_GROUP_MEMBER")]
		public partial class ItemGroupMember
		{
			[Column("HANDLE"), NotNull] public string Handle { get; set; } = null!; // NVARCHAR2(1236)
			[Column("ITEM_GROUP_BO"), Nullable] public string? ItemGroupBo { get; set; } // NVARCHAR2(1236)
			[Column("ITEM_BO"), Nullable] public string? ItemBo { get; set; } // NVARCHAR2(1236)
		}
		[Table(Name = "ITEM")]
		public partial class Item
		{
			[Column("HANDLE"), NotNull] public string Handle { get; set; } = null!; // NVARCHAR2(1236)
			[Column("CHANGE_STAMP"), Nullable] public decimal? ChangeStamp { get; set; } // NUMBER (38,0)
			[Column("SITE"), Nullable] public string? Site { get; set; } // NVARCHAR2(18)
			[Column("ITEM"), Nullable] public string? ItemColumn { get; set; } // NVARCHAR2(384)
			[Column("DESCRIPTION"), Nullable] public string? Description { get; set; } // NVARCHAR2(120)
			[Column("STATUS_BO"), Nullable] public string? StatusBo { get; set; } // NVARCHAR2(1236)
			[Column("ITEM_TYPE"), Nullable] public string? ItemType { get; set; } // NVARCHAR2(3)
			[Column("EFF_START_SEQ"), Nullable] public decimal? EffStartSeq { get; set; } // NUMBER (38,0)
			[Column("EFF_END_SEQ"), Nullable] public decimal? EffEndSeq { get; set; } // NUMBER (38,0)
			[Column("LOT_SIZE"), Nullable] public decimal? LotSize { get; set; } // NUMBER (38,6)
			[Column("QUANTITY_RESTRICTION"), Nullable] public string? QuantityRestriction { get; set; } // NVARCHAR2(3)
			[Column("ROUTER_BO"), Nullable] public string? RouterBo { get; set; } // NVARCHAR2(1236)
			[Column("BOM_BO"), Nullable] public string? BomBo { get; set; } // NVARCHAR2(1236)
			[Column("COMPONENT_GROUP_BO"), Nullable] public string? ComponentGroupBo { get; set; } // NVARCHAR2(1236)
			[Column("ITEM_GROUP_BO"), Nullable] public string? ItemGroupBo { get; set; } // NVARCHAR2(1236)
			[Column("LAST_RELEASED_DATE"), Nullable] public DateTime? LastReleasedDate { get; set; } // DATE
			[Column("ASSY_DATA_TYPE_BO"), Nullable] public string? AssyDataTypeBo { get; set; } // NVARCHAR2(1236)
			[Column("PRE_ASSEMBLED"), Nullable] public string? PreAssembled { get; set; } // NVARCHAR2(15)
			[Column("REVISION"), Nullable] public string? Revision { get; set; } // NVARCHAR2(60)
			[Column("CURRENT_REVISION"), Nullable] public string? CurrentRevision { get; set; } // NVARCHAR2(15)
			[Column("EFF_START_DATE"), Nullable] public DateTime? EffStartDate { get; set; } // DATE
			[Column("EFF_END_DATE"), Nullable] public DateTime? EffEndDate { get; set; } // DATE
			[Column("SELECTOR_ACTIVITY_BO"), Nullable] public string? SelectorActivityBo { get; set; } // NVARCHAR2(1236)
			[Column("SELECTOR_NOTE"), Nullable] public string? SelectorNote { get; set; } // NVARCHAR2(120)
			[Column("ASSIGN_SERIAL_AT_RELEASE"), Nullable] public string? AssignSerialAtRelease { get; set; } // NVARCHAR2(15)
			[Column("CREATED_DATE_TIME"), Nullable] public DateTime? CreatedDateTime { get; set; } // DATE
			[Column("MODIFIED_DATE_TIME"), Nullable] public DateTime? ModifiedDateTime { get; set; } // DATE
			[Column("DRAWING_NAME"), Nullable] public string? DrawingName { get; set; } // NVARCHAR2(768)
			[Column("MAXIMUM_USAGE"), Nullable] public decimal? MaximumUsage { get; set; } // NUMBER (38,0)
			[Column("USE_COMP_FROM_DRAWING"), Nullable] public string? UseCompFromDrawing { get; set; } // NVARCHAR2(15)
			[Column("PANEL"), Nullable] public string? Panel { get; set; } // NVARCHAR2(15)
			[Column("REMOVAL_ASSY_DATA_TYPE_BO"), Nullable] public string? RemovalAssyDataTypeBo { get; set; } // NVARCHAR2(1236)
			[Column("INV_ASSY_DATA_TYPE_BO"), Nullable] public string? InvAssyDataTypeBo { get; set; } // NVARCHAR2(1236)
			[Column("ORIGINAL_STATUS_BO"), Nullable] public string? OriginalStatusBo { get; set; } // NVARCHAR2(1236)
			[Column("QTY_MULTIPLIER"), Nullable] public decimal? QtyMultiplier { get; set; } // NUMBER (38,6)
			[Column("CREATE_TRACKABLE_SFC"), Nullable] public string? CreateTrackableSfc { get; set; } // NVARCHAR2(3)
			[Column("MASK_GROUP_BO"), Nullable] public string? MaskGroupBo { get; set; } // NVARCHAR2(1236)
			[Column("TRANSFER_ITEM_GROUP_BO"), Nullable] public string? TransferItemGroupBo { get; set; } // NVARCHAR2(1236)
			[Column("UNIT_OF_MEASURE"), Nullable] public string? UnitOfMeasure { get; set; } // NVARCHAR2(120)
			[Column("HOLD_ID"), Nullable] public decimal? HoldId { get; set; } // NUMBER (38,0)
			[Column("COLLECT_PARENT_SERIAL"), Nullable] public string? CollectParentSerial { get; set; } // NVARCHAR2(15)
			[Column("REQ_SERIAL_CHANGE"), Nullable] public string? ReqSerialChange { get; set; } // NVARCHAR2(15)
			[Column("IS_COLLECTOR"), Nullable] public string? IsCollector { get; set; } // NVARCHAR2(15)
			[Column("INC_BATCH_NUMBER"), Nullable] public string? IncBatchNumber { get; set; } // NVARCHAR2(3)
			[Column("TIME_SENSITIVE"), Nullable] public string? TimeSensitive { get; set; } // NVARCHAR2(15)
			[Column("MAX_SHELF_LIFE"), Nullable] public decimal? MaxShelfLife { get; set; } // NUMBER (38,6)
			[Column("MAX_SHELF_LIFE_UNITS"), Nullable] public string? MaxShelfLifeUnits { get; set; } // NVARCHAR2(6)
			[Column("MAX_FLOOR_LIFE"), Nullable] public decimal? MaxFloorLife { get; set; } // NUMBER (38,6)
			[Column("MAX_FLOOR_LIFE_UNITS"), Nullable] public string? MaxFloorLifeUnits { get; set; } // NVARCHAR2(6)
			[Column("NOTES"), Nullable] public string? Notes { get; set; } // NVARCHAR2(4000)
			[Column("TB_COMP_TYPE"), Nullable] public string? TbCompType { get; set; } // NVARCHAR2(3)
			[Column("CONSUMPTION_TOL"), Nullable] public decimal? ConsumptionTol { get; set; } // NUMBER (38,6)
			[Column("ERP_BACKFLUSHING"), Nullable] public string? ErpBackflushing { get; set; } // NVARCHAR2(15)
			[Column("STORAGE_LOCATION_BO"), Nullable] public string? StorageLocationBo { get; set; } // NVARCHAR2(1236)
			[Column("ERP_PUTAWAY_STORLOC"), Nullable] public string? ErpPutawayStorloc { get; set; } // NVARCHAR2(12)
			[Column("USE_ORDER_ID_REL1"), Nullable] public string? UseOrderIdREL1 { get; set; } // NVARCHAR2(15)
			[Column("ERP_GTIN"), Nullable] public string? ErpGtin { get; set; } // NVARCHAR2(54)
			[Column("AIN_MODEL_EXTERNAL_ID"), Nullable] public string? AinModelExternalId { get; set; } // NVARCHAR2(120)
		}

		public class AllowedNcCodeOutput
		{
			internal string? NcCodeBo { get; set; }
			internal string? NcCode { get; set; }
			internal string? NcCodeDescription { get; set; }
		}

		public class ProductionFailedNcDataOutput
		{
			public AllowedNcCodeOutput? NcCode { get; set; }
			public string? UserBo { get; set; }
			public string? Operation { get; set; }
			public string? OperationBo { get; set; }
			public string? OperationDescription { get; set; }
			public string? CurrentOperationBo { get; set; }
			public string? SfcBo { get; set; }
			public string? Sfc { get; set; }
			public string? ShopOrderBo { get; set; }
			public string? ItemBo { get; set; }
			public string? NcDataBo { get; set; }
			public string? ResourceBo { get; set; }
			public string? WorkCenterBo { get; set; }
			public DateTime PartitionDate { get; set; }
			public string? Site { get; set; }
		}

		public class FilterByTestOperationOutput
		{
			public ProductionFailedNcDataOutput? Parent { get; set; }
			public string? SfcStepBo { get; set; }
			public string? RouterOperationBo { get; set; }
		}

		public class GetAdditionalDataOutput
		{
			public string? SiteDescription { get; set; }
			public FilterByTestOperationOutput? Parent { get; set; }
			public string? ShopOrder { get; set; }
			public string? Resrce { get; set; }
			public string? ResrceDescription { get; set; }
			public string? Workcenter { get; set; }
			public string? WorkcenterDescription { get; set; }
			public string? Line { get; set; }
			public string? LineDescription { get; set; }
			public string? Item { get; set; }
			public string? ItemDescription { get; set; }
			public string? ProductLine { get; set; }
			public string? ProductGroup { get; set; }
			public string? ItemGroup { get; set; }
			public string? ItemGroupDescription { get; set; }
			public string? TestCategory { get; set; }
			public string? UserId { get; set; }
			public string? BadgeNumber { get; set; }
		}

		private static IQueryable<AllowedNcCodeOutput> GetAllowedNcCode(WipDB wipDB)
		{
			return (from ncCode in wipDB.NcCode
					join ncGroupMember in wipDB.NcGroupMember
						on ncCode.Handle equals ncGroupMember.NcCodeOrGroupGbo
					where
						ncGroupMember.NcGroupBo == "NCGroupBO:" + ncCode.Site + ",CATAN_AUTO" ||
						ncGroupMember.NcGroupBo == "NCGroupBO:" + ncCode.Site + ",CATAN_MAN" ||
						ncGroupMember.NcGroupBo == "NCGroupBO:" + ncCode.Site + ",CATAN_ALL"
					select new AllowedNcCodeOutput
					{
						NcCodeBo = ncCode.Handle,
						NcCode = ncCode.NcCodeColumn,
						NcCodeDescription = ncCode.Description
					}).Distinct().AsCte(nameof(GetAllowedNcCode));
		}

		private static IQueryable<ProductionFailedNcDataOutput> FindProductionFailedNcData(WipDB wipDB,
			IQueryable<AllowedNcCodeOutput> ncCode)
		{
			return (from ncData in wipDB.NcData
					from ncCodeItem in ncCode.InnerJoin(ncCodeItem => ncCodeItem.NcCodeBo == ncData.NcCodeBo)
					join sfc in wipDB.Sfc
						on ncData.NcContextGbo equals sfc.Handle
					join operationItem in wipDB.Operation
						on ncData.OperationBo equals operationItem.Handle
					join customFields in wipDB.CustomFields
						on new { Handle = sfc.ShopOrderBo, Attribute = "ORDER_TYPE", Value = "ZPRN" } equals new
						{ customFields.Handle, customFields.Attribute, customFields.Value }
					select new ProductionFailedNcDataOutput
					{
						NcCode = ncCodeItem,
						Operation = operationItem.OperationColumn,
						OperationBo = ncData.OperationBo,
						UserBo = ncData.UserBo,
						OperationDescription = operationItem.Description,
						CurrentOperationBo = "OperationBO:" + ncData.Site + "," + operationItem.OperationColumn + ",#",
						SfcBo = sfc.Handle,
						Sfc = sfc.SfcColumn,
						ShopOrderBo = sfc.ShopOrderBo,
						ItemBo = sfc.ItemBo,
						NcDataBo = ncData.Handle,
						ResourceBo = ncData.ResourceBo,
						WorkCenterBo = ncData.WorkCenterBo,
						PartitionDate = ncData.PartitionDate,
						Site = ncData.Site
					}).AsCte(nameof(FindProductionFailedNcData));
		}

		private static IQueryable<FilterByTestOperationOutput> FilterByTestOperation(WipDB wipDB,
			IQueryable<ProductionFailedNcDataOutput> input)
		{
			return (from inputItem in input
					join sfcRouting in wipDB.SfcRouting
						on inputItem.SfcBo equals sfcRouting.SfcBo
					join sfcRouter in wipDB.SfcRouter
						on sfcRouting.Handle equals sfcRouter.SfcRoutingBo
					join sfcStep in wipDB.SfcStep
						on sfcRouter.Handle equals sfcStep.SfcRouterBo
					join routerStep in wipDB.RouterStep
						on new { sfcRouter.RouterBo, sfcStep.StepId } equals new { routerStep.RouterBo, routerStep.StepId }
					join routerOperation in wipDB.RouterOperation
						on new { RouterStepBo = routerStep.Handle, OperationBo = inputItem.CurrentOperationBo } equals new
						{ routerOperation.RouterStepBo, routerOperation.OperationBo }
					join customFields in wipDB.CustomFields
						on new { routerOperation.Handle, Attribute = "OPERATION_TYPE", Value = "T" } equals new
						{ customFields.Handle, customFields.Attribute, customFields.Value }
					join router in wipDB.Router
						on sfcRouter.RouterBo equals router.Handle
					where (sfcRouter.Completed == "false" && sfcRouter.InUse == "true") ||
						  (sfcRouter.Completed == "true" && router.RouterType == "U")
					select new FilterByTestOperationOutput
					{
						Parent = inputItem,
						SfcStepBo = sfcStep.Handle,
						RouterOperationBo = routerOperation.Handle
					}).AsCte(nameof(FilterByTestOperation));
		}

		private static IQueryable<GetAdditionalDataOutput> GetAdditionalData(WipDB wipDB,
			IQueryable<FilterByTestOperationOutput> input)
		{
			return (from inputItem in input
					from site in wipDB.Site.LeftJoin(site => site.SiteColumn == inputItem.Parent!.Site)
					from usr in wipDB.Usr.LeftJoin(usr => usr.Handle == inputItem.Parent!.UserBo)
					from shopOrder in wipDB.ShopOrder.LeftJoin(
						shopOrder => shopOrder.Handle == inputItem.Parent!.ShopOrderBo)
					from resrce in wipDB.Resrce.LeftJoin(resrce => resrce.Handle == inputItem.Parent!.ResourceBo)
					from workCenter in wipDB.WorkCenter.LeftJoin(workCenter =>
						workCenter.Handle == inputItem.Parent!.WorkCenterBo)
					from workCenterMember in wipDB.WorkCenterMember.LeftJoin(workCenterMember =>
						workCenterMember.WorkCenterOrResourceGbo == inputItem.Parent!.WorkCenterBo)
					from line in wipDB.WorkCenter.LeftJoin(line => line.Handle == workCenterMember.WorkCenterBo)
					from item in wipDB.Item.LeftJoin(item => item.Handle == inputItem.Parent!.ItemBo)
					from itemGroupMember in wipDB.ItemGroupMember.LeftJoin(itemGroupMember =>
						itemGroupMember.ItemBo == inputItem.Parent!.ItemBo)
					from itemGroup in wipDB.ItemGroup.LeftJoin(itemGroup => itemGroup.Handle == itemGroupMember.ItemGroupBo)
					from customField in wipDB.CustomFields.LeftJoin(customField =>
						customField.Attribute == "PRODUCT_LINE" && customField.Handle == inputItem.Parent!.ItemBo)
					from customField2 in wipDB.CustomFields.LeftJoin(customField =>
						customField.Attribute == "SPART" && customField.Handle == inputItem.Parent!.ItemBo)
					from customField3 in wipDB.CustomFields.LeftJoin(customField =>
						customField.Attribute == "TEST_CATEGORY" && customField.Handle == inputItem.RouterOperationBo)
					select new GetAdditionalDataOutput
					{
						SiteDescription = site.Description,
						Parent = inputItem,
						ShopOrder = shopOrder.ShopOrderColumn,
						Resrce = resrce.ResrceColumn,
						ResrceDescription = resrce.Description,
						Workcenter = workCenter.WorkCenterColumn,
						WorkcenterDescription = workCenter.Description,
						Line = line.WorkCenterColumn,
						LineDescription = line.Description,
						Item = item.ItemColumn,
						ItemDescription = item.Description,
						ProductLine = customField.Value,
						ProductGroup = customField2.Value,
						ItemGroup = itemGroup.ItemGroupColumn,
						ItemGroupDescription = itemGroup.Description,
						TestCategory = customField3.Value,
						UserId = usr.UserId,
						BadgeNumber = usr.BadgeNumber
					}).AsCte(nameof(GetAdditionalData));
		}

		public class WipDB : DataConnection
		{
			public WipDB(string configuration)
				: base(configuration)
			{
			}

			public ITable<WipNcCode> NcCode => this.GetTable<WipNcCode>();
			public ITable<WipNcGroupMember> NcGroupMember => this.GetTable<WipNcGroupMember>();
			public ITable<SfcRouting> SfcRouting => this.GetTable<SfcRouting>();
			public ITable<SfcRouter> SfcRouter => this.GetTable<SfcRouter>();
			public ITable<RouterStep> RouterStep => this.GetTable<RouterStep>();
			public ITable<RouterOperation> RouterOperation => this.GetTable<RouterOperation>();
			public ITable<CustomFields> CustomFields => this.GetTable<CustomFields>();
			public ITable<Router> Router => this.GetTable<Router>();
			public ITable<SfcStep> SfcStep => this.GetTable<SfcStep>();
			public ITable<Operation> Operation => this.GetTable<Operation>();
			public ITable<Sfc> Sfc => this.GetTable<Sfc>();
			public ITable<NcData> NcData => this.GetTable<NcData>();
			public ITable<Site> Site => this.GetTable<Site>();
			public ITable<Usr> Usr => this.GetTable<Usr>();
			public ITable<ShopOrder> ShopOrder => this.GetTable<ShopOrder>();
			public ITable<Resrce> Resrce => this.GetTable<Resrce>();
			public ITable<WorkCenter> WorkCenter => this.GetTable<WorkCenter>();
			public ITable<WorkCenterMember> WorkCenterMember => this.GetTable<WorkCenterMember>();
			public ITable<Item> Item => this.GetTable<Item>();
			public ITable<ItemGroupMember> ItemGroupMember => this.GetTable<ItemGroupMember>();
			public ITable<ItemGroup> ItemGroup => this.GetTable<ItemGroup>();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5382")]
		public async Task DeleteFromCteWithWindowFunction([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			await using var db = GetDataContext(context);

			// Insert test data - multiple rows with same IntColumn to test ROW_NUMBER partitioning
			await using var tmp = db.CreateLocalTable(
			[
				new CteWindowSample { IntColumn = 1, DateColumn = new DateTime(2024, 1, 1), Value = 100 },
				new CteWindowSample { IntColumn = 1, DateColumn = new DateTime(2024, 1, 2), Value = 100 }, // Duplicate value - should be deleted
				new CteWindowSample { IntColumn = 1, DateColumn = new DateTime(2024, 1, 3), Value = 150 },
				new CteWindowSample { IntColumn = 2, DateColumn = new DateTime(2024, 1, 1), Value = 200 },
				new CteWindowSample { IntColumn = 2, DateColumn = new DateTime(2024, 1, 2), Value = 200 }, // Duplicate value - should be deleted
				new CteWindowSample { IntColumn = 2, DateColumn = new DateTime(2024, 1, 3), Value = 250 }
			]);

			// Create CTE with ROW_NUMBER window function
			var cte = tmp
				.Select(sample => new
				{
					Sample = sample,
					RowNum = Sql.Ext.RowNumber().Over().PartitionBy(sample.IntColumn).OrderBy(sample.DateColumn).ToValue()
				})
				.AsCte();

			// Delete rows where consecutive values are the same
			var deleted = await cte
				.InnerJoin(cte,
					(next, prev) => prev.Sample.IntColumn == next.Sample.IntColumn &&
					                prev.RowNum == next.RowNum - 1,
					(next, prev) => new { Previous = prev, Next = next })
				.Where(query => query.Previous.Sample.Value == query.Next.Sample.Value)
				.Select(query => query.Next)
				.DeleteAsync();

			deleted.ShouldBe(2);

			// Verify remaining rows
			var remaining = await tmp.OrderBy(x => x.IntColumn).ThenBy(x => x.DateColumn).ToListAsync();
			remaining.Count.ShouldBe(4);
			
			// Should have kept first occurrence and rows with different values
			remaining[0].IntColumn.ShouldBe(1);
			remaining[0].DateColumn.ShouldBe(new DateTime(2024, 1, 1));
			remaining[0].Value.ShouldBe(100);
			
			remaining[1].IntColumn.ShouldBe(1);
			remaining[1].DateColumn.ShouldBe(new DateTime(2024, 1, 3));
			remaining[1].Value.ShouldBe(150);
			
			remaining[2].IntColumn.ShouldBe(2);
			remaining[2].DateColumn.ShouldBe(new DateTime(2024, 1, 1));
			remaining[2].Value.ShouldBe(200);
			
			remaining[3].IntColumn.ShouldBe(2);
			remaining[3].DateColumn.ShouldBe(new DateTime(2024, 1, 3));
			remaining[3].Value.ShouldBe(250);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5382")]
		public async Task UpdateViaCteWithWindowFunction([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			await using var db = GetDataContext(context);

			// Test data with values to update
			await using var tmp = db.CreateLocalTable(
			[
				new CteWindowSample { IntColumn = 1, DateColumn = new DateTime(2024, 1, 1), Value = 100 },
				new CteWindowSample { IntColumn = 1, DateColumn = new DateTime(2024, 1, 2), Value = 200 },
				new CteWindowSample { IntColumn = 2, DateColumn = new DateTime(2024, 1, 1), Value = 300 },
				new CteWindowSample { IntColumn = 2, DateColumn = new DateTime(2024, 1, 2), Value = 400 }
			]);

			// Create CTE with ROW_NUMBER window function - get first row in each partition
			var cte = tmp
				.Select(sample => new
				{
					Sample = sample,
					RowNum = Sql.Ext.RowNumber().Over().PartitionBy(sample.IntColumn).OrderBy(sample.DateColumn).ToValue()
				})
				.Where(x => x.RowNum == 1)
				.AsCte();

			// Update rows by joining with CTE - multiply value by 10 for first rows in each partition
			var updated = await tmp
				.InnerJoin(cte,
					(target, source) => target.IntColumn == source.Sample.IntColumn && 
					                    target.DateColumn == source.Sample.DateColumn,
					(target, source) => target)
				.UpdateAsync(t => new CteWindowSample { Value = t.Value * 10 });

			updated.ShouldBe(2);

			// Verify updated rows
			var result = await tmp.OrderBy(x => x.IntColumn).ThenBy(x => x.DateColumn).ToListAsync();
			result.Count.ShouldBe(4);

			// First rows should be updated (multiplied by 10)
			result[0].IntColumn.ShouldBe(1);
			result[0].DateColumn.ShouldBe(new DateTime(2024, 1, 1));
			result[0].Value.ShouldBe(1000);

			result[1].IntColumn.ShouldBe(1);
			result[1].DateColumn.ShouldBe(new DateTime(2024, 1, 2));
			result[1].Value.ShouldBe(200);

			result[2].IntColumn.ShouldBe(2);
			result[2].DateColumn.ShouldBe(new DateTime(2024, 1, 1));
			result[2].Value.ShouldBe(3000);

			result[3].IntColumn.ShouldBe(2);
			result[3].DateColumn.ShouldBe(new DateTime(2024, 1, 2));
			result[3].Value.ShouldBe(400);
		}

		[Table]
		public sealed class CteWindowSample
		{
			[Column] public int IntColumn { get; set; }
			[Column] public DateTime DateColumn { get; set; }
			[Column] public decimal Value { get; set; }
		}

		// https://github.com/linq2db/linq2db/issues/5683 - recursive CTE drops columns when the recursive term projects a differing-but-assignable type
		[Test(Description = "https://github.com/linq2db/linq2db/issues/5683")]
		public void RecursiveCteWithDerivedRecursiveTerm([RecursiveCteContextSource] string context)
		{
			using var db    = GetDataContext(context);
			using var parts = db.CreateLocalTable(new[]
			{
				new DerivedTermPart { Id = 1, Name = "A" },
				new DerivedTermPart { Id = 2, Name = "B" },
				new DerivedTermPart { Id = 3, Name = "C" },
			});
			using var refs = db.CreateLocalTable(new[]
			{
				new DerivedTermReference { Id = 1, ParentId = 1, ReferenceId = 2 },
				new DerivedTermReference { Id = 2, ParentId = 2, ReferenceId = 3 },
			});

			// Anchor term paginated with OrderBy/Skip/Take (wraps it in a subquery).
			var anchor = parts
				.Select(x => new CteHierarchyRow
				{
					RootPartSortField = x.Name,
					RootPartId        = x.Id,
					HierarchyLevel    = 0,
					PartId            = x.Id
				})
				.OrderBy(x => x.RootPartSortField)
				.Skip(0)
				.Take(20);

			var partCte = db.GetCte<CteHierarchyRow>(partHierarchy =>
				anchor.Concat(
					partHierarchy.InnerJoin(
						refs,
						(cte, reference) => reference.ParentId == cte.PartId,
						// the recursive term projects a DERIVED type cast to the CTE type, so the merged
						// set-operation projection is `test ? CteHierarchyRow : Convert(DerivedCteHierarchyRow)`
						(cte, reference) => (CteHierarchyRow)new DerivedCteHierarchyRow
						{
							RootPartSortField = cte.RootPartSortField,
							RootPartId        = cte.RootPartId,
							PartId            = reference.ReferenceId,
							HierarchyLevel    = cte.HierarchyLevel + 1
						})));

			// Outer query joins on cte.PartId but only projects RootPartId/RootPartSortField.
			var allRelevant = parts
				.InnerJoin(
					partCte,
					(me, cte) => me.Id == cte.PartId,
					(me, id) => new { id.RootPartId, id.RootPartSortField, id.HierarchyLevel, me });

			var result = allRelevant.ToList();

			// CTE columns PartId/RootPartId/RootPartSortField must survive the cast so the recursion and the
			// outer join resolve correctly, and each root DerivedTermPart's Name must round-trip.
			// Expected (RootPartId, joined DerivedTermPart.Id, RootPartSortField, HierarchyLevel) tuples for the hierarchy:
			//   roots: (1,1,"A",0) (2,2,"B",0) (3,3,"C",0); 1->2: (1,2,"A",1); 2->3: (2,3,"B",1); 1->2->3: (1,3,"A",2)
			result
				.Select(x => (x.RootPartId, x.me.Id, SortField: (string)x.RootPartSortField!, x.HierarchyLevel))
				.OrderBy(x => (x.RootPartId, x.Id))
				.ShouldBe(new[] { (1, 1, "A", 0), (1, 2, "A", 1), (1, 3, "A", 2), (2, 2, "B", 0), (2, 3, "B", 1), (3, 3, "C", 0) });
		}

		// https://github.com/linq2db/linq2db/issues/5683 - the issue's own shape, spelled without the cast
		[Test(Description = "https://github.com/linq2db/linq2db/issues/5683")]
		public void RecursiveCteWithDerivedRecursiveTermWithoutCast([RecursiveCteContextSource] string context)
		{
			using var db    = GetDataContext(context);
			using var parts = db.CreateLocalTable(new[]
			{
				new DerivedTermPart { Id = 1, Name = "A" },
				new DerivedTermPart { Id = 2, Name = "B" },
				new DerivedTermPart { Id = 3, Name = "C" },
			});
			using var refs = db.CreateLocalTable(new[]
			{
				new DerivedTermReference { Id = 1, ParentId = 1, ReferenceId = 2 },
				new DerivedTermReference { Id = 2, ParentId = 2, ReferenceId = 3 },
			});

			var anchor = parts
				.Select(x => new CteHierarchyRow
				{
					RootPartSortField = x.Name,
					RootPartId        = x.Id,
					HierarchyLevel    = 0,
					PartId            = x.Id
				})
				.OrderBy(x => x.RootPartSortField)
				.Skip(0)
				.Take(20);

			var partCte = db.GetCte<CteHierarchyRow>(partHierarchy =>
				anchor.Concat(
					partHierarchy.InnerJoin(
						refs,
						(cte, reference) => reference.ParentId == cte.PartId,
						// no `(CteHierarchyRow)` cast - covariance alone makes the lambda return IQueryable<CteHierarchyRow>,
						// so nothing in the tree says the branch is read as the CTE's element type
						(cte, reference) => new DerivedCteHierarchyRow
						{
							RootPartSortField = cte.RootPartSortField,
							RootPartId        = cte.RootPartId,
							PartId            = reference.ReferenceId,
							HierarchyLevel    = cte.HierarchyLevel + 1
						})));

			var result = parts
				.InnerJoin(partCte, (me, cte) => me.Id == cte.PartId, (me, id) => new { id.RootPartId, id.RootPartSortField, id.HierarchyLevel, me })
				.ToList();

			result
				.Select(x => (x.RootPartId, x.me.Id, SortField: (string)x.RootPartSortField!, x.HierarchyLevel))
				.OrderBy(x => (x.RootPartId, x.Id))
				.ToArray()
				.ShouldBe(new[] { (1, 1, "A", 0), (1, 2, "A", 1), (1, 3, "A", 2), (2, 2, "B", 0), (2, 3, "B", 1), (3, 3, "C", 0) });
		}

		// https://github.com/linq2db/linq2db/issues/5683 - the anchor projects a differing-but-assignable type
		[Test(Description = "https://github.com/linq2db/linq2db/issues/5683")]
		public void RecursiveCteWithDerivedAnchor([RecursiveCteContextSource] string context)
		{
			using var db    = GetDataContext(context);
			using var parts = db.CreateLocalTable(new[]
			{
				new DerivedTermPart { Id = 1, Name = "A" },
				new DerivedTermPart { Id = 2, Name = "B" },
				new DerivedTermPart { Id = 3, Name = "C" },
			});
			using var refs = db.CreateLocalTable(new[]
			{
				new DerivedTermReference { Id = 1, ParentId = 1, ReferenceId = 2 },
				new DerivedTermReference { Id = 2, ParentId = 2, ReferenceId = 3 },
			});

			var anchor = parts
				.Select(x => (CteHierarchyRow)new DerivedCteHierarchyRow
				{
					RootPartSortField = x.Name,
					RootPartId        = x.Id,
					HierarchyLevel    = 0,
					PartId            = x.Id
				});

			var partCte = db.GetCte<CteHierarchyRow>(partHierarchy =>
				anchor.Concat(
					partHierarchy.InnerJoin(
						refs,
						(cte, reference) => reference.ParentId == cte.PartId,
						(cte, reference) => new CteHierarchyRow
						{
							RootPartSortField = cte.RootPartSortField,
							RootPartId        = cte.RootPartId,
							PartId            = reference.ReferenceId,
							HierarchyLevel    = cte.HierarchyLevel + 1
						})));

			var result = parts
				.InnerJoin(partCte, (me, cte) => me.Id == cte.PartId, (me, id) => new { id.RootPartId, id.RootPartSortField, id.HierarchyLevel, me })
				.ToList();

			result
				.Select(x => (x.RootPartId, x.me.Id, SortField: (string)x.RootPartSortField!, x.HierarchyLevel))
				.OrderBy(x => (x.RootPartId, x.Id))
				.ToArray()
				.ShouldBe(new[] { (1, 1, "A", 0), (1, 2, "A", 1), (1, 3, "A", 2), (2, 2, "B", 0), (2, 3, "B", 1), (3, 3, "C", 0) });
		}

		sealed class DerivedTermPart
		{
			[PrimaryKey] public int     Id   { get; set; }
			public              string? Name { get; set; }
		}

		sealed class DerivedTermReference
		{
			[PrimaryKey] public int Id          { get; set; }
			public              int ParentId    { get; set; }
			public              int ReferenceId { get; set; }
		}

		class CteHierarchyRow
		{
			public object? RootPartSortField { get; set; }
			public int     RootPartId        { get; set; }
			public int     PartId            { get; set; }
			public int     HierarchyLevel    { get; set; }
		}

		sealed class DerivedCteHierarchyRow : CteHierarchyRow;

		[Test]
		public void RecursiveCteWithAssociationOuterJoinAndEagerLoad([IncludeDataSources(false, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);
			using var t1 = db.CreateLocalTable<CteSeedRow>();
			using var t2 = db.CreateLocalTable<CteSeedParent>();
			using var t3 = db.CreateLocalTable<CteRecursiveStep>();
			using var t4 = db.CreateLocalTable<CteOuterJoinedRow>();
			using var t5 = db.CreateLocalTable<CteProjectedValueRow>();

			var cte = db.GetCte<CteNodeRecord>(cte =>
			{
				return t1.Select(s => new CteNodeRecord(s.Table2.Id))
					.Concat(
						from c in cte
						join r3 in t3 on c.Id equals r3.Value2
						select new CteNodeRecord(r3.Value));
			});

			var query =
				from c in cte
				join r4 in t4 on c.Id equals r4.Id into records4
				from r3 in records4.DefaultIfEmpty()
				where r3.Value != null
				select new
				{
					Values = t5
						.Where(a => a.Value == c.Id)
						.ToArray()
				};

			query.ToArray();
		}

		public class CteSeedRow
		{
			[PrimaryKey]
			public int Id { get; set; }
			public int FK { get; set; }

			[Association(ThisKey = nameof(FK), OtherKey = nameof(CteSeedParent.Id), CanBeNull = false)]
			public CteSeedParent Table2 { get; } = null!;
		}

		public class CteSeedParent
		{
			[PrimaryKey]
			public int Id { get; set; }
		}

		public class CteRecursiveStep
		{
			[PrimaryKey]
			public int Id { get; set; }
			public int Value2 { get; set; }
			public int Value { get; set; }
		}

		public class CteOuterJoinedRow
		{
			[PrimaryKey]
			public int Id { get; set; }
			public int? Value { get; set; }
		}

		public class CteProjectedValueRow
		{
			[PrimaryKey]
			public int Id { get; set; }
			public int Value { get; set; }
		}

		private sealed record CteNodeRecord(int Id);

		#region Null test of an outer-joined CTE side

		[Table]
		sealed class CteNullTestRow
		{
			[PrimaryKey] public int Id    { get; set; }
			[Column]     public int Value { get; set; }
		}

		sealed class CteNullTestKey
		{
			public int Id { get; set; }
		}

		sealed class CteNullTestNullableKey
		{
			public int? Id { get; set; }
		}

		static CteNullTestRow[] CteNullTestSeed(int from, int to)
		{
			return Enumerable.Range(from, to - from + 1).Select(i => new CteNullTestRow { Id = i, Value = i * 10 }).ToArray();
		}

		[Test]
		public void FullJoinCteProjectionNullTest([CteContextSource(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);
			using var t1 = db.CreateLocalTable("CteNullTestA", CteNullTestSeed(1, 3));
			using var t2 = db.CreateLocalTable("CteNullTestB", CteNullTestSeed(2, 4));

			var a = t1.Select(e => new CteNullTestKey { Id = e.Id }).AsCte("a");
			var b = t2.Select(e => new CteNullTestKey { Id = e.Id }).AsCte("b");

			var result = a
				.FullJoin(b, (x, y) => x.Id == y.Id, (x, y) => new
				{
					Id   = x != null ? x.Id : y.Id,
					HasX = x != null,
					HasY = y != null,
				})
				.ToList()
				.OrderBy(r => r.Id)
				.ToList();

			result.Select(r => r.Id).ShouldBe([1, 2, 3, 4]);
			result.Select(r => r.HasX).ShouldBe([true, true, true, false]);
			result.Select(r => r.HasY).ShouldBe([false, true, true, true]);
		}

		[Test]
		public void FullJoinCteTableNullTest([CteContextSource(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);
			using var t1 = db.CreateLocalTable("CteNullTestA", CteNullTestSeed(1, 3));
			using var t2 = db.CreateLocalTable("CteNullTestB", CteNullTestSeed(2, 4));

			var result = t1.AsCte("a")
				.FullJoin(t2.AsCte("b"), (x, y) => x.Id == y.Id, (x, y) => new
				{
					Id    = x != null ? x.Id : y.Id,
					Value = (x != null ? x.Value : 0) + (y != null ? y.Value : 0),
				})
				.ToList()
				.OrderBy(r => r.Id)
				.ToList();

			result.Select(r => r.Id).ShouldBe([1, 2, 3, 4]);
			result.Select(r => r.Value).ShouldBe([10, 40, 60, 40]);
		}

		[Test]
		public void LeftJoinCteProjectionNullTest([CteContextSource] string context)
		{
			using var db = GetDataContext(context);
			using var t1 = db.CreateLocalTable("CteNullTestA", CteNullTestSeed(1, 4));
			using var t2 = db.CreateLocalTable("CteNullTestB", CteNullTestSeed(1, 2));

			var tier = t2.Select(e => new CteNullTestKey { Id = e.Id }).AsCte("tier");

			var result =
				(
					from o in t1
					from k in tier.LeftJoin(k => k.Id == o.Id)
					select new { o.Id, Missing = k == null ? 1 : 0 }
				)
				.ToList()
				.OrderBy(r => r.Id)
				.ToList();

			result.Select(r => r.Missing).ShouldBe([0, 0, 1, 1]);
		}

		[Test]
		public void LeftJoinCteNullableProjectionNullTest([CteContextSource] string context)
		{
			using var db = GetDataContext(context);
			using var t1 = db.CreateLocalTable("CteNullTestA", CteNullTestSeed(1, 4));
			using var t2 = db.CreateLocalTable("CteNullTestB", CteNullTestSeed(1, 2));

			var tier = t2.Select(e => new CteNullTestNullableKey { Id = e.Id }).AsCte("tier");

			var result =
				(
					from o in t1
					from k in tier.LeftJoin(k => k.Id == o.Id)
					select new { o.Id, Missing = k == null ? 1 : 0 }
				)
				.ToList()
				.OrderBy(r => r.Id)
				.ToList();

			result.Select(r => r.Missing).ShouldBe([0, 0, 1, 1]);
		}

		[Test]
		public void LeftJoinCteTableNullTest([CteContextSource] string context)
		{
			using var db = GetDataContext(context);
			using var t1 = db.CreateLocalTable("CteNullTestA", CteNullTestSeed(1, 4));
			using var t2 = db.CreateLocalTable("CteNullTestB", CteNullTestSeed(1, 2));

			var tier = t2.AsCte("tier");

			var result =
				(
					from o in t1
					from k in tier.LeftJoin(k => k.Id == o.Id)
					select new { o.Id, Missing = k == null ? 1 : 0, Value = k != null ? k.Value : -1 }
				)
				.ToList()
				.OrderBy(r => r.Id)
				.ToList();

			result.Select(r => r.Missing).ShouldBe([0, 0, 1, 1]);
			result.Select(r => r.Value).ShouldBe([10, 20, -1, -1]);
		}

		[Test]
		public void RightJoinCteProjectionNullTest([CteContextSource] string context)
		{
			using var db = GetDataContext(context);
			using var t1 = db.CreateLocalTable("CteNullTestA", CteNullTestSeed(1, 4));
			using var t2 = db.CreateLocalTable("CteNullTestB", CteNullTestSeed(1, 2));

			var tier = t2.Select(e => new CteNullTestKey { Id = e.Id }).AsCte("tier");

			var result = tier
				.RightJoin(t1, (k, o) => k.Id == o.Id, (k, o) => new { o.Id, Missing = k == null ? 1 : 0 })
				.ToList()
				.OrderBy(r => r.Id)
				.ToList();

			result.Select(r => r.Missing).ShouldBe([0, 0, 1, 1]);
		}

		// The raw SQL has no FROM clause, which Oracle, Firebird, DB2 and Informix reject, so the FromSql tests run
		// on SQLite, PostgreSQL and SQL Server only.
		const string CteNullTestFromSql = "SELECT 1 AS \"Id\" UNION ALL SELECT 2";

		[Test]
		public void LeftJoinCteFromSqlNullTest([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllPostgreSQL, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);
			using var t1 = db.CreateLocalTable("CteNullTestA", CteNullTestSeed(1, 4));

			var tier = db.FromSql<CteNullTestKey>(CteNullTestFromSql).AsCte("tier");

			var result =
				(
					from o in t1
					from k in tier.LeftJoin(k => k.Id == o.Id)
					select new { o.Id, Missing = k == null ? 1 : 0 }
				)
				.ToList()
				.OrderBy(r => r.Id)
				.ToList();

			result.Select(r => r.Missing).ShouldBe([0, 0, 1, 1]);
		}

		// A nullable key makes the null test use a marker column; this shape was not affected.
		[Test]
		public void LeftJoinCteFromSqlNullableKeyNullTest([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllPostgreSQL, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);
			using var t1 = db.CreateLocalTable("CteNullTestA", CteNullTestSeed(1, 4));

			var tier = db.FromSql<CteNullTestNullableKey>(CteNullTestFromSql).AsCte("tier");

			var result =
				(
					from o in t1
					from k in tier.LeftJoin(k => k.Id == o.Id)
					select new { o.Id, Missing = k == null ? 1 : 0 }
				)
				.ToList()
				.OrderBy(r => r.Id)
				.ToList();

			result.Select(r => r.Missing).ShouldBe([0, 0, 1, 1]);
		}

		[Test]
		public void LeftJoinCteAntiJoinTest([CteContextSource] string context)
		{
			using var db = GetDataContext(context);
			using var t1 = db.CreateLocalTable("CteNullTestA", CteNullTestSeed(1, 4));
			using var t2 = db.CreateLocalTable("CteNullTestB", CteNullTestSeed(1, 2));

			var tier = t2.Select(e => new CteNullTestKey { Id = e.Id }).AsCte("tier");

			var result =
				(
					from o in t1
					from k in tier.LeftJoin(k => k.Id == o.Id)
					where k == null
					select o.Id
				)
				.ToList()
				.OrderBy(id => id)
				.ToList();

			result.ShouldBe([3, 4]);
		}

		[Test]
		public void LeftJoinCteAntiJoinDeleteTest([CteContextSource(true, TestProvName.AllFirebird, ProviderName.DB2, TestProvName.AllMariaDB, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			using var t1 = db.CreateLocalTable("CteNullTestA", CteNullTestSeed(1, 4));
			using var t2 = db.CreateLocalTable("CteNullTestB", CteNullTestSeed(1, 2));

			var tier = t2.Select(e => new CteNullTestKey { Id = e.Id }).AsCte("tier");

			(
				from o in t1
				from k in tier.LeftJoin(k => k.Id == o.Id)
				where k == null
				select o
			)
			.Delete();

			t1.Select(o => o.Id).ToList().OrderBy(id => id).ShouldBe([1, 2]);
		}

		[Test]
		public void LeftJoinRecursiveCteNullTest([RecursiveCteContextSource] string context)
		{
			using var db = GetDataContext(context);
			using var t1 = db.CreateLocalTable("CteNullTestA", CteNullTestSeed(1, 4));

			var tier = db.GetCte<CteNullTestKey>(cte =>
				t1.Where(e => e.Id == 1).Select(e => new CteNullTestKey { Id = e.Id })
					.Concat(
						from c in cte
						where c.Id < 2
						select new CteNullTestKey { Id = c.Id + 1 }),
				"tier");

			var result =
				(
					from o in t1
					from k in tier.LeftJoin(k => k.Id == o.Id)
					select new { o.Id, Missing = k == null ? 1 : 0 }
				)
				.ToList()
				.OrderBy(r => r.Id)
				.ToList();

			result.Select(r => r.Missing).ShouldBe([0, 0, 1, 1]);
		}

		#endregion
	}
}
