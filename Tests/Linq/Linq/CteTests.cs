using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Humanizer;
using LinqToDB;
using LinqToDB.Expressions;

using NUnit.Framework;
using Tests.Tools;

namespace Tests.Linq
{
	using Model;

	public class CteTests : TestBase
	{
		public static string[] CteSupportedProviders = new[]
		{
			ProviderName.SqlServer2008, ProviderName.SqlServer2012, ProviderName.SqlServer2014,
			ProviderName.Firebird,
			ProviderName.PostgreSQL,
			ProviderName.DB2,
			ProviderName.SQLite, ProviderName.SQLiteClassic, ProviderName.SQLiteMS,
			ProviderName.Oracle, ProviderName.OracleManaged, ProviderName.OracleNative
			//ProviderName.Informix,
			// Will be supported in SQL 8.0 - ProviderName.MySql
		};

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
				: base(CteSupportedProviders.Except(excludedProviders).ToArray())
			{
			}

			public CteContextSourceAttribute(bool includeLinqService, params string[] excludedProviders)
				: base(includeLinqService, CteSupportedProviders.Except(excludedProviders).ToArray())
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
					from c4 in db.Child.Where(c4 => c4.ParentID % 2 == 0).AsCte("LAST").InnerJoin(c4 => c4.ParentID == c3.ParentID)
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
			var newExpr = source.Expression.Transform(e =>
			{
				if (e is MethodCallExpression methodCall && methodCall.Method.Name == "AsCte")
					return methodCall.Arguments[0];
				return e;
			});

			return source.Provider.CreateQuery<TSource>(newExpr);
		}

		[Test, NorthwindDataContext]
		public void ProductAndCategoryNamesOverTenDollars(string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var cteQuery =
					from p in db.Product
					where p.UnitPrice.Value > 10
					select new
					{
						p.ProductName,
						p.Category.CategoryName,
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

		[Test, NorthwindDataContext]
		public void ProductAndCategoryNamesOverTenDollars2(string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var cteQuery =
					from p in db.Product
					where p.UnitPrice.Value > 10
					select new
					{
						p.ProductName,
						p.Category.CategoryName,
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

		[Test, NorthwindDataContext]
		public void ProductsOverTenDollars(string context)
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
					where p.UnitPrice.Value > 10
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


		[Test, NorthwindDataContext]
		public void EmployeeSubordinatesReport(string context)
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

				var result =
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

				var expected =
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

				var expectedStr = expected.ToString();
				var resultdStr  = result.ToString();

				AreEqual(expected, result);
			}
		}

		class EmployeeHierarchyCTE
		{
			public int EmployeeID;
			public string LastName;
			public string FirstName;
			public int? ReportsTo;
			public int HierarchyLevel;
		}

		[Test, NorthwindDataContext(true)]
		public void EmployeeHierarchy(string context)
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

		private class CteDMLTests
		{
			protected bool Equals(CteDMLTests other)
			{
				return ChildID == other.ChildID && ParentID == other.ParentID;
			}

			public override bool Equals(object obj)
			{
				if (ReferenceEquals(null, obj)) return false;
				if (ReferenceEquals(this, obj)) return true;
				if (obj.GetType() != this.GetType()) return false;
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
			using (var testTable = db.CreateLocalTable<CteDMLTests>("CteChild"))
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

		[Test]
		public void TestDelete([CteContextSource(ProviderName.Firebird, ProviderName.DB2)] string context)
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

		[Test]
		public void TestUpdate(
			[CteContextSource(ProviderName.Firebird, ProviderName.DB2, ProviderName.Oracle, ProviderName.OracleManaged, ProviderName.OracleNative)]
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
			{
				using (var tree = db.CreateLocalTable(hierarchyData))
				{
					var hierarchy = GetHierarchyDown(tree, db);

					var result = hierarchy.OrderBy(h => h.Id);
					var expected = EnumerateDown(hierarchyData, 0, null).OrderBy(h => h.Id);

					AreEqual(expected, result, ComparerBuilder<HierarchyData>.GetEqualityComparer());
				}
			}
		}

		[Test]
		public void TestDoubleRecursion([CteContextSource(true, ProviderName.DB2)] string context)
		{
			var hierarchyData = GeHirarchyData();

			using (var db = GetDataContext(context))
			{
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
		}

		[Test]
		public void RecursiveCount([CteContextSource(true, ProviderName.DB2)] string context)
		{
			var hierarchyData = GeHirarchyData();

			using (var db = GetDataContext(context))
			{
				using (var tree = db.CreateLocalTable(hierarchyData))
				{
					var hierarchy = GetHierarchyDown(tree, db);
					var expected = EnumerateDown(hierarchyData, 0, null);

					Assert.AreEqual(expected.Count(), hierarchy.Count());
				}
			}
		}

		[Test]
		public void RecursiveInsertInto([CteContextSource(true, ProviderName.DB2)] string context)
		{
			var hierarchyData = GeHirarchyData();

			using (var db = GetDataContext(context))
			{
				using (var tree = db.CreateLocalTable(hierarchyData))
				using (var resultTable = db.CreateLocalTable<HierarchyData>())
				{
					var hierarchy = GetHierarchyDown(tree, db);
					hierarchy.Insert(resultTable, r => r);

					var result = resultTable.OrderBy(h => h.Id);
					var expected = EnumerateDown(hierarchyData, 0, null).OrderBy(h => h.Id);

					AreEqual(expected, result, ComparerBuilder<HierarchyData>.GetEqualityComparer());
				}
			}
		}

		[Test]
		public void RecursiveDeepNesting([CteContextSource(true, ProviderName.DB2)] string context)
		{
			using (var db = GetDataContext(context))
			{
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

					Assert.DoesNotThrow(() => Console.WriteLine(query.ToString()));
				}
			}
		}

		private class TestWrapper
		{
			public Child Child { get; set; }

			protected bool Equals(TestWrapper other)
			{
				return Equals(Child, other.Child);
			}

			public override bool Equals(object obj)
			{
				if (ReferenceEquals(null, obj)) return false;
				if (ReferenceEquals(this, obj)) return true;
				if (obj.GetType() != this.GetType()) return false;
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
			public Child Child   { get; set; }
			public Parent Parent { get; set; }

			protected bool Equals(TestWrapper2 other)
			{
				return Equals(Child, other.Child) && Equals(Parent, other.Parent);
			}

			public override bool Equals(object obj)
			{
				if (ReferenceEquals(null, obj)) return false;
				if (ReferenceEquals(this, obj)) return true;
				if (obj.GetType() != this.GetType()) return false;
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
					join c in cte1 on p.ParentID equals c.Child.ParentID
					select new {p, c};

				var result = query.ToArray();

				var expected =
					from p in db.Parent
					join c in cteQuery on p.ParentID equals c.Child.ParentID
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

	}
}
