using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Internal.Expressions;

namespace Tests.Linq
{
	public static class PaginationExtensions
	{
		public class PaginationResult<T>
		{
			public PaginationResult(int totalCount, int page, int pageSize, List<T> items)
			{
				TotalCount = totalCount;
				Page = page;
				PageSize = pageSize;
				Items = items;
			}

			public int TotalCount { get; }
			public List<T> Items { get; }
			public int Page { get; }
			public int PageSize { get; }
		}

		public static PaginationResult<T> Paginate<T>(this IQueryable<T> query, int page, int pageSize, bool includeTotalCount = false)
		{
			return ProcessPaginationResult(EnvelopeQuery(query, page, pageSize, includeTotalCount), pageSize);
		}

		public static Task<PaginationResult<T>> PaginateAsync<T>(this IQueryable<T> query, int page, int pageSize, bool includeTotalCount = false, CancellationToken cancellationToken = default)
		{
			return ProcessPaginationResultAsync(EnvelopeQuery(query, page, pageSize, includeTotalCount), pageSize, cancellationToken);
		}

		public static Expression ApplyOrderBy(Type entityType, Expression queryExpr, IEnumerable<Tuple<string, bool>> order)
		{
			var param = Expression.Parameter(entityType, "e");
			var isFirst = true;
			foreach (var tuple in order)
			{
				var lambda = Expression.Lambda(MakePropPath(param, tuple.Item1), param);
				var methodName =
					isFirst ? tuple.Item2 ? nameof(Queryable.OrderByDescending) : nameof(Queryable.OrderBy)
					: tuple.Item2 ? nameof(Queryable.ThenByDescending) : nameof(Queryable.ThenBy);

				queryExpr = Expression.Call(typeof(Queryable), methodName, new[] { entityType, lambda.Body.Type }, queryExpr, Expression.Quote(lambda));
				isFirst = false;
			}

			return queryExpr;
		}

		public static PaginationResult<T> GetPageByCondition<T>(this IQueryable<T> query, int pageSize,
			Expression<Func<T, bool>> predicate, bool includeTotal = false)
		{
			return ProcessPaginationResult(GetPageByConditionInternal(query, pageSize, predicate, includeTotal), pageSize);
		}

		public static int GetPageNumberByCondition<T>(this IQueryable<T> query, int pageSize,
			Expression<Func<T, bool>> predicate, bool includeTotal = false)
		{
			return GetPageNumberByConditionInternal(query, pageSize, predicate, includeTotal).FirstOrDefault();
		}

		public static Task<int> GetPageNumberByConditionAsync<T>(this IQueryable<T> query, int pageSize,
			Expression<Func<T, bool>> predicate, bool includeTotal = false, CancellationToken cancellationToken = default)
		{
			return GetPageNumberByConditionInternal(query, pageSize, predicate, includeTotal).FirstOrDefaultAsync(cancellationToken);
		}

		public static Task<PaginationResult<T>> GetPageByConditionAsync<T>(this IQueryable<T> query, int pageSize,
			Expression<Func<T, bool>> predicate, bool includeTotal = false, CancellationToken cancellationToken = default)
		{
			return ProcessPaginationResultAsync(GetPageByConditionInternal(query, pageSize, predicate, includeTotal), pageSize, cancellationToken);
		}

		public static IQueryable<T> ApplyOrderBy<T>(this IQueryable<T> query, IEnumerable<Tuple<string, bool>> order)
		{
			var expr = ApplyOrderBy(typeof(T), query.Expression, order);
			return query.Provider.CreateQuery<T>(expr);
		}

		#region Helpers

