﻿using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace LinqToDB
{
	using Data;
	using Data.RetryPolicy;
	using DataProvider;
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
		public static LinqOptions WithPreloadGroups(this LinqOptions options, bool preloadGroups)
		{
			return options with { PreloadGroups = preloadGroups };
		}

		/// <summary>
		/// Controls behavior of linq2db when there is no updateable fields in Update query:
		/// - if <c>true</c> - query not executed and Update operation returns 0 as number of affected records;
		/// - if <c>false</c> - <see cref="LinqToDBException"/> will be thrown.
		/// Default value: <c>false</c>.
		/// </summary>
		public static LinqOptions WithIgnoreEmptyUpdate(LinqOptions options, bool ignoreEmptyUpdate)
		{
			return options with { IgnoreEmptyUpdate = ignoreEmptyUpdate };
		}

		/// <summary>
		/// Enables generation of test class for each LINQ query, executed while this option is enabled.
		/// This option could be useful for issue reporting, when you need to provide reproducible case.
		/// Test file will be placed to <c>linq2db</c> subfolder of temp folder and exact file path will be logged
		/// to data connection tracing infrastructure.
		/// See <see cref="DataConnection.TraceSwitch"/> for more details.
		/// Default value: <c>false</c>.
		/// </summary>
		public static LinqOptions WithGenerateExpressionTest(this LinqOptions options, bool generateExpressionTest)
		{
			return options with { GenerateExpressionTest = generateExpressionTest };
		}

		/// <summary>
		/// Enables logging of generated mapping expression to data connection tracing infrastructure.
		/// See <see cref="DataConnection.TraceSwitch"/> for more details.
		/// Default value: <c>false</c>.
		/// </summary>
		public static LinqOptions WithTraceMapperExpression(this LinqOptions options, bool traceMapperExpression)
		{
			return options with { TraceMapperExpression = traceMapperExpression };
		}

		/// <summary>
		/// Controls behavior, when LINQ query chain contains multiple
		/// <see cref="System.Linq.Queryable.OrderBy          {TSource,TKey}(System.Linq.IQueryable{TSource}, Expression{Func{TSource, TKey}})"/> or
		/// <see cref="System.Linq.Queryable.OrderByDescending{TSource,TKey}(System.Linq.IQueryable{TSource}, Expression{Func{TSource, TKey}})"/> calls:
		/// - if <c>true</c> - non-first OrderBy* call will be treated as ThenBy* call;
		/// - if <c>false</c> - OrderBy* call will discard sort specifications, added by previous OrderBy* and ThenBy* calls.
		/// Default value: <c>false</c>.
		/// </summary>
		public static LinqOptions WithDoNotClearOrderBys(this LinqOptions options, bool doNotClearOrderBys)
		{
			return options with { DoNotClearOrderBys = doNotClearOrderBys };
		}

		/// <summary>
		/// If enabled, linq2db will try to reduce number of generated SQL JOINs for LINQ query.
		/// Attempted optimizations:
		/// - removes duplicate joins by unique target table key;
		/// - removes self-joins by unique key;
		/// - removes left joins if joined table is not used in query.
		/// Default value: <c>true</c>.
		/// </summary>
		public static LinqOptions WithOptimizeJoins(this LinqOptions options, bool optimizeJoins)
		{
			return options with { OptimizeJoins = optimizeJoins };
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
		public static LinqOptions WithCompareNullsAsValues(this LinqOptions options, bool compareNullsAsValues)
		{
			return options with { CompareNullsAsValues = compareNullsAsValues };
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
		public static LinqOptions WithGuardGrouping(this LinqOptions options, bool guardGrouping)
		{
			return options with { GuardGrouping = guardGrouping };
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
		public static LinqOptions WithDisableQueryCache(this LinqOptions options, bool disableQueryCache)
		{
			return options with { DisableQueryCache = disableQueryCache };
		}

		/// <summary>
		/// Specifies timeout when query will be evicted from cache since last execution of query.
		/// Default value is 1 hour.
		/// </summary>
		public static LinqOptions WithCacheSlidingExpiration(this LinqOptions options, TimeSpan? cacheSlidingExpiration)
		{
			return options with { CacheSlidingExpiration = cacheSlidingExpiration };
		}

		/// <summary>
		/// Used to generate CROSS APPLY or OUTER APPLY if possible.
		/// Default value: <c>true</c>.
		/// </summary>
		public static LinqOptions WithPreferApply(this LinqOptions options, bool preferApply)
		{
			return options with { PreferApply = preferApply };
		}

		/// <summary>
		/// Allows SQL generation to automatically transform
		/// <code>SELECT DISTINCT value FROM Table ORDER BY date</code>
		/// Into GROUP BY equivalent if syntax is not supported
		/// Default value: <c>true</c>.
		/// </summary>
		public static LinqOptions WithKeepDistinctOrdered(this LinqOptions options, bool keepDistinctOrdered)
		{
			return options with { KeepDistinctOrdered = keepDistinctOrdered };
		}

		/// <summary>
		/// Enables Take/Skip parameterization.
		/// Default value: <c>true</c>.
		/// </summary>
		public static LinqOptions WithParameterizeTakeSkip(this LinqOptions options, bool parameterizeTakeSkip)
		{
			return options with { ParameterizeTakeSkip = parameterizeTakeSkip };
		}

		/// <summary>
		/// If <c>true</c>, user could add new mappings to context mapping schems (<see cref="IDataContext.MappingSchema"/>).
		/// Otherwise <see cref="LinqToDBException"/> will be generated on locked mapping schema edit attempt.
		/// It is not recommended to enable this option as it has performance implications.
		/// Proper approach is to create single <see cref="MappingSchema"/> instance once, configure mappings for it and use this <see cref="MappingSchema"/> instance for all context instances.
		/// </summary>
		public static LinqOptions WithEnableContextSchemaEdit(this LinqOptions options, bool enableContextSchemaEdit)
		{
			return options with { EnableContextSchemaEdit = enableContextSchemaEdit };
		}

		#endregion

		#region DataOptions.LinqOptions

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
		public static DataOptions UseCacheSlidingExpiration(this DataOptions options, TimeSpan? cacheSlidingExpiration)
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
		/// If <c>true</c>, user could add new mappings to context mapping schems (<see cref="IDataContext.MappingSchema"/>).
		/// Otherwise <see cref="LinqToDBException"/> will be generated on locked mapping schema edit attempt.
		/// It is not recommended to enable this option as it has performance implications.
		/// Proper approach is to create single <see cref="MappingSchema"/> instance once, configure mappings for it and use this <see cref="MappingSchema"/> instance for all context instances.
		/// </summary>
		public static DataOptions UseEnableContextSchemaEdit(this DataOptions options, bool enableContextSchemaEdit)
		{
			return options.WithOptions<LinqOptions>(o => o with { EnableContextSchemaEdit = enableContextSchemaEdit });
		}

		#endregion

		#region ConnectionOptions

		/// <summary>
		/// Sets ConfigurationString option.
		/// </summary>
		public static ConnectionOptions WithConfigurationString(this ConnectionOptions options, string? configurationString)
		{
			return options with { ConfigurationString = configurationString };
		}

		/// <summary>
		/// Sets ConnectionString option.
		/// </summary>
		public static ConnectionOptions WithConnectionString(this ConnectionOptions options, string? connectionString)
		{
			return options with { ConnectionString = connectionString };
		}

		/// <summary>
		/// Sets DataProvider option.
		/// </summary>
		public static ConnectionOptions WithDataProvider(this ConnectionOptions options, IDataProvider? dataProvider)
		{
			return options with { DataProvider = dataProvider };
		}

		/// <summary>
		/// Sets ProviderName option.
		/// </summary>
		public static ConnectionOptions WithProviderName(this ConnectionOptions options, string providerName)
		{
			return options with { ProviderName = providerName };
		}

		/// <summary>
		/// Sets MappingSchema option.
		/// </summary>
		public static ConnectionOptions WithMappingSchema(this ConnectionOptions options, MappingSchema mappingSchema)
		{
			return options with { MappingSchema = mappingSchema };
		}

		/// <summary>
		/// Sets DbConnection option.
		/// </summary>
		public static ConnectionOptions WithDbConnection(this ConnectionOptions options, DbConnection? connection)
		{
			return options with { DbConnection = connection };
		}

		/// <summary>
		/// Sets DbTransaction option.
		/// </summary>
		public static ConnectionOptions WithDbTransaction(this ConnectionOptions options, DbTransaction transaction)
		{
			return options with { DbTransaction = transaction };
		}

		/// <summary>
		/// Sets DisposeConnection option.
		/// </summary>
		public static ConnectionOptions WithDisposeConnection(this ConnectionOptions options, bool disposeConnection)
		{
			return options with { DisposeConnection = disposeConnection };
		}

		/// <summary>
		/// Sets ConnectionFactory option.
		/// </summary>
		public static ConnectionOptions WithConnectionFactory(this ConnectionOptions options, Func<DataOptions, DbConnection> connectionFactory)
		{
			return options with { ConnectionFactory = connectionFactory };
		}

		/// <summary>
		/// Sets DataProviderFactory option.
		/// </summary>
		public static ConnectionOptions WithDataProviderFactory(this ConnectionOptions options, Func<ConnectionOptions, IDataProvider> dataProviderFactory)
		{
			return options with { DataProviderFactory = dataProviderFactory };
		}

		/// <summary>
		/// Sets custom actions, executed before connection opened.
		/// </summary>
		/// <param name="afterConnectionOpening">
		/// Action, executed before database connection opened.
		/// Accepts connection instance as parameter.
		/// </param>
		/// <param name="afterConnectionOpeningAsync">
		/// Action, executed after database connection opened from async execution path.
		/// Accepts connection instance as parameter.
		/// If this option is not set, <paramref name="afterConnectionOpening"/> synchronous action called.
		/// Use this option only if you need to perform async work from action, otherwise <paramref name="afterConnectionOpening"/> is sufficient.
		/// </param>
		public static ConnectionOptions WithBeforeConnectionOpened(
			this ConnectionOptions                       options,
			Action<DbConnection>                         afterConnectionOpening,
			Func<DbConnection, CancellationToken, Task>? afterConnectionOpeningAsync = null)
		{
			return options with { ConnectionInterceptor = new(afterConnectionOpening, afterConnectionOpeningAsync, options.ConnectionInterceptor?.OnConnectionOpened, options.ConnectionInterceptor?.OnConnectionOpenedAsync) };
		}

		/// <summary>
		/// Sets custom actions, executed after connection opened.
		/// </summary>
		/// <param name="afterConnectionOpened">
		/// Action, executed for connection instance after <see cref="DbConnection.Open"/> call.
		/// Also called after <see cref="DbConnection.OpenAsync(CancellationToken)"/> call if <paramref name="afterConnectionOpenedAsync"/> action is not provided.
		/// Accepts connection instance as parameter.
		/// </param>
		/// <param name="afterConnectionOpenedAsync">
		/// Action, executed for connection instance from async execution path after <see cref="DbConnection.OpenAsync(CancellationToken)"/> call.
		/// Accepts connection instance as parameter.
		/// If this option is not set, <paramref name="afterConnectionOpened"/> synchronous action called.
		/// Use this option only if you need to perform async work from action, otherwise <paramref name="afterConnectionOpened"/> is sufficient.
		/// </param>
		public static ConnectionOptions WithAfterConnectionOpened(
			this ConnectionOptions                       options,
			Action<DbConnection>                         afterConnectionOpened,
			Func<DbConnection, CancellationToken, Task>? afterConnectionOpenedAsync = null)
		{
			return options with { ConnectionInterceptor = new (options.ConnectionInterceptor?.OnConnectionOpening, options.ConnectionInterceptor?.OnConnectionOpeningAsync, afterConnectionOpened, afterConnectionOpenedAsync) };
		}

		#endregion

		#region DataOptions.ConnectionOptions

		/// <summary>
		/// Defines provider name and connection sting to use with DataOptions.
		/// </summary>
		public static DataOptions UseConnectionString(this DataOptions options, string providerName, string connectionString)
		{
			return options.WithOptions<ConnectionOptions>(o => o with { ProviderName = providerName, ConnectionString = connectionString });
		}

		/// <summary>
		/// Defines data provider and connection sting to use with DataOptions.
		/// </summary>
		public static DataOptions UseConnectionString(this DataOptions options, IDataProvider dataProvider, string connectionString)
		{
			return options.WithOptions<ConnectionOptions>(o => o with { DataProvider = dataProvider, ConnectionString = connectionString });
		}

		/// <summary>
		/// Defines connection sting to use with DataOptions.
		/// </summary>
		public static DataOptions UseConnectionString(this DataOptions options, string connectionString)
		{
			return options.WithOptions<ConnectionOptions>(o => o with { ConnectionString = connectionString });
		}

		/// <summary>
		/// Defines configuration sting to use with DataOptions.
		/// </summary>
		public static DataOptions UseConfigurationString(this DataOptions options, string? configurationString)
		{
			return options.WithOptions<ConnectionOptions>(o => o with { ConfigurationString = configurationString });
		}

		/// <summary>
		/// Defines configuration sting and MappingSchema to use with DataOptions.
		/// </summary>
		public static DataOptions UseConfigurationString(this DataOptions options, string configurationString, MappingSchema mappingSchema)
		{
			return options
				.WithOptions<ConnectionOptions> (o => o with { ConfigurationString = configurationString, MappingSchema = mappingSchema });
		}

		/// <summary>
		/// Defines configuration sting to use with DataOptions.
		/// </summary>
		public static DataOptions UseConfiguration(this DataOptions options, string? configurationString)
		{
			return options.WithOptions<ConnectionOptions>(o => o with { ConfigurationString = configurationString });
		}

		/// <summary>
		/// Defines configuration sting and MappingSchema to use with DataOptions.
		/// </summary>
		public static DataOptions UseConfiguration(this DataOptions options, string configurationString, MappingSchema mappingSchema)
		{
			return options
				.WithOptions<ConnectionOptions> (o => o with { ConfigurationString = configurationString, MappingSchema = mappingSchema });
		}

		/// <summary>
		/// Defines DbConnection to use with DataOptions.
		/// </summary>
		public static DataOptions UseConnection(this DataOptions options, DbConnection connection)
		{
			return options.WithOptions<ConnectionOptions>(o => o with { DbConnection = connection });
		}

		/// <summary>
		/// Defines data provider and DbConnection to use with DataOptions.
		/// </summary>
		public static DataOptions UseConnection(this DataOptions options, IDataProvider dataProvider, DbConnection connection)
		{
			return options.WithOptions<ConnectionOptions>(o => o with { DataProvider = dataProvider, DbConnection = connection });
		}

		/// <summary>
		/// Defines data provider and DbConnection to use with DataOptions.
		/// </summary>
		public static DataOptions UseConnection(this DataOptions options, IDataProvider dataProvider, DbConnection connection, bool disposeConnection)
		{
			return options.WithOptions<ConnectionOptions>(o => o with { DataProvider = dataProvider, DbConnection = connection, DisposeConnection = disposeConnection });
		}

		/// <summary>
		/// Defines provider name to use with DataOptions.
		/// </summary>
		public static DataOptions UseProvider(this DataOptions options, string providerName)
		{
			return options.WithOptions<ConnectionOptions>(o => o with { ProviderName = providerName });
		}

		/// <summary>
		/// Defines data provider to use with DataOptions.
		/// </summary>
		public static DataOptions UseDataProvider(this DataOptions options, IDataProvider dataProvider)
		{
			return options.WithOptions<ConnectionOptions>(o => o with { DataProvider = dataProvider });
		}

		/// <summary>
		/// Defines mapping schema to use with DataOptions.
		/// </summary>
		public static DataOptions UseMappingSchema(this DataOptions options, MappingSchema mappingSchema)
		{
			return options.WithOptions<ConnectionOptions>(o => o with { MappingSchema = mappingSchema });
		}

		/// <summary>
		/// Defines connection factory to use with DataOptions.
		/// </summary>
		public static DataOptions UseConnectionFactory(this DataOptions options, Func<DataOptions, DbConnection> connectionFactory)
		{
			return options.WithOptions<ConnectionOptions>(o => o with { ConnectionFactory = connectionFactory });
		}

		/// <summary>
		/// Defines data provider and connection factory to use with DataOptions.
		/// </summary>
		public static DataOptions UseConnectionFactory(this DataOptions options, IDataProvider dataProvider, Func<DataOptions, DbConnection> connectionFactory)
		{
			return options.WithOptions<ConnectionOptions>(o => o with { DataProvider = dataProvider, ConnectionFactory = connectionFactory });
		}

		/// <summary>
		/// Sets DataProviderFactory option.
		/// </summary>
		public static DataOptions UseDataProviderFactory(this DataOptions options, Func<ConnectionOptions, IDataProvider> dataProviderFactory)
		{
			return options.WithOptions<ConnectionOptions>(o => o with { DataProviderFactory = dataProviderFactory });
		}

		/// <summary>
		/// Sets custom actions, executed before connection opened.
		/// </summary>
		/// <param name="afterConnectionOpening">
		/// Action, executed before database connection opened.
		/// Accepts connection instance as parameter.
		/// </param>
		/// <param name="afterConnectionOpeningAsync">
		/// Action, executed after database connection opened from async execution path.
		/// Accepts connection instance as parameter.
		/// If this option is not set, <paramref name="afterConnectionOpening"/> synchronous action called.
		/// Use this option only if you need to perform async work from action, otherwise <paramref name="afterConnectionOpening"/> is sufficient.
		/// </param>
		public static DataOptions UseBeforeConnectionOpened(
			this DataOptions                             options,
			Action<DbConnection>                         afterConnectionOpening,
			Func<DbConnection, CancellationToken, Task>? afterConnectionOpeningAsync = null)
		{
			return options.WithOptions<ConnectionOptions>(o => o with { ConnectionInterceptor = new(afterConnectionOpening, afterConnectionOpeningAsync, options.ConnectionOptions.ConnectionInterceptor?.OnConnectionOpened, options.ConnectionOptions.ConnectionInterceptor?.OnConnectionOpenedAsync) });
		}

		/// <summary>
		/// Sets custom actions, executed after connection opened.
		/// </summary>
		/// <param name="afterConnectionOpened">
		/// Action, executed for connection instance after <see cref="DbConnection.Open"/> call.
		/// Also called after <see cref="DbConnection.OpenAsync(CancellationToken)"/> call if <paramref name="afterConnectionOpenedAsync"/> action is not provided.
		/// Accepts connection instance as parameter.
		/// </param>
		/// <param name="afterConnectionOpenedAsync">
		/// Action, executed for connection instance from async execution path after <see cref="DbConnection.OpenAsync(CancellationToken)"/> call.
		/// Accepts connection instance as parameter.
		/// If this option is not set, <paramref name="afterConnectionOpened"/> synchronous action called.
		/// Use this option only if you need to perform async work from action, otherwise <paramref name="afterConnectionOpened"/> is sufficient.
		/// </param>
		public static DataOptions UseAfterConnectionOpened(
			this DataOptions                             options,
			Action<DbConnection>                         afterConnectionOpened,
			Func<DbConnection, CancellationToken, Task>? afterConnectionOpenedAsync = null)
		{
			return options.WithOptions<ConnectionOptions>(o => o with { ConnectionInterceptor = new(options.ConnectionOptions.ConnectionInterceptor?.OnConnectionOpening, options.ConnectionOptions.ConnectionInterceptor?.OnConnectionOpeningAsync, afterConnectionOpened, afterConnectionOpenedAsync) });
		}

		/// <summary>
		/// Defines data provider and transaction to use with DataOptions.
		/// </summary>
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

		/// <summary>
		/// Removes <see cref="IInterceptor" /> instance from the context.
		/// </summary>
		public static DataOptions RemoveInterceptor(this DataOptions options, IInterceptor interceptor)
		{
			return options.WithOptions<DataContextOptions>(o =>
			{
				var list = new List<IInterceptor>();

				if (o.Interceptors != null)
					foreach (var i in o.Interceptors)
						if (i != interceptor)
							list.Add(i);

				return o with { Interceptors = list };
			});
		}

		#endregion

		#region QueryTraceOptions

		/// <summary>
		/// Configure the database to use specified trace level.
		/// </summary>
		/// <returns>The builder instance so calls can be chained.</returns>
		public static QueryTraceOptions WithTraceLevel(this QueryTraceOptions options, TraceLevel traceLevel)
		{
			return options with { TraceLevel = traceLevel };
		}

		/// <summary>
		/// Configure the database to use the specified callback for logging or tracing.
		/// </summary>
		/// <param name="onTrace">Callback, may not be called depending on the trace level.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public static QueryTraceOptions WithOnTrace(this QueryTraceOptions options, Action<TraceInfo> onTrace)
		{
			return options with { OnTrace = onTrace };
		}

		/// <summary>
		/// Configure the database to use the specified a string trace callback.
		/// </summary>
		/// <param name="write">Callback, may not be called depending on the trace level.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public static QueryTraceOptions WithWriteTrace(this QueryTraceOptions options, Action<string?,string?,TraceLevel> write)
		{
			return options with { WriteTrace = write };
		}

		#endregion

		#region DataOptions.DataTraceOptions

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
		public static RetryPolicyOptions WithRetryPolicy(this RetryPolicyOptions options, IRetryPolicy retryPolicy)
		{
			return options with { RetryPolicy = retryPolicy };
		}

		/// <summary>
		/// Retry policy factory method, used to create retry policy for new <see cref="DataConnection"/> instance.
		/// If factory method is not set, retry policy is not used.
		/// Not set by default.
		/// </summary>
		public static RetryPolicyOptions WithFactory(this RetryPolicyOptions options, Func<DataConnection,IRetryPolicy?>? factory)
		{
			return options with { Factory = factory };
		}

		/// <summary>
		/// The number of retry attempts.
		/// Default value: <c>5</c>.
		/// </summary>
		public static RetryPolicyOptions WithMaxRetryCount(this RetryPolicyOptions options, int maxRetryCount)
		{
			return options with { MaxRetryCount = maxRetryCount };
		}

		/// <summary>
		/// The maximum time delay between retries, must be nonnegative.
		/// Default value: 30 seconds.
		/// </summary>
		public static RetryPolicyOptions WithMaxDelay(this RetryPolicyOptions options, TimeSpan maxDelay)
		{
			return options with { MaxDelay = maxDelay };
		}

		/// <summary>
		/// The maximum random factor, must not be lesser than 1.
		/// Default value: <c>1.1</c>.
		/// </summary>
		public static RetryPolicyOptions WithRandomFactor(this RetryPolicyOptions options, double randomFactor)
		{
			return options with { RandomFactor = randomFactor };
		}

		/// <summary>
		/// The base for the exponential function used to compute the delay between retries, must be positive.
		/// Default value: <c>2</c>.
		/// </summary>
		public static RetryPolicyOptions WithExponentialBase(this RetryPolicyOptions options, double exponentialBase)
		{
			return options with { ExponentialBase = exponentialBase };
		}

		/// <summary>
		/// The coefficient for the exponential function used to compute the delay between retries, must be nonnegative.
		/// Default value: 1 second.
		/// </summary>
		public static RetryPolicyOptions WithCoefficient(this RetryPolicyOptions options, TimeSpan coefficient)
		{
			return options with { Coefficient = coefficient };
		}

		#endregion

		#region DataOptions.RetryPolicyOptions

		/// <summary>
		/// Uses retry policy.
		/// </summary>
		public static DataOptions UseRetryPolicy(this DataOptions options, IRetryPolicy retryPolicy)
		{
			return options.WithOptions<RetryPolicyOptions>(o => o with { RetryPolicy = retryPolicy });
		}

		/// <summary>
		/// Uses default retry policy factory.
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

		#region BulkCopyOptions

		/// <summary>
		/// Number of rows in each batch. At the end of each batch, the rows in the batch are sent to the server.
		/// Returns an integer value or zero if no value has been set.
		/// </summary>
		public static BulkCopyOptions WithMaxBatchSize(this BulkCopyOptions options, int? maxBatchSize)
		{
			return options with { MaxBatchSize = maxBatchSize };
		}

		/// <summary>
		/// Number of seconds for the operation to complete before it times out.
		/// </summary>
		public static BulkCopyOptions WithBulkCopyTimeout(this BulkCopyOptions options, int? bulkCopyTimeout)
		{
			return options with { BulkCopyTimeout = bulkCopyTimeout };
		}

		/// <summary>
		/// Bulk copy mode.
		/// </summary>
		public static BulkCopyOptions WithBulkCopyType(this BulkCopyOptions options, BulkCopyType bulkCopyType)
		{
			return options with { BulkCopyType = bulkCopyType };
		}

		/// <summary>
		/// Checks constraints while data is being inserted.
		/// </summary>
		public static BulkCopyOptions WithCheckConstraints(this BulkCopyOptions options, bool? checkConstraints)
		{
			return options with { CheckConstraints = checkConstraints };
		}

		/// <summary>
		/// If this option set to true, bulk copy will use values of columns, marked with IsIdentity flag.
		/// SkipOnInsert flag in this case will be ignored.
		/// Otherwise those columns will be skipped and values will be generated by server.
		/// Not compatible with <see cref="BulkCopyType.RowByRow"/> mode.
		/// </summary>
		public static BulkCopyOptions WithKeepIdentity(this BulkCopyOptions options, bool? keepIdentity)
		{
			return options with { KeepIdentity = keepIdentity };
		}

		/// <summary>
		/// Obtains a bulk update lock for the duration of the bulk copy operation.
		/// </summary>
		public static BulkCopyOptions WithTableLock(this BulkCopyOptions options, bool? tableLock)
		{
			return options with { TableLock = tableLock };
		}

		/// <summary>
		/// Preserves null values in the destination table regardless of the settings for default values.
		/// </summary>
		public static BulkCopyOptions WithKeepNulls(this BulkCopyOptions options, bool? keepNulls)
		{
			return options with { KeepNulls = keepNulls };
		}

		/// <summary>
		/// When specified, causes the server to fire the insert triggers for the rows being inserted into the database.
		/// </summary>
		public static BulkCopyOptions WithFireTriggers(this BulkCopyOptions options, bool? fireTriggers)
		{
			return options with { FireTriggers = fireTriggers };
		}

		/// <summary>
		/// When specified, each batch of the bulk-copy operation will occur within a transaction.
		/// </summary>
		public static BulkCopyOptions WithUseInternalTransaction(this BulkCopyOptions options, bool? useInternalTransaction)
		{
			return options with { UseInternalTransaction = useInternalTransaction };
		}

		/// <summary>
		/// Gets or sets explicit name of target server instead of one, configured for copied entity in mapping schema.
		/// See <see cref="LinqExtensions.ServerName{T}(ITable{T}, string)"/> method for support information per provider.
		/// Also note that it is not supported by provider-specific insert method.
		/// </summary>
		public static BulkCopyOptions WithServerName(this BulkCopyOptions options, string? serverName)
		{
			return options with { ServerName = serverName };
		}

		/// <summary>
		/// Gets or sets explicit name of target database instead of one, configured for copied entity in mapping schema.
		/// See <see cref="LinqExtensions.DatabaseName{T}(ITable{T}, string)"/> method for support information per provider.
		/// </summary>
		public static BulkCopyOptions WithDatabaseName(this BulkCopyOptions options, string? databaseName)
		{
			return options with { DatabaseName = databaseName };
		}

		/// <summary>
		/// Gets or sets explicit name of target schema/owner instead of one, configured for copied entity in mapping schema.
		/// See <see cref="LinqExtensions.SchemaName{T}(ITable{T}, string)"/> method for support information per provider.
		/// </summary>
		public static BulkCopyOptions WithSchemaName(this BulkCopyOptions options, string? schemaName)
		{
			return options with { SchemaName = schemaName };
		}

		/// <summary>
		/// Gets or sets explicit name of target table instead of one, configured for copied entity in mapping schema.
		/// </summary>
		public static BulkCopyOptions WithTableName(this BulkCopyOptions options, string? tableName)
		{
			return options with { TableName = tableName };
		}

		/// <summary>
		/// Gets or sets <see cref="TableOptions"/> flags overrides instead of configured for copied entity in mapping schema.
		/// See <see cref="TableExtensions.IsTemporary{T}(ITable{T}, bool)"/> method for support information per provider.
		/// </summary>
		public static BulkCopyOptions WithTableOptions(this BulkCopyOptions options, TableOptions tableOptions)
		{
			return options with { TableOptions = tableOptions };
		}

		/// <summary>
		/// Gets or sets counter after how many copied records <see cref="BulkCopyOptions.RowsCopiedCallback"/> should be called.
		/// E.g. if you set it to 10, callback will be called after each 10 copied records.
		/// To disable callback, set this option to 0 (default value).
		/// </summary>
		public static BulkCopyOptions WithNotifyAfter(this BulkCopyOptions options, int notifyAfter)
		{
			return options with { NotifyAfter = notifyAfter };
		}

		/// <summary>
		/// Gets or sets callback method that will be called by BulkCopy operation after each <see cref="BulkCopyOptions.NotifyAfter"/> rows copied.
		/// This callback will not be used if <see cref="BulkCopyOptions.NotifyAfter"/> set to 0.
		/// </summary>
		public static BulkCopyOptions WithRowsCopiedCallback(this BulkCopyOptions options, Action<BulkCopyRowsCopied>? rowsCopiedCallback)
		{
			return options with { RowsCopiedCallback = rowsCopiedCallback };
		}

		/// <summary>
		/// Gets or sets whether to Always use Parameters for MultipleRowsCopy. Default is false.
		/// If True, provider's override for <see cref="BasicBulkCopy.MaxParameters"/> will be used to determine the maximum number of rows per insert,
		/// Unless overridden by <see cref="BulkCopyOptions.MaxParametersForBatch"/>.
		/// </summary>
		public static BulkCopyOptions WithUseParameters(this BulkCopyOptions options, bool useParameters)
		{
			return options with { UseParameters = useParameters };
		}

		/// <summary>
		/// If set, will override the Maximum parameters per batch statement from <see cref="BasicBulkCopy.MaxParameters"/>.
		/// </summary>
		public static BulkCopyOptions WithMaxParametersForBatch(this BulkCopyOptions options, int? maxParametersForBatch)
		{
			return options with { MaxParametersForBatch = maxParametersForBatch };
		}

		/// <summary>
		/// Implemented only by ClickHouse.Client provider. Defines number of connections, used for parallel insert in <see cref="BulkCopyType.ProviderSpecific"/> mode.
		/// </summary>
		public static BulkCopyOptions WithMaxDegreeOfParallelism(this BulkCopyOptions options, int? maxDegreeOfParallelism)
		{
			return options with { MaxDegreeOfParallelism = maxDegreeOfParallelism };
		}

		/// <summary>
		/// Implemented only by ClickHouse.Client provider. When set, provider-specific bulk copy will use session-less connection even if called over connection with session.
		/// Note that session-less connections cannot be used with session-bound functionality like temporary tables.
		/// </summary>
		public static BulkCopyOptions WithWithoutSession(this BulkCopyOptions options, bool withoutSession)
		{
			return options with { WithoutSession = withoutSession };
		}

		#endregion

		#region DataOptions.BulkCopyOptions

		/// <summary>
		/// Number of rows in each batch. At the end of each batch, the rows in the batch are sent to the server.
		/// Returns an integer value or zero if no value has been set.
		/// </summary>
		public static DataOptions UseBulkCopyMaxBatchSize(this DataOptions options, int? maxBatchSize)
		{
			return options.WithOptions<BulkCopyOptions>(o => o with { MaxBatchSize = maxBatchSize });
		}

		/// <summary>
		/// Number of seconds for the operation to complete before it times out.
		/// </summary>
		public static DataOptions UseBulkCopyTimeout(this DataOptions options, int? bulkCopyTimeout)
		{
			return options.WithOptions<BulkCopyOptions>(o => o with { BulkCopyTimeout = bulkCopyTimeout });
		}

		/// <summary>
		/// Specify bulk copy implementation type.
		/// </summary>
		public static DataOptions UseBulkCopyType(this DataOptions options, BulkCopyType bulkCopyType)
		{
			return options.WithOptions<BulkCopyOptions>(o => o with { BulkCopyType = bulkCopyType });
		}

		/// <summary>
		/// Enables database constrains enforcement during bulk copy operation.
		/// Supported with <see cref="BulkCopyType.ProviderSpecific" /> bulk copy mode for following databases:
		/// <list type="bullet">
		/// <item>Oracle</item>
		/// <item>SQL Server</item>
		/// <item>SAP/Sybase ASE</item>
		/// </list>
		/// </summary>
		public static DataOptions UseBulkCopyCheckConstraints(this DataOptions options, bool? checkConstraints)
		{
			return options.WithOptions<BulkCopyOptions>(o => o with { CheckConstraints = checkConstraints });
		}

		/// <summary>
		/// If this option set to true, bulk copy will use values of columns, marked with IsIdentity flag.
		/// SkipOnInsert flag in this case will be ignored.
		/// Otherwise those columns will be skipped and values will be generated by server.
		/// Not compatible with <see cref="BulkCopyType.RowByRow"/> mode.
		/// </summary>
		public static DataOptions UseBulkCopyKeepIdentity(this DataOptions options, bool? keepIdentity)
		{
			return options.WithOptions<BulkCopyOptions>(o => o with { KeepIdentity = keepIdentity });
		}

		/// <summary>
		/// Applies table lock during bulk copy operation.
		/// Supported with <see cref="BulkCopyType.ProviderSpecific" /> bulk copy mode for following databases:
		/// <list type="bullet">
		/// <item>DB2</item>
		/// <item>Informix (using DB2 provider)</item>
		/// <item>SQL Server</item>
		/// <item>SAP/Sybase ASE</item>
		/// </list>
		/// </summary>
		public static DataOptions UseBulkCopyTableLock(this DataOptions options, bool? tableLock)
		{
			return options.WithOptions<BulkCopyOptions>(o => o with { TableLock = tableLock });
		}

		/// <summary>
		/// Enables instert of <c>NULL</c> values instead of values from colum default constraint during bulk copy operation.
		/// Supported with <see cref="BulkCopyType.ProviderSpecific" /> bulk copy mode for following databases:
		/// <list type="bullet">
		/// <item>SQL Server</item>
		/// <item>SAP/Sybase ASE</item>
		/// </list>
		/// </summary>
		public static DataOptions UseBulkCopyKeepNulls(this DataOptions options, bool? keepNulls)
		{
			return options.WithOptions<BulkCopyOptions>(o => o with { KeepNulls = keepNulls });
		}

		/// <summary>
		/// Enables insert triggers during bulk copy operation.
		/// Supported with <see cref="BulkCopyType.ProviderSpecific" /> bulk copy mode for following databases:
		/// <list type="bullet">
		/// <item>Oracle</item>
		/// <item>SQL Server</item>
		/// <item>SAP/Sybase ASE</item>
		/// </list>
		/// </summary>
		public static DataOptions UseBulkCopyFireTriggers(this DataOptions options, bool? fireTriggers)
		{
			return options.WithOptions<BulkCopyOptions>(o => o with { FireTriggers = fireTriggers });
		}

		/// <summary>
		/// Enables automatic transaction creation during bulk copy operation.
		/// Supported with <see cref="BulkCopyType.ProviderSpecific" /> bulk copy mode for following databases:
		/// <list type="bullet">
		/// <item>Oracle</item>
		/// <item>SQL Server</item>
		/// <item>SAP/Sybase ASE</item>
		/// </list>
		/// </summary>
		public static DataOptions UseBulkCopyUseInternalTransaction(this DataOptions options, bool? useInternalTransaction)
		{
			return options.WithOptions<BulkCopyOptions>(o => o with { UseInternalTransaction = useInternalTransaction });
		}

		/// <summary>
		/// Gets or sets explicit name of target server instead of one, configured for copied entity in mapping schema.
		/// See <see cref="LinqExtensions.ServerName{T}(ITable{T}, string)"/> method for support information per provider.
		/// Also note that it is not supported by provider-specific insert method.
		/// </summary>
		public static DataOptions UseBulkCopyServerName(this DataOptions options, string? serverName)
		{
			return options.WithOptions<BulkCopyOptions>(o => o with { ServerName = serverName });
		}

		/// <summary>
		/// Gets or sets explicit name of target database instead of one, configured for copied entity in mapping schema.
		/// See <see cref="LinqExtensions.DatabaseName{T}(ITable{T}, string)"/> method for support information per provider.
		/// </summary>
		public static DataOptions UseBulkCopyDatabaseName(this DataOptions options, string? databaseName)
		{
			return options.WithOptions<BulkCopyOptions>(o => o with { DatabaseName = databaseName });
		}

		/// <summary>
		/// Gets or sets explicit name of target schema/owner instead of one, configured for copied entity in mapping schema.
		/// See <see cref="LinqExtensions.SchemaName{T}(ITable{T}, string)"/> method for support information per provider.
		/// </summary>
		public static DataOptions UseBulkCopySchemaName(this DataOptions options, string? schemaName)
		{
			return options.WithOptions<BulkCopyOptions>(o => o with { SchemaName = schemaName });
		}

		/// <summary>
		/// Gets or sets explicit name of target table instead of one, configured for copied entity in mapping schema.
		/// </summary>
		public static DataOptions UseBulkCopyTableName(this DataOptions options, string? tableName)
		{
			return options.WithOptions<BulkCopyOptions>(o => o with { TableName = tableName });
		}

		/// <summary>
		/// Gets or sets <see cref="TableOptions"/> flags overrides instead of configured for copied entity in mapping schema.
		/// See <see cref="TableExtensions.IsTemporary{T}(ITable{T}, bool)"/> method for support information per provider.
		/// </summary>
		public static DataOptions UseBulkCopyTableOptions(this DataOptions options, TableOptions tableOptions)
		{
			return options.WithOptions<BulkCopyOptions>(o => o with { TableOptions = tableOptions });
		}

		/// <summary>
		/// Gets or sets counter after how many copied records <see cref="BulkCopyOptions.RowsCopiedCallback"/> should be called.
		/// E.g. if you set it to 10, callback will be called after each 10 copied records.
		/// To disable callback, set this option to 0 (default value).
		/// </summary>
		public static DataOptions UseBulkCopyNotifyAfter(this DataOptions options, int notifyAfter)
		{
			return options.WithOptions<BulkCopyOptions>(o => o with { NotifyAfter = notifyAfter });
		}

		/// <summary>
		/// Gets or sets callback method that will be called by BulkCopy operation after each <see cref="BulkCopyOptions.NotifyAfter"/> rows copied.
		/// This callback will not be used if <see cref="BulkCopyOptions.NotifyAfter"/> set to 0.
		/// </summary>
		public static DataOptions UseBulkCopyRowsCopiedCallback(this DataOptions options, Action<BulkCopyRowsCopied>? rowsCopiedCallback)
		{
			return options.WithOptions<BulkCopyOptions>(o => o with { RowsCopiedCallback = rowsCopiedCallback });
		}

		/// <summary>
		/// Gets or sets whether to Always use Parameters for MultipleRowsCopy. Default is false.
		/// If True, provider's override for <see cref="BasicBulkCopy.MaxParameters"/> will be used to determine the maximum number of rows per insert,
		/// Unless overridden by <see cref="BulkCopyOptions.MaxParametersForBatch"/>.
		/// </summary>
		public static DataOptions UseBulkCopyUseParameters(this DataOptions options, bool useParameters)
		{
			return options.WithOptions<BulkCopyOptions>(o => o with { UseParameters = useParameters });
		}

		/// <summary>
		/// If set, will override the Maximum parameters per batch statement from <see cref="BasicBulkCopy.MaxParameters"/>.
		/// </summary>
		public static DataOptions UseBulkCopyMaxParametersForBatch(this DataOptions options, int? maxParametersForBatch)
		{
			return options.WithOptions<BulkCopyOptions>(o => o with { MaxParametersForBatch = maxParametersForBatch });
		}

		/// <summary>
		/// Implemented only by ClickHouse.Client provider. Defines number of connections, used for parallel insert in <see cref="BulkCopyType.ProviderSpecific"/> mode.
		/// </summary>
		public static DataOptions UseBulkCopyMaxDegreeOfParallelism(this DataOptions options, int? maxDegreeOfParallelism)
		{
			return options.WithOptions<BulkCopyOptions>(o => o with { MaxDegreeOfParallelism = maxDegreeOfParallelism });
		}

		/// <summary>
		/// Implemented only by ClickHouse.Client provider. When set, provider-specific bulk copy will use session-less connection even if called over connection with session.
		/// Note that session-less connections cannot be used with session-bound functionality like temporary tables.
		/// </summary>
		public static DataOptions UseBulkCopyWithoutSession(this DataOptions options, bool withoutSession)
		{
			return options.WithOptions<BulkCopyOptions>(o => o with { WithoutSession = withoutSession });
		}

		#endregion
	}
}
