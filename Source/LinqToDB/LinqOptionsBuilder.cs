using System;
using System.Linq.Expressions;

namespace LinqToDB
{
	using Data;
	using Linq;
	using Infrastructure;

	public class LinqOptionsBuilder : ILinqOptionsDataContextOptionsBuilderInfrastructure
	{
		public LinqOptionsBuilder(DataContextOptionsBuilder optionsBuilder)
		{
			OptionsBuilder = optionsBuilder;
		}

		public DataContextOptionsBuilder OptionsBuilder { get; }

		/// <summary>
		///     Sets an option by cloning the extension used to store the settings. This ensures the builder
		///     does not modify options that are already in use elsewhere.
		/// </summary>
		/// <param name="setAction"> An action to set the option. </param>
		/// <returns> The same builder instance so that multiple calls can be chained. </returns>
		protected virtual LinqOptionsBuilder WithOption(Func<LinqOptions, LinqOptions> setAction)
		{
			((IDataContextOptionsBuilderInfrastructure)OptionsBuilder).AddOrUpdateExtension(
				setAction(OptionsBuilder.Options.FindExtension<LinqOptions>() ?? Common.Configuration.Linq.Options));

			return this;
		}


		/// <summary>
		/// Controls how group data for LINQ queries ended with GroupBy will be loaded:
		/// - if <c>true</c> - group data will be loaded together with main query, resulting in 1 + N queries, where N - number of groups;
		/// - if <c>false</c> - group data will be loaded when you call enumerator for specific group <see cref="System.Linq.IGrouping{TKey, TElement}"/>.
		/// Default value: <c>false</c>.
		/// </summary>
		public virtual LinqOptionsBuilder WithPreloadGroups(bool preloadGroups) =>
			WithOption(o => o with { PreloadGroups = preloadGroups });

		/// <summary>
		/// Controls behavior of linq2db when there is no updateable fields in Update query:
		/// - if <c>true</c> - query not executed and Update operation returns 0 as number of affected records;
		/// - if <c>false</c> - <see cref="LinqException"/> will be thrown.
		/// Default value: <c>false</c>.
		/// </summary>
		public virtual LinqOptionsBuilder WithIgnoreEmptyUpdate(bool ignoreEmptyUpdate) =>
			WithOption(o => o with { IgnoreEmptyUpdate = ignoreEmptyUpdate });

		/// <summary>
		/// Enables generation of test class for each LINQ query, executed while this option is enabled.
		/// This option could be useful for issue reporting, when you need to provide reproducible case.
		/// Test file will be placed to <c>linq2db</c> subfolder of temp folder and exact file path will be logged
		/// to data connection tracing infrastructure.
		/// See <see cref="DataConnection.TraceSwitch"/> for more details.
		/// Default value: <c>false</c>.
		/// </summary>
		public virtual LinqOptionsBuilder WithGenerateExpressionTest(bool generateExpressionTest) =>
			WithOption(o => o with { GenerateExpressionTest = generateExpressionTest });

		/// <summary>
		/// Enables logging of generated mapping expression to data connection tracing infrastructure.
		/// See <see cref="DataConnection.TraceSwitch"/> for more details.
		/// Default value: <c>false</c>.
		/// </summary>
		public virtual LinqOptionsBuilder WithTraceMapperExpression(bool traceMapperExpression) =>
			WithOption(o => o with { TraceMapperExpression = traceMapperExpression });

		/// <summary>
		/// Controls behavior, when LINQ query chain contains multiple <see cref="System.Linq.Queryable.OrderBy{TSource, TKey}(System.Linq.IQueryable{TSource}, Expression{Func{TSource, TKey}})"/> or <see cref="System.Linq.Queryable.OrderByDescending{TSource, TKey}(System.Linq.IQueryable{TSource}, Expression{Func{TSource, TKey}})"/> calls:
		/// - if <c>true</c> - non-first OrderBy* call will be treated as ThenBy* call;
		/// - if <c>false</c> - OrderBy* call will discard sort specifications, added by previous OrderBy* and ThenBy* calls.
		/// Default value: <c>false</c>.
		/// </summary>
		public virtual LinqOptionsBuilder WithDoNotClearOrderBys(bool doNotClearOrderBys) =>
			WithOption(o => o with { DoNotClearOrderBys = doNotClearOrderBys });

		/// <summary>
		/// If enabled, linq2db will try to reduce number of generated SQL JOINs for LINQ query.
		/// Attempted optimizations:
		/// - removes duplicate joins by unique target table key;
		/// - removes self-joins by unique key;
		/// - removes left joins if joined table is not used in query.
		/// Default value: <c>true</c>.
		/// </summary>
		public virtual LinqOptionsBuilder WithOptimizeJoins(bool optimizeJoins) =>
			WithOption(o => o with { OptimizeJoins = optimizeJoins });

