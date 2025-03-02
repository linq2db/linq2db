using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Reflection;
using LinqToDB.Mapping;
using LinqToDB.Reflection;

using NUnit.Framework;

namespace Tests.Linq
{
	/// <summary>
	/// It is a sample how to create dynamic query for specific pagination
	/// </summary>
	public static class Paginator
	{
		public class PageResult<T, TCursor>
		{
			public long?   TotalCount { get; set; }
			public TCursor Cursor     { get; set; } = default!;
			public List<T> Items      { get; set; } = default!;
		}

		sealed class CteBody<T, TCursor>
		{
			public long?   TotalCount;
			public long    RowNumber;
			public TCursor Cursor      = default!;
			public T       Data        = default!;
		}

		static Expression? Unwrap(this Expression? ex)
		{
			if (ex == null)
				return null;

			switch (ex.NodeType)
			{
				case ExpressionType.Quote          :
				case ExpressionType.ConvertChecked :
				case ExpressionType.Convert        :
					return ((UnaryExpression)ex).Operand.Unwrap();
			}

			return ex;
		}

		static MethodInfo? FindMethodInfoInType(Type type, string methodName, int paramCount)
		{
			var method = type.GetRuntimeMethods()
				.FirstOrDefault(m => m.Name == methodName && m.GetParameters().Length == paramCount);
			return method;
		}

		static MethodInfo FindMethodInfo(Type type, string methodName, int paramCount)
		{
			var method = FindMethodInfoInType(type, methodName, paramCount);

			if (method != null)
				return method;

			method = type.GetInterfaces().Select(it => FindMethodInfoInType(it, methodName, paramCount))
				.FirstOrDefault(m => m != null);

			if (method == null)
				throw new InvalidOperationException($"Method '{methodName}' not found in type '{type.Name}'.");

			return method;
		}

		static IQueryable<CteBody<T, TCursor>> GetCteQuery<T, TCursor>(IOrderedQueryable<T> query, Expression<Func<T, TCursor>> cursor, bool getTotal)
		{
			var expression   = query.Expression;
			var orderByChain = new List<Tuple<Expression, bool>>();

			var current      = expression;
			while (current.NodeType == ExpressionType.Call)
			{
				var mc = (MethodCallExpression)current;
				if (typeof(Queryable) == mc.Method.DeclaringType)
				{
					var supported = true;
					switch (mc.Method.Name)
					{
						case "OrderBy":
						case "ThenBy":
							{
								orderByChain.Add(Tuple.Create(mc.Arguments[1], false));
								break;
							}
						case "OrderByDescending":
						case "ThenByDescending":
							{
								orderByChain.Add(Tuple.Create(mc.Arguments[1], true));
								break;
							}
						default:
							supported = false;
							break;
					}

					if (!supported)
						break;

					current = mc.Arguments[0];
				}
				else
					break;
			}

			// This is rest of query
			//
			var queryExpression = current;

			// Generating order part of RowNumber
			//
			Expression<Func<T, AnalyticFunctions.IOverMayHavePartitionAndOrder<long>>> overExpression =
				t => Sql.Ext.RowNumber().Over();

			var isFirst = true;

			var entityParam = Expression.Parameter(typeof(T), "e");
			Expression rowNumberBody = overExpression.Body;
			for (int i = orderByChain.Count - 1; i >= 0; i--)
			{
				var order = orderByChain[i];
				string methodName;
				if (order.Item2)
					methodName = isFirst ? "OrderByDesc" : "ThenByDesc";
				else
					methodName = isFirst ? "OrderBy" : "ThenBy";
				isFirst = false;

				var currentType = rowNumberBody.Type;
				var methodInfo = FindMethodInfo(currentType, methodName, 1).GetGenericMethodDefinition();

				var arg = ((LambdaExpression)Unwrap(order.Item1)!).GetBody(entityParam);

				rowNumberBody = Expression.Call(rowNumberBody, methodInfo.MakeGenericMethod(arg.Type), arg);
			}

			var toValueMethodInfo = FindMethodInfo(rowNumberBody.Type, "ToValue", 0);
			rowNumberBody = Expression.Call(rowNumberBody, toValueMethodInfo);

			// Generating Select
			//
			var dataMember      = MemberHelper.FieldOf((CteBody<T, TCursor> cte) => cte.Data);
			var rowNumberMember = MemberHelper.FieldOf((CteBody<T, TCursor> cte) => cte.RowNumber);
			var cursorMember    = MemberHelper.FieldOf((CteBody<T, TCursor> cte) => cte.Cursor);

			var bindings = new List<MemberBinding>();

			if (getTotal)
			{
				var totalMember = MemberHelper.FieldOf((CteBody<T, TCursor> cte) => cte.TotalCount);
				Expression<Func<int>> totalCountExpr = () => Sql.Ext.Count().Over().ToValue();

				bindings.Add(Expression.Bind(totalMember, Expression.Convert(totalCountExpr.Body, typeof(long?))));
			}

			bindings.Add(Expression.Bind(rowNumberMember, rowNumberBody));
			bindings.Add(Expression.Bind(cursorMember,    cursor.GetBody(entityParam)));
			bindings.Add(Expression.Bind(dataMember,      entityParam));

			var newExpression = Expression.New(typeof(CteBody<T, TCursor>).GetConstructor(Type.EmptyTypes)!);

			var selectLambda = Expression.Lambda(Expression.MemberInit(newExpression, bindings), entityParam);
			var selectMethod = Methods.Queryable.Select.MakeGenericMethod(typeof(T), typeof(CteBody<T, TCursor>));
			var selectQuery  = Expression.Call(selectMethod, queryExpression, Expression.Quote(selectLambda));

			// Done! CTE query is ready
			var cteQuery = query.Provider.CreateQuery<CteBody<T, TCursor>>(selectQuery);
			return cteQuery;
		}

