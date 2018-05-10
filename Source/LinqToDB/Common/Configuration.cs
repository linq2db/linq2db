using System;

using JetBrains.Annotations;

namespace LinqToDB.Common
{
	using Data;
	using Data.RetryPolicy;

	/// <summary>
	/// Contains global linq2db settings.
	/// </summary>
	[PublicAPI]
	public static class Configuration
	{
		/// <summary>
		/// If <c>true</c> - non-primitive and non-enum value types (structures) will be treated as scalar types (e.g. <see cref="DateTime"/>) during mapping;
		/// otherwise they will be treated the same way as classes.
		/// Default value: <c>true</c>.
		/// </summary>
		public static bool IsStructIsScalarType = true;

		/// <summary>
		/// If <c>true</c> - Enum values are stored as by calling ToString().
		/// Default value: <c>true</c>.
		/// </summary>
		public static bool UseEnumValueNameForStringColumns = true;

		/// <summary>
		/// If <c>true</c> - data providers will try to use standard ADO.NET interfaces instead of provider-specific functionality when possible. This option could be usefull if you need to intercept
		/// database calls using tools such as <a href="https://github.com/MiniProfiler/dotnet">MiniProfiler</a>.
		/// Default value: <c>false</c>.
		/// </summary>
		public static bool AvoidSpecificDataProviderAPI;

		public static class Data
		{
			public static bool ThrowOnDisposed = true;
		}

		/// <summary>
		/// LINQ query settings.
		/// </summary>
		[PublicAPI]
		public static class Linq
		{
			/// <summary>
			/// Controls how group data for LINQ queries ended with GroupBy will be loaded:
			/// - if <c>true</c> - group data will be loaded together with main query, resulting in 1 + N queries, where N - number of groups;
			/// - if <c>false</c> - group data will be loaded when you call enumerator for specific group <see cref="System.Linq.IGrouping{TKey, TElement}"/>.
			/// Default value: <c>false</c>.
			/// </summary>
			public static bool PreloadGroups;

			/// <summary>
			/// Controls behavior of linq2db when there is no updateable fields in Update query:
			/// - if <c>true</c> - query not executed and Update operation returns 0 as number of affected records;
			/// - if <c>false</c> - <see cref="LinqToDB.Linq.LinqException"/> will be thrown.
			/// Default value: <c>false</c>.
			/// </summary>
			public static bool IgnoreEmptyUpdate;

			/// <summary>
			/// Controls behavior of linq2db when multiple queries required to load requested data:
			/// - if <c>true</c> - multiple queries allowed;
			/// - if <c>false</c> - <see cref="LinqToDB.Linq.LinqException"/> will be thrown.
			/// This option required, if you want to select related collections, e.g. using <see cref="LinqExtensions.LoadWith{T}(ITable{T}, System.Linq.Expressions.Expression{Func{T, object}})"/> method.
			/// Default value: <c>false</c>.
			/// </summary>
			public static bool AllowMultipleQuery;

			/// <summary>
			/// Enables generation of test class for each LINQ query, executed while this option is enabled.
			/// This option could be usefull for issue reporting, when you need to provide reproduceable case.
			/// Test file will be placed to <c>linq2db</c> subfolder of temp folder and exact file path will be logged
			/// to data connection tracing infrastructure.
			/// See <see cref="DataConnection.TraceSwitch"/> for more details.
			/// Default value: <c>false</c>.
			/// </summary>
			public static bool GenerateExpressionTest;

			/// <summary>
			/// Enables logging of generated mapping expression to data connection tracing infrastructure.
			/// See <see cref="DataConnection.TraceSwitch"/> for more details.
			/// Default value: <c>false</c>.
			/// </summary>
			public static bool TraceMapperExpression;

			/// <summary>
			/// Controls behavior, when LINQ query chain contains multiple <see cref="System.Linq.Queryable.OrderBy{TSource, TKey}(System.Linq.IQueryable{TSource}, System.Linq.Expressions.Expression{Func{TSource, TKey}})"/> or <see cref="System.Linq.Queryable.OrderByDescending{TSource, TKey}(System.Linq.IQueryable{TSource}, System.Linq.Expressions.Expression{Func{TSource, TKey}})"/> calls:
			/// - if <c>true</c> - non-first OrderBy* call will be treated as ThenBy* call;
			/// - if <c>false</c> - OrdredBy* call will discard sort specifications, added by previous OrderBy* and ThenBy* calls.
			/// Default value: <c>false</c>.
			/// </summary>
			public static bool DoNotClearOrderBys;

			/// <summary>
			/// If enabled, linq2db will try to reduce number of generated SQL JOINs for LINQ query.
			/// Attempted optimizations:
			/// - removes duplicate joins by unique target table key;
			/// - removes self-joins by unique key;
			/// - removes left joins if joined table is not used in query.
			/// Default value: <c>true</c>.
			/// </summary>
			public static bool OptimizeJoins = true;

