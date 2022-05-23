using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Linq.Expressions;

using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace LinqToDB
{
	using Data;
	using Data.RetryPolicy;
	using DataProvider;
	using Infrastructure;
	using Interceptors;
	using Mapping;

	/// <summary>
	/// Set of extensions for <see cref="DataOptions"/>.
	/// </summary>
	[PublicAPI]
	public static partial class DataOptionsExtensions
	{
		#region LinqOptions

		/// <summary>
		/// Controls how group data for LINQ queries ended with GroupBy will be loaded:
		/// - if <c>true</c> - group data will be loaded together with main query, resulting in 1 + N queries, where N - number of groups;
		/// - if <c>false</c> - group data will be loaded when you call enumerator for specific group <see cref="System.Linq.IGrouping{TKey, TElement}"/>.
		/// Default value: <c>false</c>.
		/// </summary>
		public static DataOptions UsePreloadGroups(this DataOptions options, bool preloadGroups)
		{
			return options.WithOptions<LinqOptions>(o => o with { PreloadGroups = preloadGroups });
		}

		/// <summary>
		/// Controls behavior of linq2db when there is no updateable fields in Update query:
		/// - if <c>true</c> - query not executed and Update operation returns 0 as number of affected records;
		/// - if <c>false</c> - <see cref="LinqToDBException"/> will be thrown.
		/// Default value: <c>false</c>.
		/// </summary>
		public static DataOptions UseIgnoreEmptyUpdate(DataOptions options, bool ignoreEmptyUpdate)
		{
			return options.WithOptions<LinqOptions>(o => o with { IgnoreEmptyUpdate = ignoreEmptyUpdate });
		}

		/// <summary>
		/// Enables generation of test class for each LINQ query, executed while this option is enabled.
		/// This option could be useful for issue reporting, when you need to provide reproducible case.
		/// Test file will be placed to <c>linq2db</c> subfolder of temp folder and exact file path will be logged
		/// to data connection tracing infrastructure.
		/// See <see cref="DataConnection.TraceSwitch"/> for more details.
		/// Default value: <c>false</c>.
		/// </summary>
		public static DataOptions UseGenerateExpressionTest(this DataOptions options, bool generateExpressionTest)
		{
			return options.WithOptions<LinqOptions>(o => o with { GenerateExpressionTest = generateExpressionTest });
		}

		/// <summary>
		/// Enables logging of generated mapping expression to data connection tracing infrastructure.
		/// See <see cref="DataConnection.TraceSwitch"/> for more details.
		/// Default value: <c>false</c>.
		/// </summary>
		public static DataOptions UseTraceMapperExpression(this DataOptions options, bool traceMapperExpression)
		{
			return options.WithOptions<LinqOptions>(o => o with { TraceMapperExpression = traceMapperExpression });
		}

		/// <summary>
		/// Controls behavior, when LINQ query chain contains multiple
		/// <see cref="System.Linq.Queryable.OrderBy          {TSource,TKey}(System.Linq.IQueryable{TSource}, Expression{Func{TSource, TKey}})"/> or
		/// <see cref="System.Linq.Queryable.OrderByDescending{TSource,TKey}(System.Linq.IQueryable{TSource}, Expression{Func{TSource, TKey}})"/> calls:
		/// - if <c>true</c> - non-first OrderBy* call will be treated as ThenBy* call;
		/// - if <c>false</c> - OrderBy* call will discard sort specifications, added by previous OrderBy* and ThenBy* calls.
		/// Default value: <c>false</c>.
		/// </summary>
		public static DataOptions UseDoNotClearOrderBys(this DataOptions options, bool doNotClearOrderBys)
		{
			return options.WithOptions<LinqOptions>(o => o with { DoNotClearOrderBys = doNotClearOrderBys });
		}

		/// <summary>
		/// If enabled, linq2db will try to reduce number of generated SQL JOINs for LINQ query.
		/// Attempted optimizations:
		/// - removes duplicate joins by unique target table key;
		/// - removes self-joins by unique key;
		/// - removes left joins if joined table is not used in query.
		/// Default value: <c>true</c>.
		/// </summary>
		public static DataOptions UseOptimizeJoins(this DataOptions options, bool optimizeJoins)
		{
			return options.WithOptions<LinqOptions>(o => o with { OptimizeJoins = optimizeJoins });
		}

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
		public static DataOptions UseCompareNullsAsValues(this DataOptions options, bool compareNullsAsValues)
		{
			return options.WithOptions<LinqOptions>(o => o with { CompareNullsAsValues = compareNullsAsValues });
		}

		/// <summary>
		/// Controls behavior of LINQ query, which ends with GroupBy call.
		/// - if <c>true</c> - <seealso cref="LinqToDBException"/> will be thrown for such queries;
		/// - if <c>false</c> - behavior is controlled by <see cref="UsePreloadGroups"/> option.
		/// Default value: <c>true</c>.
		/// </summary>
		/// <remarks>
		/// <a href="https://github.com/linq2db/linq2db/issues/365">More details</a>.
		/// </remarks>
		public static DataOptions UseGuardGrouping(this DataOptions options, bool guardGrouping)
		{
			return options.WithOptions<LinqOptions>(o => o with { GuardGrouping = guardGrouping });
		}

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
		/// to call <see cref="Linq.Query{T}.ClearCache"/> method to cleanup cache after queries, that produce severe memory leaks you need to fix.
		/// <para />
		/// <a href="https://github.com/linq2db/linq2db/issues/256">More details</a>.
		/// </summary>
		public static DataOptions UseDisableQueryCache(this DataOptions options, bool disableQueryCache)
		{
			return options.WithOptions<LinqOptions>(o => o with { DisableQueryCache = disableQueryCache });
		}

		/// <summary>
		/// Specifies timeout when query will be evicted from cache since last execution of query.
		/// Default value is 1 hour.
		/// </summary>
		public static DataOptions UseCacheSlidingExpiration(this DataOptions options, TimeSpan cacheSlidingExpiration)
		{
			return options.WithOptions<LinqOptions>(o => o with { CacheSlidingExpiration = cacheSlidingExpiration });
		}

		/// <summary>
		/// Used to generate CROSS APPLY or OUTER APPLY if possible.
		/// Default value: <c>true</c>.
		/// </summary>
		public static DataOptions UsePreferApply(this DataOptions options, bool preferApply)
		{
			return options.WithOptions<LinqOptions>(o => o with { PreferApply = preferApply });
		}

		/// <summary>
		/// Allows SQL generation to automatically transform
		/// <code>SELECT DISTINCT value FROM Table ORDER BY date</code>
		/// Into GROUP BY equivalent if syntax is not supported
		/// Default value: <c>true</c>.
		/// </summary>
		public static DataOptions UseKeepDistinctOrdered(this DataOptions options, bool keepDistinctOrdered)
		{
			return options.WithOptions<LinqOptions>(o => o with { KeepDistinctOrdered = keepDistinctOrdered });
		}

		/// <summary>
		/// Enables Take/Skip parameterization.
		/// Default value: <c>true</c>.
		/// </summary>
		public static DataOptions UseParameterizeTakeSkip(this DataOptions options, bool parameterizeTakeSkip)
		{
			return options.WithOptions<LinqOptions>(o => o with { ParameterizeTakeSkip = parameterizeTakeSkip });
		}

		/// <summary>
		/// If <c>true</c>, auto support for fluent mapping is ON,
		/// which means that you do not need to create additional MappingSchema object to define FluentMapping.
		/// You can use <c>context.MappingSchema.GetFluentMappingBuilder()</c>.
		/// </summary>
		public static DataOptions UseEnableAutoFluentMapping(this DataOptions options, bool enableAutoFluentMapping)
		{
			return options.WithOptions<LinqOptions>(o => o with { EnableAutoFluentMapping = enableAutoFluentMapping });
		}

		#endregion

		#region ConnectionOptions

		public static DataOptions UseConnectionString(this DataOptions options, string providerName, string connectionString)
		{
			return options.WithOptions<ConnectionOptions>(o => o with { ProviderName = providerName, ConnectionString = connectionString });
		}

		public static DataOptions UseConnectionString(this DataOptions options, IDataProvider dataProvider, string connectionString)
		{
			return options.WithOptions<ConnectionOptions>(o => o with { DataProvider = dataProvider, ConnectionString = connectionString });
		}

		public static DataOptions UseConnectionString(this DataOptions options, string connectionString)
		{
			return options.WithOptions<ConnectionOptions>(o => o with { ConnectionString = connectionString });
		}

		public static DataOptions UseConfigurationString(this DataOptions options, string? configurationString)
		{
			return options.WithOptions<ConnectionOptions>(o => o with { ConfigurationString = configurationString });
		}

		public static DataOptions UseConfigurationString(this DataOptions options, string configurationString, MappingSchema mappingSchema)
		{
			return options
				.WithOptions<ConnectionOptions> (o => o with { ConfigurationString = configurationString })
				.WithOptions<DataContextOptions>(o => o with { MappingSchema       = mappingSchema       });
		}

		public static DataOptions UseConnection(this DataOptions options, DbConnection connection)
		{
			return options.WithOptions<ConnectionOptions>(o => o with { DbConnection = connection });
		}

		public static DataOptions UseConnection(this DataOptions options, IDataProvider dataProvider, DbConnection connection)
		{
			return options.WithOptions<ConnectionOptions>(o => o with { DataProvider = dataProvider, DbConnection = connection });
		}

		public static DataOptions UseConnection(this DataOptions options, IDataProvider dataProvider, DbConnection connection, bool disposeConnection)
		{
			return options.WithOptions<ConnectionOptions>(o => o with { DataProvider = dataProvider, DbConnection = connection, DisposeConnection = disposeConnection });
		}

		public static DataOptions UseProvider(this DataOptions options, string providerName)
		{
			return options.WithOptions<ConnectionOptions>(o => o with { ProviderName = providerName });
		}

		public static DataOptions UseDataProvider(this DataOptions options, IDataProvider dataProvider)
		{
			return options.WithOptions<ConnectionOptions>(o => o with { DataProvider = dataProvider });
		}

		public static DataOptions UseMappingSchema(this DataOptions options, MappingSchema mappingSchema)
		{
			return options.WithOptions<DataContextOptions>(o => o with { MappingSchema = mappingSchema });
		}

		public static DataOptions UseConnectionFactory(this DataOptions options, Func<DbConnection> connectionFactory)
		{
			return options.WithOptions<ConnectionOptions>(o => o with { ConnectionFactory = connectionFactory });
		}

		public static DataOptions UseConnectionFactory(this DataOptions options, IDataProvider dataProvider, Func<DbConnection> connectionFactory)
		{
			return options.WithOptions<ConnectionOptions>(o => o with { DataProvider = dataProvider, ConnectionFactory = connectionFactory });
		}

		public static DataOptions UseTransaction(this DataOptions options, IDataProvider dataProvider, DbTransaction transaction)
		{
			return options.WithOptions<ConnectionOptions>(o => o with { DataProvider = dataProvider, DbTransaction = transaction });
		}

		/// <summary>
		/// <para>
		/// Adds <see cref="IInterceptor" /> instances to those registered on the context.
		/// </para>
		/// <para>
		/// Interceptors can be used to view, change, or suppress operations taken by linq2db.
		/// See the specific implementations of <see cref="IInterceptor" /> for details. For example, 'ICommandInterceptor'.
		/// </para>
		/// <para>
		/// A single interceptor instance can implement multiple different interceptor interfaces. I will be registered as
		/// an interceptor for all interfaces that it implements.
		/// </para>
		/// <para>
		/// Extensions can also register multiple <see cref="IInterceptor" />s in the internal service provider.
		/// If both injected and application interceptors are found, then the injected interceptors are run in the
		/// order that they are resolved from the service provider, and then the application interceptors are run
		/// in the order that they were added to the context.
		/// </para>
		/// <para>
		/// Calling this method multiple times will result in all interceptors in every call being added to the context.
		/// Interceptors added in a previous call are not overridden by interceptors added in a later call.
		/// </para>
		/// </summary>
		/// <param name="interceptors"> The interceptors to add. </param>
		/// <returns> The same builder instance so that multiple calls can be chained. </returns>
		public static DataOptions UseInterceptors(this DataOptions options, IEnumerable<IInterceptor> interceptors)
		{
			return options.WithOptions<DataContextOptions>(o =>
			{
				var list = new List<IInterceptor>();

				if (o.Interceptors != null)
					list.AddRange(o.Interceptors);
				list.AddRange(interceptors);

				return o with { Interceptors = list };
			});
		}

		/// <summary>
		/// <para>
		/// Adds <see cref="IInterceptor" /> instances to those registered on the context.
		/// </para>
		/// <para>
		/// Interceptors can be used to view, change, or suppress operations taken by linq2db.
		/// See the specific implementations of <see cref="IInterceptor" /> for details. For example, 'ICommandInterceptor'.
		/// </para>
		/// <para>
		/// Extensions can also register multiple <see cref="IInterceptor" />s in the internal service provider.
		/// If both injected and application interceptors are found, then the injected interceptors are run in the
		/// order that they are resolved from the service provider, and then the application interceptors are run
		/// in the order that they were added to the context.
		/// </para>
		/// <para>
		/// Calling this method multiple times will result in all interceptors in every call being added to the context.
		/// Interceptors added in a previous call are not overridden by interceptors added in a later call.
		/// </para>
		/// </summary>
		/// <param name="interceptors"> The interceptors to add. </param>
		/// <returns> The same builder instance so that multiple calls can be chained. </returns>
		public static DataOptions UseInterceptors(this DataOptions options, params IInterceptor[] interceptors)
		{
			return options.WithOptions<DataContextOptions>(o =>
			{
				var list = new List<IInterceptor>();

				if (o.Interceptors != null)
					list.AddRange(o.Interceptors);
				list.AddRange(interceptors);

				return o with { Interceptors = list };
			});
		}

		/// <summary>
		/// <para>
		/// Adds <see cref="IInterceptor" /> instance to those registered on the context.
		/// </para>
		/// <para>
		/// Interceptors can be used to view, change, or suppress operations taken by linq2db.
		/// See the specific implementations of <see cref="IInterceptor" /> for details. For example, 'ICommandInterceptor'.
		/// </para>
		/// <para>
		/// Extensions can also register multiple <see cref="IInterceptor" />s in the internal service provider.
		/// If both injected and application interceptors are found, then the injected interceptors are run in the
		/// order that they are resolved from the service provider, and then the application interceptors are run
		/// in the order that they were added to the context.
		/// </para>
		/// <para>
		/// Calling this method multiple times will result in all interceptors in every call being added to the context.
		/// Interceptors added in a previous call are not overridden by interceptors added in a later call.
		/// </para>
		/// </summary>
		/// <param name="interceptor"> The interceptor to add. </param>
		/// <returns> The same builder instance so that multiple calls can be chained.</returns>
		public static DataOptions UseInterceptor(this DataOptions options, IInterceptor interceptor)
		{
			return options.WithOptions<DataContextOptions>(o =>
			{
				var list = new List<IInterceptor>();

				if (o.Interceptors != null)
					list.AddRange(o.Interceptors);
				list.Add(interceptor);

				return o with { Interceptors = list };
			});
		}

		#endregion

		#region DataTraceOptions

		/// <summary>
		/// Configure the database to use specified trace level.
		/// </summary>
		/// <returns>The builder instance so calls can be chained.</returns>
		public static DataOptions UseTraceLevel(this DataOptions options, TraceLevel traceLevel)
		{
			return options.WithOptions<QueryTraceOptions>(o => o with { TraceLevel = traceLevel });
		}

		/// <summary>
		/// Configure the database to use the specified callback for logging or tracing.
		/// </summary>
		/// <param name="onTrace">Callback, may not be called depending on the trace level.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public static DataOptions UseTracing(this DataOptions options, Action<TraceInfo> onTrace)
		{
			return options.WithOptions<QueryTraceOptions>(o => o with { OnTrace = onTrace });
		}

		/// <summary>
		/// Configure the database to use the specified trace level and callback for logging or tracing.
		/// </summary>
		/// <param name="traceLevel">Trace level to use.</param>
		/// <param name="onTrace">Callback, may not be called depending on the trace level.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public static DataOptions UseTracing(this DataOptions options, TraceLevel traceLevel, Action<TraceInfo> onTrace)
		{
			return options.WithOptions<QueryTraceOptions>(o => o with { OnTrace = onTrace, TraceLevel = traceLevel });
		}

		/// <summary>
		/// Configure the database to use the specified a string trace callback.
		/// </summary>
		/// <param name="write">Callback, may not be called depending on the trace level.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public static DataOptions UseTraceWith(this DataOptions options, Action<string?,string?,TraceLevel> write)
		{
			return options.WithOptions<QueryTraceOptions>(o => o with { WriteTrace = write });
		}

		#endregion

		#region RetryPolicyOptions

		/// <summary>
		/// Uses retry policy
		/// </summary>
		public static DataOptions UseRetryPolicy(this DataOptions options, IRetryPolicy retryPolicy)
		{
			return options.WithOptions<RetryPolicyOptions>(o => o with { RetryPolicy = retryPolicy });
		}

		/// <summary>
		/// Uses default retry policy factory
		/// </summary>
		public static DataOptions UseDefaultRetryPolicyFactory(this DataOptions options)
		{
			return options.WithOptions<RetryPolicyOptions>(o => o with { Factory = DefaultRetryPolicyFactory.GetRetryPolicy });
		}

		/// <summary>
		/// Retry policy factory method, used to create retry policy for new <see cref="DataConnection"/> instance.
		/// If factory method is not set, retry policy is not used.
		/// Not set by default.
		/// </summary>
		public static DataOptions UseFactory(this DataOptions options, Func<DataConnection,IRetryPolicy?>? factory)
		{
			return options.WithOptions<RetryPolicyOptions>(o => o with { Factory = factory });
		}

		/// <summary>
		/// The number of retry attempts.
		/// Default value: <c>5</c>.
		/// </summary>
		public static DataOptions UseMaxRetryCount(this DataOptions options, int maxRetryCount)
		{
			return options.WithOptions<RetryPolicyOptions>(o => o with { MaxRetryCount = maxRetryCount });
		}

		/// <summary>
		/// The maximum time delay between retries, must be nonnegative.
		/// Default value: 30 seconds.
		/// </summary>
		public static DataOptions UseMaxDelay(this DataOptions options, TimeSpan defaultMaxDelay)
		{
			return options.WithOptions<RetryPolicyOptions>(o => o with { MaxDelay = defaultMaxDelay });
		}

		/// <summary>
		/// The maximum random factor, must not be lesser than 1.
		/// Default value: <c>1.1</c>.
		/// </summary>
		public static DataOptions UseRandomFactor(this DataOptions options, double randomFactor)
		{
			return options.WithOptions<RetryPolicyOptions>(o => o with { RandomFactor = randomFactor });
		}

		/// <summary>
		/// The base for the exponential function used to compute the delay between retries, must be positive.
		/// Default value: <c>2</c>.
		/// </summary>
		public static DataOptions UseExponentialBase(this DataOptions options, double exponentialBase)
		{
			return options.WithOptions<RetryPolicyOptions>(o => o with { ExponentialBase = exponentialBase });
		}

		/// <summary>
		/// The coefficient for the exponential function used to compute the delay between retries, must be nonnegative.
		/// Default value: 1 second.
		/// </summary>
		public static DataOptions UseCoefficient(this DataOptions options, TimeSpan coefficient)
		{
			return options.WithOptions<RetryPolicyOptions>(o => o with { Coefficient = coefficient });
		}

		#endregion
	}
}