		[return: NotNullIfNotNull(nameof(ex))]
		static Expression? Unwrap(this Expression? ex)
		{
			if (ex == null)
				return null;

			switch (ex.NodeType)
			{
				case ExpressionType.Quote:
				case ExpressionType.ConvertChecked:
				case ExpressionType.Convert:
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
				throw new LinqToDBException($"Method '{methodName}' not found in type '{type.Name}'.");

			return method;
		}

		static Expression ExtractOrderByPart(Expression query, List<Tuple<Expression, bool>> orderBy)
		{
			var current = query;
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
							orderBy.Add(Tuple.Create(mc.Arguments[1], false));
							break;
						}
						case "OrderByDescending":
						case "ThenByDescending":
						{
							orderBy.Add(Tuple.Create(mc.Arguments[1], true));
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

			return current;
		}

		static Expression FinalizeFunction(Expression functionBody)
		{
			var toValueMethodInfo = FindMethodInfo(functionBody.Type, "ToValue", 0);
			functionBody = Expression.Call(functionBody, toValueMethodInfo);
			return functionBody;
		}

		static Expression GenerateOrderBy(Expression entity, Expression functionBody, List<Tuple<Expression, bool>> orderBy)
		{
			var isFirst = true;

			for (int i = orderBy.Count - 1; i >= 0; i--)
			{
				var order = orderBy[i];
				string methodName;
				if (order.Item2)
					methodName = isFirst ? "OrderByDesc" : "ThenByDesc";
				else
					methodName = isFirst ? "OrderBy" : "ThenBy";
				isFirst = false;

				var currentType = functionBody.Type;
				var methodInfo = FindMethodInfo(currentType, methodName, 1).GetGenericMethodDefinition();

				var arg = ((LambdaExpression)Unwrap(order.Item1)!).GetBody(entity);

				functionBody = Expression.Call(functionBody, methodInfo.MakeGenericMethod(arg.Type), arg);
			}

			return functionBody;
		}

		static Expression GeneratePartitionBy(Expression functionBody, Expression[] partitionBy)
		{
			if (partitionBy.Length == 0)
				return functionBody;

			var method = FindMethodInfo(functionBody.Type, "PartitionBy", 1);

			var partitionsExpr = Expression.NewArrayInit(typeof(object), partitionBy);

			var call = Expression.Call(functionBody, method, partitionsExpr);

			return call;
		}

		static Expression MakePropPath(Expression objExpression, string path)
		{
			return path.Split('.').Aggregate(objExpression, Expression.PropertyOrField);
		}

		private sealed class Envelope<T>
		{
			public int TotalCount { get; set; }
			public T Data { get; set; } = default!;
			public int Page { get; set; }
		}

		static IQueryable<Envelope<T>> EnvelopeQuery<T>(IQueryable<T> query, int page, int pageSize, bool includeTotalCount)
		{
			var withCount = includeTotalCount
				? query.Select(q =>
					new Envelope<T> {TotalCount = Sql.Ext.Count().Over().ToValue(), Page = page, Data = q})
				: query.Select(q => new Envelope<T> {TotalCount = -1, Page = page, Data = q});

			return withCount.Skip((page - 1) * pageSize).Take(pageSize);
		}

		static PaginationResult<T> ProcessPaginationResult<T>(IQueryable<Envelope<T>> query, int pageSize)
		{
			int totalRecords;
			int page = 0;

			using (var enumerator = query.GetEnumerator())
			{
				List<T> result;
				if (!enumerator.MoveNext())
				{
					totalRecords = 0;
					result = new List<T>();
				}
				else
				{
					totalRecords = enumerator.Current.TotalCount;
					page = enumerator.Current.Page;
					result = new List<T>(pageSize);
					do
					{
						result.Add(enumerator.Current.Data);
					} while (enumerator.MoveNext());
				}

				return new PaginationResult<T>(totalRecords, page, pageSize, result);
			}
		}

		static async Task<PaginationResult<T>> ProcessPaginationResultAsync<T>(IQueryable<Envelope<T>> query, int pageSize, CancellationToken cancellationToken)
		{
			var items = query.AsAsyncEnumerable();
			int totalRecords;
			int page = 0;

			await using (var enumerator = items.GetAsyncEnumerator(cancellationToken))
			{
				List<T> result;
				if (!await enumerator.MoveNextAsync())
				{
					totalRecords = 0;
					result = new List<T>();
				}
				else
				{
					totalRecords = enumerator.Current.TotalCount;
					page = enumerator.Current.Page;
					result = new List<T>(pageSize);
					do
					{
						result.Add(enumerator.Current.Data);
					} while (await enumerator.MoveNextAsync());
				}

				return new PaginationResult<T>(totalRecords, page, pageSize, result);
			}
		}

		sealed class RownNumberHolder<T>
		{
			public T Data = default!;
			public long RowNumber;
			public int TotalCount;
		}

		static Expression<Func<int>> _totalCountTemplate = () => Sql.Ext.Count().Over().ToValue();
		static Expression _totalCountEmpty = Expression.Constant(-1);

		static Expression GetRowNumberQuery<T>(Expression queryWithoutOrder, List<Tuple<Expression, bool>> orderBy, bool includeTotal)
		{
			if (orderBy.Count == 0)
				throw new InvalidOperationException("OrderBy for query is not specified");

			Expression<Func<T, AnalyticFunctions.IOverMayHavePartitionAndOrder<long>>> overExpression =
				t => Sql.Ext.RowNumber().Over();

			Expression<Func<IQueryable<T>, long, int, IQueryable<RownNumberHolder<T>>>> selectExpression =
				(q, rn, tc) => q.Select(x => new RownNumberHolder<T> {Data = x, RowNumber = rn, TotalCount = tc});

			Expression totalCountExpr = includeTotal ? _totalCountTemplate.Body : _totalCountEmpty;

			var entityParam = ((LambdaExpression)((MethodCallExpression)selectExpression.Body).Arguments[1].Unwrap())
				.Parameters[0];

			var windowFunctionBody = overExpression.Body;
			windowFunctionBody = GenerateOrderBy(entityParam, windowFunctionBody, orderBy);
			windowFunctionBody = FinalizeFunction(windowFunctionBody);

			var queryExpr = selectExpression.GetBody(queryWithoutOrder, windowFunctionBody, totalCountExpr);

			return queryExpr;
		}

		static IQueryable<Envelope<T>> GetPageByConditionInternal<T>(IQueryable<T> query, int pageSize, Expression<Func<T, bool>> predicate, bool includeTotal)
		{
			Expression<Func<IQueryable<RownNumberHolder<T>>, IQueryable<RownNumberHolder<T>>>> cteCall = q => q.AsCte("pagination_cte");

			var queryExpr = query.Expression;

			var orderBy = new List<Tuple<Expression, bool>>();
			var withoutOrder = ExtractOrderByPart(queryExpr, orderBy);

			var rnQueryExpr = GetRowNumberQuery<T>(withoutOrder, orderBy, includeTotal);
			rnQueryExpr = cteCall.GetBody(rnQueryExpr);

			Expression<Func<IQueryable<RownNumberHolder<T>>, Expression<Func<RownNumberHolder<T>, bool>>, int,
				IQueryable<Envelope<T>>>> dataTemplate =
				includeTotal
					? (q, f, ps) =>
						q
							.Where(f).Take(1).Select(x => (int)(x.RowNumber - 1) / ps + 1)
							.SelectMany(page => q.Where(x => x.RowNumber.Between((page - 1) * ps + 1, page * ps))
								.OrderBy(x => x.RowNumber)
								.Select(x =>
									new Envelope<T>
									{
										Data = x.Data, Page = page, TotalCount = (int)x.TotalCount
									}))
					: (q, f, ps) =>
						q
							.Where(f).Take(1).Select(x => (int)(x.RowNumber - 1) / ps + 1)
							.SelectMany(page => q.Where(x => x.RowNumber.Between((page - 1) * ps + 1, page * ps))
								.OrderBy(x => x.RowNumber)
								.Select(x =>
									new Envelope<T> {Data = x.Data, Page = page, TotalCount = -1}));

			var param = Expression.Parameter(typeof(RownNumberHolder<T>), "h");
			var newPredicate = Expression.Lambda(predicate.GetBody(Expression.PropertyOrField(param, "Data")), param);

			var resultExpr = dataTemplate.GetBody(rnQueryExpr, newPredicate, Expression.Constant(pageSize));
			return query.Provider.CreateQuery<Envelope<T>>(resultExpr);
		}

		static IQueryable<int> GetPageNumberByConditionInternal<T>(IQueryable<T> query, int pageSize, Expression<Func<T, bool>> predicate, bool includeTotal)
		{
			var queryExpr = query.Expression;

			var orderBy = new List<Tuple<Expression, bool>>();
			var withoutOrder = ExtractOrderByPart(queryExpr, orderBy);

			var rnQueryExpr = GetRowNumberQuery<T>(withoutOrder, orderBy, includeTotal);

			Expression<Func<IQueryable<RownNumberHolder<T>>, Expression<Func<RownNumberHolder<T>, bool>>, int,
				IQueryable<int>>> dataTemplate =
				(q, f, ps) =>
					q.AsSubQuery().Where(f).Select(x => (int)((x.RowNumber - 1) / ps + 1));

			var param = Expression.Parameter(typeof(RownNumberHolder<T>), "h");
			var newPredicate = Expression.Lambda(predicate.GetBody(Expression.PropertyOrField(param, "Data")), param);

			var resultExpr = dataTemplate.GetBody(rnQueryExpr, newPredicate, Expression.Constant(pageSize));
			return query.Provider.CreateQuery<int>(resultExpr);
		}

		#endregion
	}
}
