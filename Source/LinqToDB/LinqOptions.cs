using System;
using System.Linq.Expressions;

namespace LinqToDB
{
	using Common;
	using Common.Internal;
	using Data;
	using Linq;

	/// <param name="PreloadGroups">
	/// Controls how group data for LINQ queries ended with GroupBy will be loaded:
	/// - if <c>true</c> - group data will be loaded together with main query, resulting in 1 + N queries, where N - number of groups;
	/// - if <c>false</c> - group data will be loaded when you call enumerator for specific group <see cref="System.Linq.IGrouping{TKey, TElement}"/>.
	/// Default value: <c>false</c>.
	/// </param>
	/// <param name="IgnoreEmptyUpdate">
	/// Controls behavior of linq2db when there is no updateable fields in Update query:
	/// - if <c>true</c> - query not executed and Update operation returns 0 as number of affected records;
	/// - if <c>false</c> - <see cref="LinqToDBException"/> will be thrown.
	/// Default value: <c>false</c>.
	/// </param>
	/// <param name="GenerateExpressionTest">
	/// Enables generation of test class for each LINQ query, executed while this option is enabled.
	/// This option could be useful for issue reporting, when you need to provide reproducible case.
	/// Test file will be placed to <c>linq2db</c> subfolder of temp folder and exact file path will be logged
	/// to data connection tracing infrastructure.
	/// See <see cref="DataConnection.TraceSwitch"/> for more details.
	/// Default value: <c>false</c>.
	/// </param>
	/// <param name="TraceMapperExpression">
	/// Enables logging of generated mapping expression to data connection tracing infrastructure.
	/// See <see cref="DataConnection.TraceSwitch"/> for more details.
	/// Default value: <c>false</c>.
	/// </param>
	/// <param name="DoNotClearOrderBys">
	/// Controls behavior, when LINQ query chain contains multiple <see cref="System.Linq.Queryable.OrderBy{TSource, TKey}(System.Linq.IQueryable{TSource}, Expression{Func{TSource, TKey}})"/> or <see cref="System.Linq.Queryable.OrderByDescending{TSource, TKey}(System.Linq.IQueryable{TSource}, Expression{Func{TSource, TKey}})"/> calls:
	/// - if <c>true</c> - non-first OrderBy* call will be treated as ThenBy* call;
	/// - if <c>false</c> - OrderBy* call will discard sort specifications, added by previous OrderBy* and ThenBy* calls.
	/// Default value: <c>false</c>.
	/// </param>
	/// <param name="OptimizeJoins">
	/// If enabled, linq2db will try to reduce number of generated SQL JOINs for LINQ query.
	/// Attempted optimizations:
	/// - removes duplicate joins by unique target table key;
	/// - removes self-joins by unique key;
	/// - removes left joins if joined table is not used in query.
	/// Default value: <c>true</c>.
	/// </param>
	/// <param name="CompareNulls">
	/// <summary>
	/// If set to <see cref="CompareNulls.LikeClr" /> nullable fields would be checked for <c>IS NULL</c> in Equal/NotEqual comparisons.
	/// If set to <see cref="CompareNulls.LikeSql" /> comparisons are compiled straight to equivalent SQL operators, 
	/// which consider nulls values as not equal.
	/// <see cref="CompareNulls.LikeSqlExceptParameters" /> is a backward compatible option that works mostly as <see cref="CompareNulls.LikeSql" />, 
	/// but sniffs parameters value and changes = into <c>IS NULL</c> when parameters are null.
	/// Comparisons to literal null are always compiled into <c>IS NULL</c>.
	/// This affects: Equal, NotEqual, Not Contains
	/// Default value: <see cref="CompareNulls.LikeClr" />.
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
	/// Would be converted to next queries under <see cref="CompareNulls.LikeClr" />:
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
	/// </param>
	/// <param name="GuardGrouping">
	/// <summary>
	/// Controls behavior of LINQ query, which ends with GroupBy call.
	/// - if <c>true</c> - <seealso cref="LinqToDBException"/> will be thrown for such queries;
	/// - if <c>false</c> - behavior is controlled by <see cref="PreloadGroups"/> option.
	/// Default value: <c>true</c>.
	/// </summary>
	/// <remarks>
	/// <a href="https://github.com/linq2db/linq2db/issues/365">More details</a>.
	/// </remarks>
	/// </param>
	/// <param name="DisableQueryCache">
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
	/// </param>
	/// <param name="CacheSlidingExpiration">
	/// Specifies timeout when query will be evicted from cache since last execution of query.
	/// Default value is 1 hour.
	/// </param>
	/// <param name="PreferApply">
	/// Used to generate CROSS APPLY or OUTER APPLY if possible.
	/// Default value: <c>true</c>.
	/// </param>
	/// <param name="KeepDistinctOrdered">
	/// Allows SQL generation to automatically transform
	/// <code>SELECT DISTINCT value FROM Table ORDER BY date</code>
	/// Into GROUP BY equivalent if syntax is not supported
	/// Default value: <c>true</c>.
	/// </param>
	/// <param name="ParameterizeTakeSkip">
	/// Enables Take/Skip parameterization.
	/// Default value: <c>true</c>.
	/// </param>
	/// <param name="EnableContextSchemaEdit">
	/// If <c>true</c>, user could add new mappings to context mapping schems (<see cref="IDataContext.MappingSchema"/>).
	/// Otherwise, <see cref="LinqToDBException"/> will be generated on locked mapping schema edit attempt.
	/// It is not recommended to enable this option as it has performance implications.
	/// Proper approach is to create single <see cref="Mapping.MappingSchema"/> instance once, configure mappings for it and use this <see cref="Mapping.MappingSchema"/> instance for all context instances.
	/// Default value: <c>false</c>.
	/// </param>
	/// <param name="PreferExistsForScalar">
	/// If <c>true</c>, EXISTS operator will be generated instead of IN operator for scalar values.
	/// <code>
	/// SELECT Value FROM MyEntity e WHERE EXISTS(SELECT * FROM MyEntity2 e2 WHERE e2.Value = e.Value)
	/// </code>
	/// vs
	/// <code>
	/// SELECT Value FROM MyEntity e WHERE Value IN (SELECT Value FROM MyEntity2 e2)
	/// </code>
	/// Default value: <c>false</c>.
	/// </param>
	public sealed record LinqOptions
	(
		bool         PreloadGroups           = false,
		bool         IgnoreEmptyUpdate       = false,
		bool         GenerateExpressionTest  = false,
		bool         TraceMapperExpression   = false,
		bool         DoNotClearOrderBys      = false,
		bool         OptimizeJoins           = true,
		CompareNulls CompareNulls            = CompareNulls.LikeClr,
		bool         GuardGrouping           = true,
		bool         DisableQueryCache       = false,
		TimeSpan?    CacheSlidingExpiration  = default,
		bool         PreferApply             = true,
		bool         KeepDistinctOrdered     = true,
		bool         ParameterizeTakeSkip    = true,
		bool         EnableContextSchemaEdit = false,
		bool         PreferExistsForScalar   = default
		// If you add another parameter here, don't forget to update
		// LinqOptions copy constructor and IConfigurationID.ConfigurationID.
	)
		: IOptionSet
	{
		public LinqOptions() : this(CacheSlidingExpiration : TimeSpan.FromHours(1))
		{
		}

		LinqOptions(LinqOptions original)
		{
			PreloadGroups           = original.PreloadGroups;
			IgnoreEmptyUpdate       = original.IgnoreEmptyUpdate;
			GenerateExpressionTest  = original.GenerateExpressionTest;
			TraceMapperExpression   = original.TraceMapperExpression;
			DoNotClearOrderBys      = original.DoNotClearOrderBys;
			OptimizeJoins           = original.OptimizeJoins;
			CompareNulls            = original.CompareNulls;
			GuardGrouping           = original.GuardGrouping;
			DisableQueryCache       = original.DisableQueryCache;
			CacheSlidingExpiration  = original.CacheSlidingExpiration;
			PreferApply             = original.PreferApply;
			KeepDistinctOrdered     = original.KeepDistinctOrdered;
			ParameterizeTakeSkip    = original.ParameterizeTakeSkip;
			EnableContextSchemaEdit = original.EnableContextSchemaEdit;
			PreferExistsForScalar   = original.PreferExistsForScalar;
		}

		int? _configurationID;
		int IConfigurationID.ConfigurationID
		{
			get
			{
				if (_configurationID == null)
				{
					using var idBuilder = new IdentifierBuilder();
					_configurationID = idBuilder
						.Add(PreloadGroups)
						.Add(IgnoreEmptyUpdate)
						.Add(GenerateExpressionTest)
						.Add(TraceMapperExpression)
						.Add(DoNotClearOrderBys)
						.Add(OptimizeJoins)
						.Add((int)CompareNulls)
						.Add(GuardGrouping)
						.Add(DisableQueryCache)
						.Add(CacheSlidingExpiration)
						.Add(PreferApply)
						.Add(KeepDistinctOrdered)
						.Add(ParameterizeTakeSkip)
						.Add(EnableContextSchemaEdit)
						.Add(PreferExistsForScalar)
						.CreateID();
				}

				return _configurationID.Value;
			}
		}

		public TimeSpan CacheSlidingExpirationOrDefault => CacheSlidingExpiration ?? TimeSpan.FromHours(1);

		#region IEquatable implementation

		public bool Equals(LinqOptions? other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;

			return ((IOptionSet)this).ConfigurationID == ((IOptionSet)other).ConfigurationID;
		}

		public override int GetHashCode()
		{
			return ((IOptionSet)this).ConfigurationID;
		}

		#endregion
	}
}