		/// <summary>
		/// If set to true nullable fields would be checked for IS NULL in Equal/NotEqual comparisons.
		/// This affects: Equal, NotEqual, Not Contains
		/// Default value: <c>true</c>.
		/// </summary>
		/// <example>
		/// <code>
		/// public class MyEntity
		/// {
		///     public int? Value;
		/// }
		///
		/// db.MyEntity.Where(e => e.Value != 10)
		///
		/// from e1 in db.MyEntity
		/// join e2 in db.MyEntity on e1.Value equals e2.Value
		/// select e1
		///
		/// var filter = new [] {1, 2, 3};
		/// db.MyEntity.Where(e => ! filter.Contains(e.Value))
		/// </code>
		///
		/// Would be converted to next queries:
		/// <code>
		/// SELECT Value FROM MyEntity WHERE Value IS NULL OR Value != 10
		///
		/// SELECT e1.Value
		/// FROM MyEntity e1
		/// INNER JOIN MyEntity e2 ON e1.Value = e2.Value OR (e1.Value IS NULL AND e2.Value IS NULL)
		///
		/// SELECT Value FROM MyEntity WHERE Value IS NULL OR NOT Value IN (1, 2, 3)
		/// </code>
		/// </example>
		public virtual LinqOptionsBuilder WithCompareNullsAsValues(bool compareNullsAsValues) =>
			WithOption(o => o with { CompareNullsAsValues = compareNullsAsValues });

		/// <summary>
		/// Controls behavior of LINQ query, which ends with GroupBy call.
		/// - if <c>true</c> - <seealso cref="LinqToDBException"/> will be thrown for such queries;
		/// - if <c>false</c> - behavior is controlled by <see cref="WithPreloadGroups"/> option.
		/// Default value: <c>true</c>.
		/// </summary>
		/// <remarks>
		/// <a href="https://github.com/linq2db/linq2db/issues/365">More details</a>.
		/// </remarks>
		public virtual LinqOptionsBuilder WithGuardGrouping(bool guardGrouping) =>
			WithOption(o => o with { GuardGrouping = guardGrouping });

		/// <summary>
		/// Used to disable LINQ expressions caching for queries.
		/// This cache reduces time, required for query parsing but have several side-effects:
		/// <para />
		/// - cached LINQ expressions could contain references to external objects as parameters, which could lead to memory leaks if those objects are not used anymore by other code
		/// <para />
		/// - cache access synchronization could lead to bigger latencies than it saves.
		/// <para />
		/// Default value: <c>false</c>.
		/// <para />
		/// It is not recommended to enable this option as it could lead to severe slowdown. Better approach will be
		/// to call <see cref="Query{T}.ClearCache"/> method to cleanup cache after queries, that produce severe memory leaks you need to fix.
		/// <para />
		/// <a href="https://github.com/linq2db/linq2db/issues/256">More details</a>.
		/// </summary>
		public virtual LinqOptionsBuilder WithDisableQueryCache(bool disableQueryCache) =>
			WithOption(o => o with { DisableQueryCache = disableQueryCache });

		/// <summary>
		/// Specifies timeout when query will be evicted from cache since last execution of query.
		/// Default value is 1 hour.
		/// </summary>
		public virtual LinqOptionsBuilder WithCacheSlidingExpiration(TimeSpan cacheSlidingExpiration) =>
			WithOption(o => o with { CacheSlidingExpiration = cacheSlidingExpiration });

		/// <summary>
		/// Used to generate CROSS APPLY or OUTER APPLY if possible.
		/// Default value: <c>true</c>.
		/// </summary>
		public virtual LinqOptionsBuilder WithPreferApply(bool preferApply) =>
			WithOption(o => o with { PreferApply = preferApply });

		/// <summary>
		/// Allows SQL generation to automatically transform
		/// <code>SELECT DISTINCT value FROM Table ORDER BY date</code>
		/// Into GROUP BY equivalent if syntax is not supported
		/// Default value: <c>true</c>.
		/// </summary>
		public virtual LinqOptionsBuilder WithKeepDistinctOrdered(bool keepDistinctOrdered) =>
			WithOption(o => o with { KeepDistinctOrdered = keepDistinctOrdered });

		/// <summary>
		/// Enables Take/Skip parameterization.
		/// Default value: <c>true</c>.
		/// </summary>
		public virtual LinqOptionsBuilder WithParameterizeTakeSkip(bool parameterizeTakeSkip) =>
			WithOption(o => o with { ParameterizeTakeSkip = parameterizeTakeSkip });

		/// <summary>
		/// If <c>true</c>, auto support for fluent mapping is ON,
		/// which means that you do not need to create additional MappingSchema object to define FluentMapping.
		/// You can use <c>context.MappingSchema.GetFluentMappingBuilder()</c>.
		/// </summary>
		public virtual LinqOptionsBuilder WithEnableAutoFluentMapping(bool enableAutoFluentMapping) =>
			WithOption(o => o with { EnableAutoFluentMapping = enableAutoFluentMapping });
	}
}
