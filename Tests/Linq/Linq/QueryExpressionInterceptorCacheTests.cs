using System;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Interceptors;
using LinqToDB.Internal.Linq;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.Linq
{
	/// <summary>
	/// A query translated for one context must not be reused by another context whose
	/// <see cref="IQueryExpressionInterceptor"/>s would translate it differently.
	/// </summary>
	[TestFixture]
	public class QueryExpressionInterceptorCacheTests : TestBase
	{
		[Table]
		sealed class CacheItem
		{
			[PrimaryKey] public int Id { get; set; }
		}

		public enum Kind
		{
			Query,
			ExposedQuery,
			QueryFilterLambda,
			QueryFilterFunc,
		}

		public enum Scenario
		{
			// A: DataConnection.AddInterceptor(Le2), B: no interceptor
			AddInterceptorVsNone,
			// A: UseInterceptor(Le2), B: no interceptor
			OptionsVsNone,
			// A: UseInterceptor(Le2), B: UseInterceptor(Gt2)
			OptionsSameTypeDifferentConfig,
			// A: AddInterceptor(Le2), B: AddInterceptor(Gt2)
			AddInterceptorSameTypeDifferentConfig,
			// A: UseInterceptor(Le2), B: UseInterceptor of the same type that leaves the tree unchanged
			OptionsSameTypeOneUnchanged,
		}

		public enum Mode
		{
			Le2,
			Gt2,
			Unchanged,
		}

		// Appends Where(e => e.Id <= 2) or Where(e => e.Id > 2) to the expression of the configured kind.
		sealed class FilterInterceptor(QueryExpressionArgs.ExpressionKind kind, Mode mode) : IQueryExpressionInterceptor
		{
			const string Marker = "__icache";

			public Expression ProcessExpression(Expression expression, QueryExpressionArgs args)
			{
				if (mode == Mode.Unchanged || args.Kind != kind || !typeof(IQueryable<CacheItem>).IsAssignableFrom(expression.Type))
					return expression;

				if (expression is MethodCallExpression { Method.Name: "Where" } mc
					&& mc.Arguments[1] is UnaryExpression { Operand: LambdaExpression { Parameters: [{ Name: Marker }] } })
					return expression;

				var p    = Expression.Parameter(typeof(CacheItem), Marker);
				var id   = Expression.Property(p, nameof(CacheItem.Id));
				var two  = Expression.Constant(2);
				var body = mode == Mode.Le2 ? Expression.LessThanOrEqual(id, two) : Expression.GreaterThan(id, two);

				return Expression.Call(
					typeof(Queryable), nameof(Queryable.Where), [typeof(CacheItem)],
					expression,
					Expression.Quote(Expression.Lambda<Func<CacheItem, bool>>(body, p)));
			}
		}

		// Adds "Id <= Limit" to the query filter; instances differ only by the value they read from themselves.
		sealed class LimitFilterInterceptor(int limit) : IQueryExpressionInterceptor
		{
			public int Limit => limit;

			public Expression ProcessExpression(Expression expression, QueryExpressionArgs args)
			{
				if (args.Kind != QueryExpressionArgs.ExpressionKind.QueryFilter || !typeof(IQueryable<CacheItem>).IsAssignableFrom(expression.Type))
					return expression;

				var p    = Expression.Parameter(typeof(CacheItem), "e");
				var body = Expression.LessThanOrEqual(
					Expression.Property(p, nameof(CacheItem.Id)),
					Expression.Property(Expression.Constant(this), nameof(Limit)));

				return Expression.Call(
					typeof(Queryable), nameof(Queryable.Where), [typeof(CacheItem)],
					expression,
					Expression.Quote(Expression.Lambda<Func<CacheItem, bool>>(body, p)));
			}
		}

		static readonly MappingSchema _lambdaFilterSchema = new FluentMappingBuilder(new MappingSchema())
			.Entity<CacheItem>().HasQueryFilter(e => e.Id > 0)
			.Build()
			.MappingSchema;

		static readonly MappingSchema _funcFilterSchema = new FluentMappingBuilder(new MappingSchema())
			.Entity<CacheItem>().HasQueryFilter((q, dc) => q.Where(e => e.Id > 0))
			.Build()
			.MappingSchema;

		static QueryExpressionArgs.ExpressionKind ToArgsKind(Kind kind)
		{
			return kind switch
			{
				Kind.Query        => QueryExpressionArgs.ExpressionKind.Query,
				Kind.ExposedQuery => QueryExpressionArgs.ExpressionKind.ExposedQuery,
				_                 => QueryExpressionArgs.ExpressionKind.QueryFilter,
			};
		}

		static int[] Expected(Mode? mode)
		{
			return mode switch
			{
				Mode.Le2 => [1, 2],
				Mode.Gt2 => [3, 4],
				_        => [1, 2, 3, 4],
			};
		}

		static int[] Run(IDataContext db)
		{
			return db.GetTable<CacheItem>().ToArray().Select(e => e.Id).OrderBy(i => i).ToArray();
		}

		static (int[] a, int[] b) RunBoth(IDataContext a, IDataContext b, bool aFirst)
		{
			int[] resA, resB;

			if (aFirst)
			{
				resA = Run(a);
				resB = Run(b);
			}
			else
			{
				resB = Run(b);
				resA = Run(a);
			}

			return (resA, resB);
		}

		[Test, QueryCacheTest]
		public void RuntimeInterceptorVsNone([IncludeDataSources(true, ProviderName.SQLiteMS)] string context, [Values] bool aFirst)
		{
			Query.ClearCaches();

			using var setup = GetDataContext(context, o => o.UseMappingSchema(_lambdaFilterSchema));
			using var t     = setup.CreateLocalTable(Enumerable.Range(1, 4).Select(i => new CacheItem { Id = i }));

			using var a = GetDataContext(context, o => o.UseMappingSchema(_lambdaFilterSchema));
			a.AddInterceptor(new LimitFilterInterceptor(2));
			using var b = GetDataContext(context, o => o.UseMappingSchema(_lambdaFilterSchema));

			var (resA, resB) = RunBoth(a, b, aFirst);

			using (Assert.EnterMultipleScope())
			{
				resA.ShouldBe([1, 2], "context A");
				resB.ShouldBe([1, 2, 3, 4], "context B");
			}
		}

		[Test, QueryCacheTest]
		public void SameTypeDifferentConfiguration([IncludeDataSources(ProviderName.SQLiteMS)] string context, [Values] bool aFirst)
		{
			Query.ClearCaches();

			using var setup = GetDataConnection(context, o => o.UseMappingSchema(_lambdaFilterSchema));
			using var t     = setup.CreateLocalTable(Enumerable.Range(1, 4).Select(i => new CacheItem { Id = i }));

			using var a = GetDataConnection(context, o => o.UseMappingSchema(_lambdaFilterSchema).UseInterceptor(new LimitFilterInterceptor(2)));
			using var b = GetDataConnection(context, o => o.UseMappingSchema(_lambdaFilterSchema).UseInterceptor(new LimitFilterInterceptor(3)));

			var (resA, resB) = RunBoth(a, b, aFirst);

			using (Assert.EnterMultipleScope())
			{
				resA.ShouldBe([1, 2], "context A");
				resB.ShouldBe([1, 2, 3], "context B");
			}
		}

		// Instances of one interceptor type that differ only by the value they read must still share one cached query.
		[Test, QueryCacheTest]
		public void SameTypeDifferentConfigurationSharesCachedQuery([IncludeDataSources(ProviderName.SQLiteMS)] string context)
		{
			Query.ClearCaches();

			using var setup = GetDataConnection(context, o => o.UseMappingSchema(_lambdaFilterSchema));
			using var t     = setup.CreateLocalTable(Enumerable.Range(1, 4).Select(i => new CacheItem { Id = i }));

			for (var limit = 1; limit <= 4; limit++)
			{
				using var db = GetDataConnection(context, o => o.UseMappingSchema(_lambdaFilterSchema));
				db.AddInterceptor(new LimitFilterInterceptor(limit));

				var query = db.GetTable<CacheItem>();
				var misses = query.GetCacheMissCount();

				Run(db).ShouldBe(Enumerable.Range(1, limit).ToArray());

				if (limit > 1)
					query.GetCacheMissCount().ShouldBe(misses, $"limit {limit}");
			}
		}

		[Test, QueryCacheTest]
		public void InterceptorParticipatesInCacheKey(
			[IncludeDataSources(ProviderName.SQLiteMS)] string   context,
			[Values]                                    Kind     kind,
			[Values]                                    Scenario scenario,
			[Values]                                    bool     aFirst)
		{
			var ms       = kind == Kind.QueryFilterFunc ? _funcFilterSchema : _lambdaFilterSchema;
			var argsKind = ToArgsKind(kind);

			Mode? modeA = Mode.Le2;
			Mode? modeB = scenario switch
			{
				Scenario.OptionsSameTypeDifferentConfig or Scenario.AddInterceptorSameTypeDifferentConfig => Mode.Gt2,
				Scenario.OptionsSameTypeOneUnchanged                                                      => Mode.Unchanged,
				_                                                                                         => null,
			};

			var viaOptions = scenario is Scenario.OptionsVsNone or Scenario.OptionsSameTypeDifferentConfig or Scenario.OptionsSameTypeOneUnchanged;

			DataConnection Create(Mode? mode)
			{
				if (mode == null)
					return GetDataConnection(context, o => o.UseMappingSchema(ms));

				var interceptor = new FilterInterceptor(argsKind, mode.Value);

				if (viaOptions)
					return GetDataConnection(context, o => o.UseMappingSchema(ms).UseInterceptor(interceptor));

				var dc = GetDataConnection(context, o => o.UseMappingSchema(ms));
				dc.AddInterceptor(interceptor);
				return dc;
			}

			Query.ClearCaches();

			using var setup = GetDataConnection(context, o => o.UseMappingSchema(ms));
			using var table = setup.CreateLocalTable(Enumerable.Range(1, 4).Select(i => new CacheItem { Id = i }));

			using var dbA = Create(modeA);
			using var dbB = Create(modeB);

			var (resA, resB) = RunBoth(dbA, dbB, aFirst);

			using (Assert.EnterMultipleScope())
			{
				resA.ShouldBe(Expected(modeA), "context A");
				resB.ShouldBe(Expected(modeB), "context B");
			}
		}

		// DataContext.AddInterceptor after the context's configuration id has been computed.
		[Test, QueryCacheTest]
		public void DataContextAddInterceptorAfterQuery(
			[IncludeDataSources(ProviderName.SQLiteMS)] string context,
			[Values]                                    bool   aFirst)
		{
			var ms = _lambdaFilterSchema;

			Query.ClearCaches();

			using var setup = GetDataConnection(context, o => o.UseMappingSchema(ms));
			using var table = setup.CreateLocalTable(Enumerable.Range(1, 4).Select(i => new CacheItem { Id = i }));

			var options = new DataOptions().UseConfiguration(context).UseMappingSchema(ms);

			using var dbA = new DataContext(options);
			using var dbB = new DataContext(options);

			_ = dbA.GetTable<CacheItem>().Count(e => e.Id < 0);

			dbA.AddInterceptor(new FilterInterceptor(QueryExpressionArgs.ExpressionKind.QueryFilter, Mode.Le2));

			var (resA, resB) = RunBoth(dbA, dbB, aFirst);

			using (Assert.EnterMultipleScope())
			{
				resA.ShouldBe([1, 2], "context A");
				resB.ShouldBe([1, 2, 3, 4], "context B");
			}
		}

		[Test, QueryCacheTest]
		public void RemoveInterceptor([IncludeDataSources(ProviderName.SQLiteMS)] string context, [Values] bool viaDataContext)
		{
			var ms = _lambdaFilterSchema;

			Query.ClearCaches();

			using var setup = GetDataConnection(context, o => o.UseMappingSchema(ms));
			using var table = setup.CreateLocalTable(Enumerable.Range(1, 4).Select(i => new CacheItem { Id = i }));

			using IDataContext db = viaDataContext
				? new DataContext(new DataOptions().UseConfiguration(context).UseMappingSchema(ms))
				: GetDataConnection(context, o => o.UseMappingSchema(ms));

			var interceptor = new FilterInterceptor(QueryExpressionArgs.ExpressionKind.QueryFilter, Mode.Le2);

			Run(db).ShouldBe([1, 2, 3, 4]);

			db.AddInterceptor(interceptor);
			Run(db).ShouldBe([1, 2]);

			db.RemoveInterceptor(interceptor);
			Run(db).ShouldBe([1, 2, 3, 4]);
		}
	}
}