			/// <summary>
			/// If set to true nullable fields would be checked for IS NULL in Equal/NotEqual comparasions.
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
			public static bool CompareNullsAsValues = true;

			/// <summary>
			/// Controls behavior of LINQ query, which ends with GroupBy call.
			/// - if <c>true</c> - <seealso cref="LinqToDBException"/> will be thrown for such queries;
			/// - if <c>false</c> - behavior is controlled by <see cref="PreloadGroups"/> option.
			/// Default value: <c>false</c>.
			/// </summary>
			/// <remarks>
			/// <a href="https://github.com/linq2db/linq2db/issues/365">More details</a>.
			/// </remarks>
			public static bool GuardGrouping;

#pragma warning disable 1574
			/// <summary>
			/// Used to optimize huge logical operations with large number of operands like expr1.and.axpr2...and.exprN into balanced tree.
			/// Without this option, such conditions could lead to <seealso cref="StackOverflowException"/>.
			/// Default value: <c>false</c>.
			/// </summary>
			public static bool UseBinaryAggregateExpression;
#pragma warning restore 1574

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
			/// to call <see cref="LinqToDB.Linq.Query{T}.ClearCache"/> method to cleanup cache after queries, that produce severe memory leaks you need to fix.
			/// <para />
			/// <a href="https://github.com/linq2db/linq2db/issues/256">More details</a>.
			/// </summary>
			public static bool DisableQueryCache;

			/// <summary>
			/// Used to generate CROSS APPLY or OUTER APPLY if possible.
			/// </summary>
			public static bool PreferApply = true;
		}

		/// <summary>
		/// Linq over WCF global settings.
		/// </summary>
		[PublicAPI]
		public static class LinqService
		{
			/// <summary>
			/// Controls format of type name, sent over WCF:
			/// - if <c>true</c> - name from <see cref="Type.AssemblyQualifiedName"/> used;
			/// - if <c>false</c> - name from <see cref="Type.FullName"/> used.
			/// Default value: <c>false</c>.
			/// </summary>
			public static bool SerializeAssemblyQualifiedName;

			/// <summary>
			/// Controls behavior of linq2db, when it cannot load <see cref="Type"/> by type name on query deserialization:
			/// - if <c>true</c> - <see cref="LinqToDBException"/> will be thrown;
			/// - if <c>false</c> - type load error will be ignored.
			/// Default value: <c>false</c>.
			/// </summary>
			public static bool ThrowUnresolvedTypeException;
		}

		/// <summary>
		/// Retry policy global settings.
		/// </summary>
		[PublicAPI]
		public static class RetryPolicy
		{
			/// <summary>
			/// Retry policy factory method, used to create retry policy for new <see cref="DataConnection"/> instance.
			/// If factory method is not set, retry policy is not used.
			/// Not set by default.
			/// </summary>
			public static Func<DataConnection,IRetryPolicy> Factory;

			/// <summary>
			/// Status of use of default retry policy.
			/// Getter returns <c>true</c> if default retry policy used, and false if custom retry policy used or retry policy is not set.
			/// Setter sets <see cref="Factory"/> to default retry policy factory if <paramref name="value"/> is <c>true</c>, otherwise removes retry policy.
			/// </summary>
			public static bool UseDefaultPolicy
			{
				get { return Factory == DefaultRetryPolicyFactory.GetRetryPolicy; }
				set { Factory = value ? DefaultRetryPolicyFactory.GetRetryPolicy : (Func<DataConnection,IRetryPolicy>)null; }
			}

			/// <summary>
			/// The default number of retry attempts.
			/// Default value: <c>5</c>.
			/// </summary>
			public static int DefaultMaxRetryCount = 5;

			/// <summary>
			/// The default maximum time delay between retries, must be nonnegative.
			/// Default value: 30 seconds.
			/// </summary>
			public static TimeSpan DefaultMaxDelay = TimeSpan.FromSeconds(30);

			/// <summary>
			/// The default maximum random factor, must not be lesser than 1.
			/// Default value: <c>1.1</c>.
			/// </summary>
			public static double DefaultRandomFactor = 1.1;

			/// <summary>
			/// The default base for the exponential function used to compute the delay between retries, must be positive.
			/// Default value: <c>2</c>.
			/// </summary>
			public static double DefaultExponentialBase = 2;

			/// <summary>
			/// The default coefficient for the exponential function used to compute the delay between retries, must be nonnegative.
			/// Default value: 1 second.
			/// </summary>
			public static TimeSpan DefaultCoefficient = TimeSpan.FromSeconds(1);
		}
	}
}
