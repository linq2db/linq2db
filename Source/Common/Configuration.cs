using System;

using JetBrains.Annotations;

namespace LinqToDB.Common
{
	using Data;
#if !SILVERLIGHT && !WINSTORE
	using Data.RetryPolicy;
#endif

	[PublicAPI]
	public static class Configuration
	{
		public static bool IsStructIsScalarType = true;
		public static bool AvoidSpecificDataProviderAPI;

		public static class Linq
		{
			public static bool PreloadGroups;
			public static bool IgnoreEmptyUpdate;
			public static bool AllowMultipleQuery;
			public static bool GenerateExpressionTest;
			public static bool TraceMapperExpression;
			public static bool DoNotClearOrderBys;
			public static bool OptimizeJoins = true;

			/// <summary>
			/// If set to true nullable fields would be checked for IS NULL when comparasion type is NotEqual 
			/// <example>
			/// public class MyEntity
			/// {
			///     public int? Value;
			/// }
			/// 
			/// db.MyEntity.Where(e => e.Value != 10)
			/// 
			/// Would be converted to
			/// 
			/// SELECT Value FROM MyEntity WHERE Value IS NULL OR Value != 10
			/// </example>
			/// </summary>
			public static bool CheckNullForNotEquals = true;

			/// <summary>
			/// Prevents to use constructions like q.GroupBy(_ => _.SomeValue) which leads to unexpected behaviour.
			/// </summary>
			/// <remarks>
			/// https://github.com/linq2db/linq2db/issues/365
			/// </remarks>
			public static bool GuardGrouping = false;

			/// <summary>
			/// Experimental
			/// Used to optimize big logical operations with great number of operands like expr1.and.axpr2...and.exprN into to one <see cref="LinqToDB.Expressions.BinaryAggregateExpression"/>.
			/// This saves from deep recursion in visitors.
			/// <remarks>
			/// Default: <value>false</value>
			/// Switched off in 1.8.2 as unstable
			/// </remarks>
			/// </summary>
			/// <remarks>
			/// See: 
			/// https://github.com/linq2db/linq2db/issues/447
			/// https://github.com/linq2db/linq2db/pull/563
			/// </remarks>
			public static bool UseBinaryAggregateExpression = false;

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
			/// See <see cref="https://github.com/linq2db/linq2db/issues/256"/> for more details.
			/// </summary>
			public static bool DisableQueryCache;
		}

		[PublicAPI]
		public static class LinqService
		{
			public static bool SerializeAssemblyQualifiedName;
			public static bool ThrowUnresolvedTypeException;
		}

#if !SILVERLIGHT && !WINSTORE
		public static class RetryPolicy
		{
			public static Func<DataConnection,IRetryPolicy> Factory;

			public static bool UseDefaultPolicy
			{
				get { return Factory != null; }
				set { Factory = value ? DefaultRetryPolicyFactory.GetRetryPolicy : (Func<DataConnection,IRetryPolicy>)null; }
			}

			/// <summary>
			/// The default number of retry attempts.
			/// </summary>
			public static int DefaultMaxRetryCount = 5;

			/// <summary>
			/// The default maximum time delay between retries, must be nonnegative.
			/// </summary>
			public static TimeSpan DefaultMaxDelay = TimeSpan.FromSeconds(30);

			/// <summary>
			/// The default maximum random factor, must not be lesser than 1.
			/// </summary>
			public static double DefaultRandomFactor = 1.1;

			/// <summary>
			/// The default base for the exponential function used to compute the delay between retries, must be positive.
			/// </summary>
			public static double DefaultExponentialBase = 2;

			/// <summary>
			/// The default coefficient for the exponential function used to compute the delay between retries, must be nonnegative.
			/// </summary>
			public static TimeSpan DefaultCoefficient = TimeSpan.FromSeconds(1);
		}
#endif
	}
}
