using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Exporters;
using DataModels;
using LinqToDB.Expressions;

namespace Tests.Benchmark
{
//	[MemoryDiagnoser]
	public class QueryTests
	{
		private static Expression _bigExpression;

		[GlobalSetup]
		public static void Setup()
		{
			using (var db = new NorthwindDB())
			{
				_bigExpression = GenerateBigWhere(db.VwInvoices(), e => e.ProductID).Expression;
				Console.WriteLine("-------------------------------------------");
				Console.WriteLine("-------------------------------------------");
				Console.WriteLine("-------------------------------------------");
			}
		}

		static IQueryable<T> GenerateBigWhere<T>(IQueryable<T> query, Expression<Func<T, object>> prop)
		{
			var member = MemberHelper.MemberOf(prop);

			var array = Enumerable.Range(0, 1000).ToArray();

			// Build "where" conditions
			var param = Expression.Parameter(typeof(T));
			Expression<Func<T, bool>> predicate = null;

			for (int i = 0; i < array.Length; i++)
			{
				var id = array[i];

				var filterExpression = Expression.Lambda<Func<T, bool>>
				(Expression.Equal(
					Expression.Convert(Expression.MakeMemberAccess(param, member), typeof(int)),
					Expression.Constant(id)
				), param);

				predicate = predicate != null ? predicate.Or(filterExpression) : filterExpression;
			}

			var result = query.Where(predicate);
			return result;
		}

		static bool EmulateVisit<T>(IQueryable<T> queryable)
		{
			return EmulateVisit(queryable.Expression);
		}

		static bool EmulateVisit(Expression expr)
		{
			var parameters = new List<Expression>();
			int count = 0;
			expr.Visit(e =>
			{
				++count;
				var binary = expr as BinaryExpression;
				if (binary != null)
				{
					var left = binary.Left;
					var right = binary.Right;
					parameters.Add(left);
					parameters.Add(right);
				}
				else
				{
					if (e.NodeType == ExpressionType.Lambda)
						foreach (var p in ((LambdaExpression)e).Parameters)
							parameters.Add(p);
				}
			});

			return parameters.Count > 1;
		}

		static bool EmulateVisitNonRecursive<T>(IQueryable<T> queryable)
		{
			return EmulateVisitNonRecursive(queryable.Expression);
		}

		static bool EmulateVisitNonRecursive(Expression expr)
		{
			var parameters = new List<Expression>();

			int count = 0;
			VisitTest(expr, e =>
			{
				++count;
				var binary = expr as BinaryExpression;
				if (binary != null)
				{
					var left = binary.Left;
					var right = binary.Right;
					parameters.Add(left);
					parameters.Add(right);
				}
				else
				{
					if (e.NodeType == ExpressionType.Lambda)
						foreach (var p in ((LambdaExpression)e).Parameters)
							parameters.Add(p);
				}				
			});

			return parameters.Count > 1;
		}

		public static void VisitTest(Expression expr, Action<Expression> action)
		{
			foreach (var e in expr.EnumerateParentFirst1())
			{
				action(e);
			}
		}

		//[Benchmark]
		public static void VisitAllRecursive()
		{
			using (var db = new NorthwindDB())
			{
				EmulateVisit(db.VwAlphabeticalListOfProduct()       );
				EmulateVisit(db.VwCategorySalesByYear(1977)         );
				EmulateVisit(db.VwCurrentProductList()              );
				EmulateVisit(db.VwCustomerAndSuppliersByCity()      );
				EmulateVisit(db.VwCustomerAndSuppliersByCity()      );
				EmulateVisit(db.VwInvoices()					    );
				EmulateVisit(db.VwOrdersDetailsExtended()           );
				EmulateVisit(db.VwOrdersSubtotals()                 );
				EmulateVisit(db.VwOrdersQry()                       );
				EmulateVisit(db.VwProductSalesByYear(1977)          );
				EmulateVisit(db.VwProductsAboveAveragePrice()       );
				EmulateVisit(db.VwProductsByCategory()              );
				EmulateVisit(db.VwQuarterlyOrders(1977)             );
				EmulateVisit(db.VwSalesByCategory(1977)             );
				EmulateVisit(db.VwSalesTotalsByAmount(1977, 2000)   );
				EmulateVisit(db.VwGetSummaryOfSalesByQuarter()      );
				EmulateVisit(db.VwGetSummaryOfSalesByYear(1977)     );
			}
		}

		//[Benchmark]
		public static void VisitAllNonRecursive()
		{
			using (var db = new NorthwindDB())
			{
				EmulateVisitNonRecursive(db.VwAlphabeticalListOfProduct()       );
				EmulateVisitNonRecursive(db.VwCategorySalesByYear(1977)         );
				EmulateVisitNonRecursive(db.VwCurrentProductList()              );
				EmulateVisitNonRecursive(db.VwCustomerAndSuppliersByCity()      );
				EmulateVisitNonRecursive(db.VwCustomerAndSuppliersByCity()      );
				EmulateVisitNonRecursive(db.VwInvoices()					    );
				EmulateVisitNonRecursive(db.VwOrdersDetailsExtended()           );
				EmulateVisitNonRecursive(db.VwOrdersSubtotals()                 );
				EmulateVisitNonRecursive(db.VwOrdersQry()                       );
				EmulateVisitNonRecursive(db.VwProductSalesByYear(1977)          );
				EmulateVisitNonRecursive(db.VwProductsAboveAveragePrice()       );
				EmulateVisitNonRecursive(db.VwProductsByCategory()              );
				EmulateVisitNonRecursive(db.VwQuarterlyOrders(1977)             );
				EmulateVisitNonRecursive(db.VwSalesByCategory(1977)             );
				EmulateVisitNonRecursive(db.VwSalesTotalsByAmount(1977, 2000)   );
				EmulateVisitNonRecursive(db.VwGetSummaryOfSalesByQuarter()      );
				EmulateVisitNonRecursive(db.VwGetSummaryOfSalesByYear(1977)     );
			}
		}

		[Benchmark]
		public static void VisitAllBigPredicateNonRecursive()
		{
			EmulateVisitNonRecursive(_bigExpression);
		}

		[Benchmark(OperationsPerInvoke = 1000)]
		public static void VisitAllBigPredicateRecursive()
		{
			EmulateVisit(_bigExpression);
		}

		//[Benchmark]
		public static void QueryGenerator()
		{
			using (var db = new NorthwindDB())
			{
				db.VwAlphabeticalListOfProduct()    .ToString();
				db.VwCategorySalesByYear(1977)      .ToString();
				db.VwCurrentProductList()           .ToString();
				db.VwCustomerAndSuppliersByCity()   .ToString();
				db.VwCustomerAndSuppliersByCity()   .ToString();
				db.VwInvoices()					    .ToString();
				db.VwOrdersDetailsExtended()        .ToString();
				db.VwOrdersSubtotals()              .ToString();
				db.VwOrdersQry()                    .ToString();
				db.VwProductSalesByYear(1977)       .ToString();
				db.VwProductsAboveAveragePrice()    .ToString();
				db.VwProductsByCategory()           .ToString();
				db.VwQuarterlyOrders(1977)          .ToString();
				db.VwSalesByCategory(1977)          .ToString();
				db.VwSalesTotalsByAmount(1977, 2000).ToString();
				db.VwGetSummaryOfSalesByQuarter()   .ToString();
				db.VwGetSummaryOfSalesByYear(1977)  .ToString();
			}
		}
	}
}