		static IQueryable<CteBody<T, TCursor>> GetPageViaCursorQuery<T, TCursor>(IOrderedQueryable<T> source,
			Expression<Func<T, TCursor>> cursor, TCursor cursorValue, int take, bool getTotal)
		{
			var cteQuery = GetCteQuery(source, cursor, getTotal);

			IQueryable<CteBody<T, TCursor>> query;

			if (ReferenceEquals(cursorValue, null))
			{
				query = 
					from q in cteQuery
					where q.RowNumber <= take
					select q;
			}
			else
			{
				// here we need CTE
				cteQuery = cteQuery.AsCte();
				query = 
					from q in cteQuery
					where cteQuery.Any(c =>
						c.Cursor!.Equals(cursorValue) && q.RowNumber > c.RowNumber && q.RowNumber <= c.RowNumber + take)
					select q;
			}

			return query;
		}

		public static PageResult<T, TCursor> GetPageViaCursor<T, TCursor>(IOrderedQueryable<T> source,
			Expression<Func<T, TCursor>> cursor, TCursor cursorValue, int take, bool getTotal)
		{
			var query = GetPageViaCursorQuery(source, cursor, cursorValue, take, getTotal);

			var items = query.ToList();
			if (items.Count == 0)
			{
				return new PageResult<T, TCursor>
				{
					TotalCount = 0,
					Items      = new List<T>(),
					Cursor     = cursorValue
				};
			}

			return new PageResult<T, TCursor>
			{
				TotalCount = items[0].TotalCount,
				Items      = items.Select(i => i.Data).ToList(),
				Cursor     = cursorValue
			};
		}

	}

	[TestFixture]
	public class CursorPagination : TestBase
	{
		[Table]
		sealed class Booking
		{
			[Column] public int BookingID        { get; set; }
			[Column] public DateTime ServiceDate { get; set; }
			[Column] public int Value            { get; set; }
		}

		[Test]
		public void PaginationViaCursor([IncludeDataSources(false, TestProvName.AllSqlServer)] string context)
		{
			var take = 12;
			var sampleData = Enumerable.Range(1, 100).Select(i => new Booking
			{
				ServiceDate = TestData.DateTime.AddDays(-1 - i % 3),
				Value = i,
				BookingID = i
			}).ToArray();

			using (var db = (DataConnection)GetDataContext(context))
			using (var table = db.CreateLocalTable(sampleData))
			{
				var dataQuery = table.Where(t => t.ServiceDate > TestData.DateTime.AddDays(-2));

				var query = dataQuery.OrderByDescending(t => t.ServiceDate).ThenByDescending(tt => tt.BookingID);

				var expected = query.ToList();
				var actual = new List<Booking>();

				var pageResult = Paginator.GetPageViaCursor(query, b => b.BookingID, (int?)null, take, true);

				Assert.That(pageResult.TotalCount, Is.EqualTo(expected.Count));

				while (pageResult.Items.Count > 0)
				{
					actual.AddRange(pageResult.Items);
					var cursorValue = pageResult.Items.Last().BookingID;

					pageResult = Paginator.GetPageViaCursor(query, b => b.BookingID, (int?)cursorValue, take, false);

					Assert.That(db.LastQuery, Does.Not.Contain("CTE_2"));
				}

				AreEqualWithComparer(expected, actual);
			}
		}
	}
}
