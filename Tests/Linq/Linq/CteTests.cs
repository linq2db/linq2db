using System;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Expressions;

using NUnit.Framework;

using Tests.Model;

namespace Tests.Linq
{
	public class CteTests : TestBase
	{
		class CteContextSourceAttribute : IncludeDataContextSourceAttribute
		{
			public CteContextSourceAttribute() : this(true)
			{
			}

			public CteContextSourceAttribute(bool includeLinqService) : base(includeLinqService,
				ProviderName.SqlServer2008, ProviderName.SqlServer2012, ProviderName.SqlServer2014,
				ProviderName.Firebird,
				ProviderName.SQLite, ProviderName.SQLiteClassic, ProviderName.SQLiteMS,
				ProviderName.Oracle, ProviderName.OracleManaged, ProviderName.OracleNative
				//ProviderName.Informix,
				// Will be supported in SQL 8.0 - ProviderName.MySql
				)
			{
			}
		}

		[Test, CteContextSource]
		public void Test1(string context)
		{
			using (var db = GetDataContext(context))
			{
				var cte1 = db.GetTable<Child>().Where(c => c.ParentID > 1).AsCte();
				var query = from p in db.Parent
					join c in cte1 on p.ParentID equals c.ParentID
					join c2 in cte1 on p.ParentID equals c2.ParentID
					select p;

				var cte1_ = Child.Where(c => c.ParentID > 1).ToArray();

				var expected =
					from p in Parent
					join c in cte1_ on p.ParentID equals c.ParentID
					join c2 in cte1_ on p.ParentID equals c2.ParentID
					select p;

				AreEqual(expected, query);
			}
		}

		[Test, CteContextSource]
		public void Test2(string context)
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

				// Looks like we do not populate needed field for CTE. It is aproblem thta needs to be solved
				AreEqual(expected, result);
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

		[Test, NorthwindDataContext()]
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
				var employeeHierarchyCte = db.GetCte<EmployeeHierarchyCTE>(employeeHierarchy =>
				{
					return
						(
							from e in db.Employee
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
							from e in db.Employee
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


				// UNION ALL should be on top of query
				var data = result.ToArray();
			}
		}

		[Test, CteContextSource]
		public void Test4(string context)
		{
			using (var db = GetDataContext(context))
			{
				var cte1 = db.GetTable<Child>().Where(c => c.ParentID > 1).AsCte("CTE1_");
				var query = from p in cte1
					from c4 in db.Child.Where(c4 => c4.ParentID % 2 == 0).AsCte("LAST").InnerJoin(c4 => c4.ParentID == p.ParentID)
					select c4;

				var str = query.ToString();
			}
		}

		class RecursiveCTE
		{
			public int? ParentID;
			public int? ChildID;
			public int? GrandChildID;
		}

		[Test, IncludeDataContextSource(true, ProviderName.SqlServer2008, ProviderName.SqlServer2012, ProviderName.SqlServer2014)]
		public void RecursiveTest(string context)
		{
			using (var db = GetDataContext(context))
			{
				var cteRecursive = db.GetCte<RecursiveCTE>(cte =>
				(
					from gc1 in db.GrandChild
					select new RecursiveCTE
					{
						ChildID      = gc1.ChildID,
						GrandChildID = gc1.GrandChildID,
						ParentID     = gc1.GrandChildID
					}
				)
				.Concat
				(
					from gc in db.GrandChild
					//from p  in db.Parent.InnerJoin(p => p.ParentID == gc.ParentID)
					from ct in cte//.InnerJoin(ct => ct.ChildID == gc.ChildID)
					select new RecursiveCTE
					{
						ParentID     = ct.ParentID,
						ChildID      = ct.ChildID,
						GrandChildID = ct.ChildID + 1
					}
				)
				, "MY_CTE");

				var str = cteRecursive.ToString();
				// UNION otimized out
				Assert.That(str, Contains.Substring( "UNION" ) );
				var result = cteRecursive.ToArray();
			}
		}
	}
}
