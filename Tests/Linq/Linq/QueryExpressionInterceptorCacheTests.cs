using System;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Expressions;
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

		static readonly MappingSchema _noFilterSchema = new MappingSchema();

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
		// Adds Where(e => e.Id <= 2) or Where(e => e.Id > 2) only to a filter chain that contains a "<" comparison.
		sealed class ChainFilterInterceptor(Mode mode) : IQueryExpressionInterceptor
		{
			public Expression ProcessExpression(Expression expression, QueryExpressionArgs args)
			{
				if (args.Kind != QueryExpressionArgs.ExpressionKind.QueryFilter || !typeof(IQueryable<CacheItem>).IsAssignableFrom(expression.Type))
					return expression;

				if (expression.Find(static e => e.NodeType == ExpressionType.LessThan) == null)
					return expression;

				var p    = Expression.Parameter(typeof(CacheItem), "e");
				var id   = Expression.Property(p, nameof(CacheItem.Id));
				var two  = Expression.Constant(2);
				var body = mode == Mode.Le2 ? Expression.LessThanOrEqual(id, two) : Expression.GreaterThan(id, two);

				return Expression.Call(
					typeof(Queryable), nameof(Queryable.Where), [typeof(CacheItem)],
					expression,
					Expression.Quote(Expression.Lambda<Func<CacheItem, bool>>(body, p)));
			}
		}

		static readonly MappingSchema _namedLambdaFilterSchema = new FluentMappingBuilder(new MappingSchema())
			.Entity<CacheItem>()
				.HasQueryFilter("lo", e => e.Id > 0)
				.HasQueryFilter("hi", e => e.Id < 5)
			.Build()
			.MappingSchema;

		static readonly MappingSchema _namedFuncFilterSchema = new FluentMappingBuilder(new MappingSchema())
			.Entity<CacheItem>()
				.HasQueryFilter("lo", (q, dc) => q.Where(e => e.Id > 0))
				.HasQueryFilter("hi", (q, dc) => q.Where(e => e.Id < 5))
			.Build()
			.MappingSchema;

		// Two references to one table with different active filter sets; the interceptor rewrites only the "hi" chain.
		[Test, QueryCacheTest]
		public void DifferentFilterSetsOnOneTable(
			[IncludeDataSources(ProviderName.SQLiteMS)] string context,
			[Values]                                    bool   funcFilters,
			[Values]                                    bool   aFirst)
		{
			var ms = funcFilters ? _namedFuncFilterSchema : _namedLambdaFilterSchema;

			Query.ClearCaches();

			using var setup = GetDataConnection(context, o => o.UseMappingSchema(ms));
			using var table = setup.CreateLocalTable(Enumerable.Range(1, 4).Select(i => new CacheItem { Id = i }));

			using var a = GetDataConnection(context, o => o.UseMappingSchema(ms).UseInterceptor(new ChainFilterInterceptor(Mode.Le2)));
			using var b = GetDataConnection(context, o => o.UseMappingSchema(ms).UseInterceptor(new ChainFilterInterceptor(Mode.Gt2)));

			static int[] RunConcat(IDataContext db)
			{
				return db.GetTable<CacheItem>().IgnoreFilters(["hi"])
					.Concat(db.GetTable<CacheItem>().IgnoreFilters(["lo"]))
					.Select(e => e.Id)
					.ToArray()
					.OrderBy(i => i)
					.ToArray();
			}

			int[] resA, resB;

			if (aFirst)
			{
				resA = RunConcat(a);
				resB = RunConcat(b);
			}
			else
			{
				resB = RunConcat(b);
				resA = RunConcat(a);
			}

			using (Assert.EnterMultipleScope())
			{
				resA.ShouldBe([1, 1, 2, 2, 3, 4], "context A");
				resB.ShouldBe([1, 2, 3, 3, 4, 4], "context B");
			}
		}

		// Several references to one table with the same filter set: every reference takes the executing context's value.
		[Test, QueryCacheTest]
		public void SeveralReferencesToOneTable([IncludeDataSources(ProviderName.SQLiteMS)] string context, [Values] bool join)
		{
			Query.ClearCaches();

			using var setup = GetDataConnection(context, o => o.UseMappingSchema(_lambdaFilterSchema));
			using var t     = setup.CreateLocalTable(Enumerable.Range(1, 4).Select(i => new CacheItem { Id = i }));

			int[] RunQuery(int limit)
			{
				using var db = GetDataConnection(context, o => o.UseMappingSchema(_lambdaFilterSchema).UseInterceptor(new LimitFilterInterceptor(limit)));

				var query = join
					? from x in db.GetTable<CacheItem>()
					  from y in db.GetTable<CacheItem>().Where(y => y.Id >= x.Id)
					  select y.Id
					: db.GetTable<CacheItem>().Select(e => e.Id).Concat(db.GetTable<CacheItem>().Select(e => e.Id));

				return query.ToArray().OrderBy(i => i).ToArray();
			}

			using (Assert.EnterMultipleScope())
			{
				RunQuery(2).ShouldBe(join ? [1, 2, 2] : [1, 1, 2, 2], "limit 2");
				RunQuery(3).ShouldBe(join ? [1, 2, 2, 3, 3, 3] : [1, 1, 2, 2, 3, 3], "limit 3");
			}
		}

		sealed class LimitConnection(DataOptions options, int limit) : DataConnection(options)
		{
			public int Limit => limit;
		}

		static readonly MappingSchema _contextFuncFilterSchema = new FluentMappingBuilder(new MappingSchema())
			.Entity<CacheItem>().HasQueryFilter<LimitConnection>((q, dc) => q.Where(e => e.Id <= dc.Limit))
			.Build()
			.MappingSchema;

		static readonly Func<DataConnection, IQueryable<CacheItem>> _compiledTable  = CompiledQuery.Compile((DataConnection db) => db.GetTable<CacheItem>());
		static readonly Func<DataConnection, int>                   _compiledCount  = CompiledQuery.Compile((DataConnection db) => db.GetTable<CacheItem>().Count());

		// A compiled query must not reuse the filter value of the context it was first built for.
		[Test, QueryCacheTest]
		public void CompiledQueryInterceptorFilter([IncludeDataSources(ProviderName.SQLiteMS)] string context, [Values] bool count)
		{
			Query.ClearCaches();

			using var setup = GetDataConnection(context, o => o.UseMappingSchema(_lambdaFilterSchema));
			using var t     = setup.CreateLocalTable(Enumerable.Range(1, 4).Select(i => new CacheItem { Id = i }));

			using var a = GetDataConnection(context, o => o.UseMappingSchema(_lambdaFilterSchema).UseInterceptor(new LimitFilterInterceptor(2)));
			using var b = GetDataConnection(context, o => o.UseMappingSchema(_lambdaFilterSchema).UseInterceptor(new LimitFilterInterceptor(3)));

			using (Assert.EnterMultipleScope())
			{
				if (count)
				{
					_compiledCount(a).ShouldBe(2, "context A");
					_compiledCount(b).ShouldBe(3, "context B");
					_compiledCount(a).ShouldBe(2, "context A again");
				}
				else
				{
					var queryA = _compiledTable(a);
					var queryB = _compiledTable(b);

					queryA.ToArray().Length.ShouldBe(2, "context A");
					queryB.ToArray().Length.ShouldBe(3, "context B");
					_compiledTable(a).ToArray().Length.ShouldBe(2, "context A again");

					// the contexts differ only by a value: they share one built query
					queryB.GetCompiledQueryInfo().ShouldBeSameAs(queryA.GetCompiledQueryInfo());
				}
			}
		}

		// The same for a FilterFunc filter that reads a value from the data context.
		[Test, QueryCacheTest]
		public void CompiledQueryContextFilter([IncludeDataSources(ProviderName.SQLiteMS)] string context, [Values] bool count)
		{
			Query.ClearCaches();

			using var setup = GetDataConnection(context, o => o.UseMappingSchema(_contextFuncFilterSchema));
			using var t     = setup.CreateLocalTable(Enumerable.Range(1, 4).Select(i => new CacheItem { Id = i }));

			var options = new DataOptions().UseConfiguration(context).UseMappingSchema(_contextFuncFilterSchema);

			using var a = new LimitConnection(options, 2);
			using var b = new LimitConnection(options, 3);

			using (Assert.EnterMultipleScope())
			{
				if (count)
				{
					_compiledCount(a).ShouldBe(2, "context A");
					_compiledCount(b).ShouldBe(3, "context B");
				}
				else
				{
					_compiledTable(a).ToArray().Length.ShouldBe(2, "context A");
					_compiledTable(b).ToArray().Length.ShouldBe(3, "context B");
				}
			}
		}

		public enum Scope
		{
			UseOptions,
			UseMappingSchema,
		}

		// An interceptor added inside a UseOptions / UseMappingSchema scope stays registered after the scope ends.
		[Test, QueryCacheTest]
		public void AddInterceptorInsideScope(
			[IncludeDataSources(ProviderName.SQLiteMS)] string context,
			[Values]                                    Scope  scope,
			[Values]                                    bool   viaDataContext,
			[Values]                                    bool   queryInsideScope)
		{
			var ms = _lambdaFilterSchema;

			Query.ClearCaches();

			using var setup = GetDataConnection(context, o => o.UseMappingSchema(ms));
			using var table = setup.CreateLocalTable(Enumerable.Range(1, 4).Select(i => new CacheItem { Id = i }));

			using IDataContext db = viaDataContext
				? new DataContext(new DataOptions().UseConfiguration(context).UseMappingSchema(ms))
				: GetDataConnection(context, o => o.UseMappingSchema(ms));

			Run(db).ShouldBe([1, 2, 3, 4]);

			using (scope == Scope.UseOptions
				? db.UseOptions(o => o.UseCommandTimeout(17))
				: db.UseMappingSchema(new MappingSchema()))
			{
				db.AddInterceptor(new LimitFilterInterceptor(2));

				if (queryInsideScope)
					Run(db).ShouldBe([1, 2], "inside scope");
			}

			Run(db).ShouldBe([1, 2], "after scope");
		}

		// An IQueryable executed again after AddInterceptor must not reuse the query it cached for itself.
		[Test, QueryCacheTest]
		public void ReuseQueryableAfterAddInterceptor(
			[IncludeDataSources(ProviderName.SQLiteMS)] string context,
			[Values(Kind.Query, Kind.ExposedQuery, Kind.QueryFilterLambda)] Kind kind,
			[Values]                                    bool   viaDataContext)
		{
			var ms = kind == Kind.QueryFilterLambda ? _lambdaFilterSchema : _noFilterSchema;

			Query.ClearCaches();

			using var setup = GetDataConnection(context, o => o.UseMappingSchema(ms));
			using var table = setup.CreateLocalTable(Enumerable.Range(1, 4).Select(i => new CacheItem { Id = i }));

			using IDataContext db = viaDataContext
				? new DataContext(new DataOptions().UseConfiguration(context).UseMappingSchema(ms))
				: GetDataConnection(context, o => o.UseMappingSchema(ms));

			var interceptor = new FilterInterceptor(ToArgsKind(kind), Mode.Le2);
			var query       = db.GetTable<CacheItem>();

			query.ToArray().Length.ShouldBe(4);

			db.AddInterceptor(interceptor);
			query.ToArray().Length.ShouldBe(2, "after AddInterceptor");

			db.RemoveInterceptor(interceptor);
			query.ToArray().Length.ShouldBe(4, "after RemoveInterceptor");
		}
		sealed class Box(int value)
		{
			public int Value => value;
		}

		// Adds Where(e => e.Id >= low.Value && e.Id <= high.Value) to the query filter.
		sealed class RangeFilterInterceptor(int low, int high) : IQueryExpressionInterceptor
		{
			readonly Box _low  = new(low);
			readonly Box _high = new(high);

			public Expression ProcessExpression(Expression expression, QueryExpressionArgs args)
			{
				if (args.Kind != QueryExpressionArgs.ExpressionKind.QueryFilter || !typeof(IQueryable<CacheItem>).IsAssignableFrom(expression.Type))
					return expression;

				var p    = Expression.Parameter(typeof(CacheItem), "e");
				var id   = Expression.Property(p, nameof(CacheItem.Id));
				var body = Expression.AndAlso(
					Expression.GreaterThanOrEqual(id, Expression.Property(Expression.Constant(_low),  nameof(Box.Value))),
					Expression.LessThanOrEqual   (id, Expression.Property(Expression.Constant(_high), nameof(Box.Value))));

				return Expression.Call(
					typeof(Queryable), nameof(Queryable.Where), [typeof(CacheItem)],
					expression,
					Expression.Quote(Expression.Lambda<Func<CacheItem, bool>>(body, p)));
			}
		}

		// Equal values at build time let the translator merge two parameters into one; a context with different values must
		// not reuse that query, compiled or not.
		[Test, QueryCacheTest]
		public void MergedParametersFromInterceptor([IncludeDataSources(ProviderName.SQLiteMS)] string context, [Values] bool compiled, [Values] bool aFirst)
		{
			Query.ClearCaches();

			using var setup = GetDataConnection(context, o => o.UseMappingSchema(_lambdaFilterSchema));
			using var t     = setup.CreateLocalTable(Enumerable.Range(1, 4).Select(i => new CacheItem { Id = i }));

			using var a = GetDataConnection(context, o => o.UseMappingSchema(_lambdaFilterSchema).UseInterceptor(new RangeFilterInterceptor(2, 2)));
			using var b = GetDataConnection(context, o => o.UseMappingSchema(_lambdaFilterSchema).UseInterceptor(new RangeFilterInterceptor(1, 3)));

			int[] RunQuery(DataConnection db)
			{
				var query = compiled ? _compiledTable(db) : db.GetTable<CacheItem>();

				return query.ToArray().Select(e => e.Id).OrderBy(i => i).ToArray();
			}

			int[] resA, resB;

			if (aFirst)
			{
				resA = RunQuery(a);
				resB = RunQuery(b);
			}
			else
			{
				resB = RunQuery(b);
				resA = RunQuery(a);
			}

			using (Assert.EnterMultipleScope())
			{
				resA.ShouldBe([2], "context A");
				resB.ShouldBe([1, 2, 3], "context B");
				_compiledCount(a).ShouldBe(1, "context A, compiled count");
				_compiledCount(b).ShouldBe(3, "context B, compiled count");
			}
		}

		// An IQueryable executed again after its query interceptor was replaced by another instance of the same type.
		[Test, QueryCacheTest]
		public void ReuseQueryableAfterReplacingInterceptor(
			[IncludeDataSources(ProviderName.SQLiteMS)] string context,
			[Values(Kind.Query, Kind.ExposedQuery, Kind.QueryFilterLambda)] Kind kind,
			[Values]                                    bool   viaDataContext)
		{
			var ms = kind == Kind.QueryFilterLambda ? _lambdaFilterSchema : _noFilterSchema;

			Query.ClearCaches();

			using var setup = GetDataConnection(context, o => o.UseMappingSchema(ms));
			using var table = setup.CreateLocalTable(Enumerable.Range(1, 4).Select(i => new CacheItem { Id = i }));

			using IDataContext db = viaDataContext
				? new DataContext(new DataOptions().UseConfiguration(context).UseMappingSchema(ms))
				: GetDataConnection(context, o => o.UseMappingSchema(ms));

			var unchanged = new FilterInterceptor(ToArgsKind(kind), Mode.Unchanged);
			var limiting  = new FilterInterceptor(ToArgsKind(kind), Mode.Le2);

			db.AddInterceptor(unchanged);

			var query = db.GetTable<CacheItem>();

			query.ToArray().Length.ShouldBe(4);

			db.RemoveInterceptor(unchanged);
			db.AddInterceptor(limiting);

			using (Assert.EnterMultipleScope())
			{
				query.ToArray().Length.ShouldBe(2, "same IQueryable");
				db.GetTable<CacheItem>().ToArray().Length.ShouldBe(2, "new IQueryable");
			}
		}

		// The IQueryable returned by a compiled query, enumerated again after AddInterceptor.
		[Test, QueryCacheTest]
		public void CompiledQueryableAfterAddInterceptor([IncludeDataSources(ProviderName.SQLiteMS)] string context)
		{
			Query.ClearCaches();

			using var setup = GetDataConnection(context, o => o.UseMappingSchema(_lambdaFilterSchema));
			using var t     = setup.CreateLocalTable(Enumerable.Range(1, 4).Select(i => new CacheItem { Id = i }));

			using var db = GetDataConnection(context, o => o.UseMappingSchema(_lambdaFilterSchema));

			var query = _compiledTable(db);

			query.ToArray().Length.ShouldBe(4);

			db.AddInterceptor(new LimitFilterInterceptor(3));

			using (Assert.EnterMultipleScope())
			{
				query.ToArray().Length.ShouldBe(3, "same IQueryable");
				_compiledTable(db).ToArray().Length.ShouldBe(3, "new compiled IQueryable");
			}
		}

		// Two contexts whose same-type interceptors rewrite differently, alternating on one compiled query: each keeps its
		// own built query instead of rebuilding on every switch.
		[Test, QueryCacheTest]
		public void CompiledQueryAlternatingContexts([IncludeDataSources(ProviderName.SQLiteMS)] string context)
		{
			Query.ClearCaches();

			using var setup = GetDataConnection(context, o => o.UseMappingSchema(_lambdaFilterSchema));
			using var t     = setup.CreateLocalTable(Enumerable.Range(1, 4).Select(i => new CacheItem { Id = i }));

			using var a = GetDataConnection(context, o => o.UseMappingSchema(_lambdaFilterSchema).UseInterceptor(new FilterInterceptor(QueryExpressionArgs.ExpressionKind.QueryFilter, Mode.Le2)));
			using var b = GetDataConnection(context, o => o.UseMappingSchema(_lambdaFilterSchema).UseInterceptor(new FilterInterceptor(QueryExpressionArgs.ExpressionKind.QueryFilter, Mode.Gt2)));

			var infoA = _compiledTable(a).GetCompiledQueryInfo();
			var infoB = _compiledTable(b).GetCompiledQueryInfo();

			infoA.ShouldNotBeSameAs(infoB);

			for (var i = 0; i < 5; i++)
			{
				var queryA = _compiledTable(a);
				var queryB = _compiledTable(b);

				using (Assert.EnterMultipleScope())
				{
					queryA.GetCompiledQueryInfo().ShouldBeSameAs(infoA, $"A, round {i}");
					queryB.GetCompiledQueryInfo().ShouldBeSameAs(infoB, $"B, round {i}");
					queryA.ToArray().Select(e => e.Id).OrderBy(id => id).ToArray().ShouldBe([1, 2], $"A, round {i}");
					queryB.ToArray().Select(e => e.Id).OrderBy(id => id).ToArray().ShouldBe([3, 4], $"B, round {i}");
				}
			}
		}
		// An IQueryable executed again after RemoveInterceptor, with the query cache disabled.
		[Test, QueryCacheTest]
		public void ReuseQueryableWithQueryCacheDisabled(
			[IncludeDataSources(ProviderName.SQLiteMS)] string context,
			[Values(Kind.Query, Kind.ExposedQuery)]     Kind   kind,
			[Values]                                    bool   viaDataContext)
		{
			var ms = _noFilterSchema;

			Query.ClearCaches();

			using var setup = GetDataConnection(context, o => o.UseMappingSchema(ms));
			using var table = setup.CreateLocalTable(Enumerable.Range(1, 4).Select(i => new CacheItem { Id = i }));

			using IDataContext db = viaDataContext
				? new DataContext(new DataOptions().UseConfiguration(context).UseMappingSchema(ms).UseDisableQueryCache(true))
				: GetDataConnection(context, o => o.UseMappingSchema(ms).UseDisableQueryCache(true));

			var interceptor = new FilterInterceptor(ToArgsKind(kind), Mode.Le2);

			db.AddInterceptor(interceptor);

			var query = db.GetTable<CacheItem>();

			query.ToArray().Length.ShouldBe(2);

			db.RemoveInterceptor(interceptor);

			query.ToArray().Length.ShouldBe(4, "after RemoveInterceptor");
		}

		// Returns its input until Active is set, then adds Where(e => e.Id <= 2) at its stage.
		sealed class ToggleInterceptor(QueryExpressionArgs.ExpressionKind kind) : IQueryExpressionInterceptor
		{
			readonly FilterInterceptor _filter = new(kind, Mode.Le2);

			public bool Active { get; set; }
			public int  Calls  { get; private set; }

			public Expression ProcessExpression(Expression expression, QueryExpressionArgs args)
			{
				Calls++;

				return Active ? _filter.ProcessExpression(expression, args) : expression;
			}
		}

		// An interceptor that did not rewrite the first execution may rewrite a later one: an IQueryable executed again
		// must still run it.
		[Test, QueryCacheTest]
		public void ReuseQueryableAfterInterceptorStartsRewriting(
			[IncludeDataSources(ProviderName.SQLiteMS)] string context,
			[Values(Kind.Query, Kind.ExposedQuery)]     Kind   kind,
			[Values]                                    bool   viaDataContext)
		{
			var ms = _noFilterSchema;

			Query.ClearCaches();

			using var setup = GetDataConnection(context, o => o.UseMappingSchema(ms));
			using var table = setup.CreateLocalTable(Enumerable.Range(1, 4).Select(i => new CacheItem { Id = i }));

			using IDataContext db = viaDataContext
				? new DataContext(new DataOptions().UseConfiguration(context).UseMappingSchema(ms))
				: GetDataConnection(context, o => o.UseMappingSchema(ms));

			var interceptor = new ToggleInterceptor(ToArgsKind(kind));

			db.AddInterceptor(interceptor);

			var query = db.GetTable<CacheItem>();

			query.ToArray().Length.ShouldBe(4);

			interceptor.Active = true;

			using (Assert.EnterMultipleScope())
			{
				query.ToArray().Length.ShouldBe(2, "same IQueryable");
				db.GetTable<CacheItem>().ToArray().Length.ShouldBe(2, "new IQueryable");
			}
		}

		// An IQueryable whose interceptors leave its tree unchanged keeps its query across executions.
		[Test, QueryCacheTest]
		public void ReuseQueryableKeepsQueryWhenInterceptorsDoNotRewrite([IncludeDataSources(ProviderName.SQLiteMS)] string context)
		{
			var ms = _noFilterSchema;

			Query.ClearCaches();

			using var setup = GetDataConnection(context, o => o.UseMappingSchema(ms));
			using var table = setup.CreateLocalTable(Enumerable.Range(1, 4).Select(i => new CacheItem { Id = i }));

			using var db = GetDataConnection(context, o => o.UseMappingSchema(ms));

			db.AddInterceptor(new ToggleInterceptor(QueryExpressionArgs.ExpressionKind.Query));

			var query = db.GetTable<CacheItem>();

			query.ToArray().Length.ShouldBe(4);

			var info = query.GetCompiledQueryInfo();

			query.ToArray().Length.ShouldBe(4);
			query.GetCompiledQueryInfo().ShouldBeSameAs(info);
		}
		static readonly Func<IDataContext, Type[], IQueryable<CacheItem>> _compiledIgnoreFilters =
			CompiledQuery.Compile((IDataContext db, Type[] types) => db.GetTable<CacheItem>().IgnoreFilters(types));

		static readonly MappingSchema _upToTwoFilterSchema = new FluentMappingBuilder(new MappingSchema())
			.Entity<CacheItem>().HasQueryFilter(e => e.Id <= 2)
			.Build()
			.MappingSchema;

		// A [SqlQueryDependent] argument selects the query by value; the caller may change it in the argument array
		// between creating the compiled IQueryable and enumerating it, or between two enumerations.
		[Test, QueryCacheTest]
		public void CompiledQueryableDependentArgumentChanged([IncludeDataSources(ProviderName.SQLiteMS)] string context, [Values] bool beforeFirstRun)
		{
			Query.ClearCaches();

			using var setup = GetDataConnection(context, o => o.UseMappingSchema(_upToTwoFilterSchema));
			using var t     = setup.CreateLocalTable(Enumerable.Range(1, 4).Select(i => new CacheItem { Id = i }));

			using var db = GetDataConnection(context, o => o.UseMappingSchema(_upToTwoFilterSchema));

			var types = new[] { typeof(CacheItem) };
			var query = _compiledIgnoreFilters(db, types);

			if (!beforeFirstRun)
				query.ToArray().Length.ShouldBe(4);

			types[0] = typeof(string);

			query.ToArray().Length.ShouldBe(2, "after the argument changed");
		}

		// Adds Where(e => e.Id <= 2) at its stage once after being armed; records every call.
		sealed class ArmOnceInterceptor(QueryExpressionArgs.ExpressionKind kind) : IQueryExpressionInterceptor
		{
			readonly FilterInterceptor _filter = new(kind, Mode.Le2);

			public bool                                          Armed { get; set; }
			public System.Collections.Generic.List<QueryExpressionArgs.ExpressionKind> Calls { get; } = [];

			public Expression ProcessExpression(Expression expression, QueryExpressionArgs args)
			{
				Calls.Add(args.Kind);

				if (!Armed || args.Kind != kind)
					return expression;

				Armed = false;

				return _filter.ProcessExpression(expression, args);
			}
		}

		// Each interceptor stage runs once per execution, and a rewrite it returns is used.
		[Test, QueryCacheTest]
		public void InterceptorStagesRunOncePerExecution(
			[IncludeDataSources(ProviderName.SQLiteMS)] string context,
			[Values(Kind.Query, Kind.ExposedQuery)]     Kind   kind,
			[Values]                                    bool   viaDataContext,
			[Values]                                    bool   sameQueryable)
		{
			var ms = _noFilterSchema;

			Query.ClearCaches();

			using var setup = GetDataConnection(context, o => o.UseMappingSchema(ms));
			using var table = setup.CreateLocalTable(Enumerable.Range(1, 4).Select(i => new CacheItem { Id = i }));

			using IDataContext db = viaDataContext
				? new DataContext(new DataOptions().UseConfiguration(context).UseMappingSchema(ms))
				: GetDataConnection(context, o => o.UseMappingSchema(ms));

			var interceptor = new ArmOnceInterceptor(ToArgsKind(kind));

			db.AddInterceptor(interceptor);

			var query = db.GetTable<CacheItem>();

			query.ToArray().Length.ShouldBe(4);

			interceptor.Armed = true;
			interceptor.Calls.Clear();

			var rows = (sameQueryable ? query : db.GetTable<CacheItem>()).ToArray().Length;

			using (Assert.EnterMultipleScope())
			{
				rows.ShouldBe(2);
				interceptor.Calls.Count(k => k == ToArgsKind(kind)).ShouldBe(1, string.Join(", ", interceptor.Calls));
			}
		}
	}
}
