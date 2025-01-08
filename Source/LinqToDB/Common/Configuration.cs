using System;
using System.ComponentModel;
using System.Data;
using System.Linq.Expressions;
using System.Threading.Tasks;

using JetBrains.Annotations;

#if SUPPORTS_COMPOSITE_FORMAT
using System.Text;
#endif

namespace LinqToDB.Common
{
	using Data;
	using Data.RetryPolicy;
	using Linq;

	/// <summary>
	/// Contains LINQ expression compilation options.
	/// </summary>
	public static class Compilation
	{
		private static Func<LambdaExpression,Delegate?>? _compiler;

		/// <summary>
		/// Sets LINQ expression compilation method.
		/// </summary>
		/// <param name="compiler">Method to use for expression compilation or <c>null</c> to reset compilation logic to defaults.</param>
		public static void SetExpressionCompiler(Func<LambdaExpression, Delegate?>? compiler)
		{
			_compiler = compiler;
		}

		/// <summary>
		/// Internal API.
		/// </summary>
		public static TDelegate CompileExpression<TDelegate>(this Expression<TDelegate> expression)
			where TDelegate : Delegate
		{
			return ((TDelegate?)_compiler?.Invoke(expression))
#pragma warning disable RS0030 // Do not use banned APIs
				?? expression.Compile();
#pragma warning restore RS0030 // Do not use banned APIs
		}

		/// <summary>
		/// Internal API.
		/// </summary>
		public static Delegate CompileExpression(this LambdaExpression expression)
		{
			return _compiler?.Invoke(expression)
#pragma warning disable RS0030 // Do not use banned APIs
				?? expression.Compile();
#pragma warning restore RS0030 // Do not use banned APIs
		}
	}

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
		///	<b>Obsolete</b>: All <see cref="Task"/>s are now awaited using <c>ConfigureAwait(false)</c>. Please remove references to this property.
		/// </summary>
		// TODO: V7 remove
		[Obsolete("This API doesn't have effect anymore and will be removed in future")]
		public static bool ContinueOnCapturedContext;

		/// <summary>
		/// Enables mapping expression to be compatible with <see cref="CommandBehavior.SequentialAccess"/> behavior.
		/// Note that it doesn't switch linq2db to use <see cref="CommandBehavior.SequentialAccess"/> behavior for
		/// queries, so this optimization could be used for <see cref="CommandBehavior.Default"/> too.
		/// Default value: <c>false</c>.
		/// </summary>
		public static bool OptimizeForSequentialAccess;

		/// <summary>
		/// Determines the length after which logging of binary data in SQL will be truncated.
		/// This is to avoid Out-Of-Memory exceptions when getting SqlText from <see cref="TraceInfo"/>
		/// or <see cref="IExpressionQuery"/> for logging or other purposes.
		/// </summary>
		/// <remarks>
		/// This value defaults to 100.
		/// Use a value of -1 to disable and always log full binary.
		/// Set to 0 to truncate all binary data.
		/// </remarks>
		public static int MaxBinaryParameterLengthLogging { get; set; } = 100;

		/// <summary>
		/// Determines the length after which logging of string data in SQL will be truncated.
		/// This is to avoid Out-Of-Memory exceptions when getting SqlText from <see cref="TraceInfo"/>
		/// or <see cref="IExpressionQuery"/> for logging or other purposes.
		/// </summary>
		/// <remarks>
		/// This value defaults to 200.
		/// Use a value of -1 to disable and always log full string.
		/// Set to 0 to truncate all string data.
		/// </remarks>
		public static int MaxStringParameterLengthLogging { get; set; } = 200;

		/// <summary>
		/// Determines number of items after which logging of collection data in SQL will be truncated.
		/// This is to avoid Out-Of-Memory exceptions when getting SqlText from <see cref="TraceInfo"/>
		/// or <see cref="IExpressionQuery"/> for logging or other purposes.
		/// </summary>
		/// <remarks>
		/// This value defaults to 8 elements.
		/// Use a value of -1 to disable and always log full collection.
		/// Set to 0 to truncate all data.
		/// </remarks>
		public static int MaxArrayParameterLengthLogging { get; set; } = 8;

		private static bool _useNullableTypesMetadata;
		/// <summary>
		/// Whether or not Nullable Reference Types annotations from C#
		/// are read and taken into consideration to determine if a
		/// column or association can be null.
		/// Nullable Types can be overriden with explicit CanBeNull
		/// annotations in [Column], [Association], or [Nullable].
		/// </summary>
		/// <remarks>Defaults to false.</remarks>
		public static bool UseNullableTypesMetadata
		{
			get => _useNullableTypesMetadata;
			set
			{
				// Can't change the default value of "false" on platforms where nullable metadata is unavailable.
				if (value) Mapping.Nullability.EnsureSupport();
				_useNullableTypesMetadata = value;
			}
		}

		/// <summary>
		/// Enables tracing of object materialization activity. It can significantly break performance if tracing consumer performs slow, so it is disabled by default.
		/// </summary>
		public static bool TraceMaterializationActivity { get; set; }

		public static class Data
		{
			/// <summary>
			/// Enables throwing of <see cref="ObjectDisposedException"/> when access disposed <see cref="DataConnection"/> instance.
			/// Default value: <c>true</c>.
			/// </summary>
			public static bool ThrowOnDisposed = true;

			/// <summary>
			/// Controls behavior of bulk copy timeout if <see cref="BulkCopyOptions.BulkCopyTimeout"/> is not provided.
			/// - if <c>true</c> - the current timeout on the <see cref="DataConnection"/> is used
			/// - if <c>false</c> - command timeout is infinite.
			/// Default value: <c>false</c>.
			/// </summary>
			public static bool BulkCopyUseConnectionCommandTimeout;
		}

		// N: supported in options

		/// <summary>
		/// LINQ query settings.
		/// </summary>
		[PublicAPI]
		public static class Linq
		{
			private static volatile LinqOptions _options = new ();

			/// <summary>
			/// Default <see cref="LinqOptions"/> options. Automatically synchronized with other settings in <see cref="Linq"/> class.
			/// </summary>
			public  static LinqOptions Options
			{
				get => _options;
				set
				{
					_options = value;
					DataConnection.ResetDefaultOptions();
					DataConnection.ConnectionOptionsByConfigurationString.Clear();
				}
			}

			/// <summary>
			/// Controls how group data for LINQ queries ended with GroupBy will be loaded:
			/// - if <c>true</c> - group data will be loaded together with main query, resulting in 1 + N queries, where N - number of groups;
			/// - if <c>false</c> - group data will be loaded when you call enumerator for specific group <see cref="System.Linq.IGrouping{TKey, TElement}"/>.
			/// Default value: <c>false</c>.
			/// </summary>
			// TODO: V7 remove
			[Obsolete("This API doesn't have effect anymore and will be removed in future")]
			public static bool PreloadGroups { get; set; }

			/// <summary>
			/// Controls behavior of linq2db when there is no updateable fields in Update query:
			/// - if <c>true</c> - query not executed and Update operation returns 0 as number of affected records;
			/// - if <c>false</c> - <see cref="LinqToDBException"/> will be thrown.
			/// Default value: <c>false</c>.
			/// </summary>
			public static bool IgnoreEmptyUpdate
			{
				get => Options.IgnoreEmptyUpdate;
				set
				{
					if (Options.IgnoreEmptyUpdate != value)
						Options = Options with { IgnoreEmptyUpdate = value };
				}
			}

			/// <summary>
			/// Enables generation of test class for each LINQ query, executed while this option is enabled.
			/// This option could be useful for issue reporting, when you need to provide reproducible case.
			/// Test file will be placed to <c>linq2db</c> subfolder of temp folder and exact file path will be logged
			/// to data connection tracing infrastructure.
			/// See <see cref="DataConnection.TraceSwitch"/> for more details.
			/// Default value: <c>false</c>.
			/// </summary>
			public static bool GenerateExpressionTest
			{
				get => Options.GenerateExpressionTest;
				set
				{
					if (Options.GenerateExpressionTest != value)
						Options = Options with { GenerateExpressionTest = value };
				}
			}

			/// <summary>
			/// Enables logging of generated mapping expression to data connection tracing infrastructure.
			/// See <see cref="DataConnection.TraceSwitch"/> for more details.
			/// Default value: <c>false</c>.
			/// </summary>
			public static bool TraceMapperExpression
			{
				get => Options.TraceMapperExpression;
				set
				{
					if (Options.TraceMapperExpression != value)
						Options = Options with { TraceMapperExpression = value };
				}
			}

			/// <summary>
			/// Controls behavior, when LINQ query chain contains multiple <see cref="System.Linq.Queryable.OrderBy{TSource, TKey}(System.Linq.IQueryable{TSource}, Expression{Func{TSource, TKey}})"/> or <see cref="System.Linq.Queryable.OrderByDescending{TSource, TKey}(System.Linq.IQueryable{TSource}, Expression{Func{TSource, TKey}})"/> calls:
			/// - if <c>true</c> - non-first OrderBy* call will be treated as ThenBy* call;
			/// - if <c>false</c> - OrderBy* call will discard sort specifications, added by previous OrderBy* and ThenBy* calls.
			/// Default value: <c>false</c>.
			/// </summary>
			public static bool DoNotClearOrderBys
			{
				get => Options.DoNotClearOrderBys;
				set
				{
					if (Options.DoNotClearOrderBys != value)
						Options = Options with { DoNotClearOrderBys = value };
				}
			}

			/// <summary>
			/// If enabled, linq2db will try to reduce number of generated SQL JOINs for LINQ query.
			/// Attempted optimizations:
			/// - removes duplicate joins by unique target table key;
			/// - removes self-joins by unique key;
			/// - removes left joins if joined table is not used in query.
			/// Default value: <c>true</c>.
			/// </summary>
			public static bool OptimizeJoins
			{
				get => Options.OptimizeJoins;
				set
				{
					if (Options.OptimizeJoins != value)
						Options = Options with { OptimizeJoins = value };
				}
			}

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
			public static CompareNulls CompareNulls
			{
				get => Options.CompareNulls;
				set
				{
					if (Options.CompareNulls != value)
						Options = Options with { CompareNulls = value };
				}
			}

			[Obsolete("Use CompareNulls instead: true maps to LikeClr and false to LikeSqlExceptParameters"), EditorBrowsable(EditorBrowsableState.Never)]
			public static bool CompareNullsAsValues
			{
				get => CompareNulls == CompareNulls.LikeClr;
				set => CompareNulls = value ? CompareNulls.LikeClr : CompareNulls.LikeSqlExceptParameters;
			}

			/// <summary>
			/// Controls behavior of LINQ query, which ends with GroupBy call.
			/// - if <c>true</c> - <seealso cref="LinqToDBException"/> will be thrown for such queries;
			/// - if <c>false</c> - eager loading used.
			/// Default value: <c>true</c>.
			/// </summary>
			/// <remarks>
			/// <a href="https://github.com/linq2db/linq2db/issues/365">More details</a>.
			/// </remarks>
			public static bool GuardGrouping
			{
				get => Options.GuardGrouping;
				set
				{
					if (Options.GuardGrouping != value)
						Options = Options with { GuardGrouping = value };
				}
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
			/// to call <see cref="Query{T}.ClearCache"/> method to cleanup cache after queries, that produce severe memory leaks you need to fix.
			/// <para />
			/// <a href="https://github.com/linq2db/linq2db/issues/256">More details</a>.
			/// </summary>
			public static bool DisableQueryCache
			{
				get => Options.DisableQueryCache;
				set
				{
					if (Options.DisableQueryCache != value)
						Options = Options with { DisableQueryCache = value };
				}
			}

			/// <summary>
			/// Specifies timeout when query will be evicted from cache since last execution of query.
			/// Default value is 1 hour.
			/// </summary>
			public static TimeSpan CacheSlidingExpiration
			{
				get => Options.CacheSlidingExpirationOrDefault;
				set
				{
					if (Options.CacheSlidingExpiration != value)
						Options = Options with { CacheSlidingExpiration = value };
				}
			}

			/// <summary>
			/// Used to generate CROSS APPLY or OUTER APPLY if possible.
			/// Default value: <c>true</c>.
			/// </summary>
			// TODO: V7 remove
			[Obsolete("This API doesn't have effect anymore and will be removed in future")]
			public static bool PreferApply { get; set; }

			/// <summary>
			/// Allows SQL generation to automatically transform
			/// <code>SELECT DISTINCT value FROM Table ORDER BY date</code>
			/// Into GROUP BY equivalent if syntax is not supported
			/// Default value: <c>true</c>.
			/// </summary>
			// TODO: V7 remove
			[Obsolete("This API doesn't have effect anymore and will be removed in future")]
			public static bool KeepDistinctOrdered { get; set; }

			/// <summary>
			/// Enables Take/Skip parameterization.
			/// Default value: <c>true</c>.
			/// </summary>
			public static bool ParameterizeTakeSkip
			{
				get => Options.ParameterizeTakeSkip;
				set
				{
					if (Options.ParameterizeTakeSkip != value)
						Options = Options with { ParameterizeTakeSkip = value };
				}
			}

			/// <summary>
			/// If <c>true</c>, user could add new mappings to context mapping schems (<see cref="IDataContext.MappingSchema"/>).
			/// Otherwise <see cref="LinqToDBException"/> will be generated on locked mapping schema edit attempt.
			/// It is not recommended to enable this option as it has performance implications.
			/// Proper approach is to create single <see cref="Mapping.MappingSchema"/> instance once, configure mappings for it and use this <see cref="Mapping.MappingSchema"/> instance for all context instances.
			/// Default value: <c>false</c>.
			/// </summary>
			public static bool EnableContextSchemaEdit
			{
				get => Options.EnableContextSchemaEdit;
				set
				{
					if (Options.EnableContextSchemaEdit != value)
						Options = Options with { EnableContextSchemaEdit = value };
				}
			}
		}

		/// <summary>
		/// SqlServer specific global settings.
		/// </summary>
		[PublicAPI]
		public static class SqlServer
		{
			/// <summary>
			/// if set to <c>true</c>, SchemaProvider uses <see cref="CommandBehavior.SchemaOnly"/> to get metadata.
			/// Otherwise the sp_describe_first_result_set sproc is used.
			/// Default value: <c>false</c>.
			/// </summary>
			public static bool UseSchemaOnlyToGetSchema;
		}

		/// <summary>
		/// Remote context global settings.
		/// </summary>
		[PublicAPI]
		public static class LinqService
		{
			/// <summary>
			/// Controls format of type name, sent over remote context:
			/// - if <c>true</c> - name from <see cref="Type.AssemblyQualifiedName"/> used;
			/// - if <c>false</c> - name from <see cref="Type.FullName"/> used.
			/// Default value: <c>false</c>.
			/// </summary>
			public static bool SerializeAssemblyQualifiedName;

			/// <summary>
			/// Controls behavior of Linq To DB, when it cannot load <see cref="Type"/> by type name on query deserialization:
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
			static volatile RetryPolicyOptions _options = new(
				null,
				MaxRetryCount   : 5,
				MaxDelay        : TimeSpan.FromSeconds(30),
				RandomFactor    : 1.1,
				ExponentialBase : 2,
				Coefficient     : TimeSpan.FromSeconds(1));

			/// <summary>
			/// Default <see cref="RetryPolicyOptions"/> options. Automatically synchronized with other settings in <see cref="RetryPolicy"/> class.
			/// </summary>
			public static  RetryPolicyOptions Options
			{
				get => _options;
				set
				{
					_options = value;
					DataConnection.ResetDefaultOptions();
					DataConnection.ConnectionOptionsByConfigurationString.Clear();
				}
			}

			/// <summary>
			/// Retry policy factory method, used to create retry policy for new <see cref="DataConnection"/> instance.
			/// If factory method is not set, retry policy is not used.
			/// Not set by default.
			/// </summary>
			public static Func<DataConnection,IRetryPolicy?>? Factory
			{
				get => Options.Factory;
				set => Options = Options with { Factory = value };
			}

			/// <summary>
			/// Status of use of default retry policy.
			/// Getter returns <c>true</c> if default retry policy used, and false if custom retry policy used or retry policy is not set.
			/// Setter sets <see cref="Factory"/> to default retry policy factory if <paramref name="value"/> is <c>true</c>, otherwise removes retry policy.
			/// </summary>
			public static bool UseDefaultPolicy
			{
				get => Factory == DefaultRetryPolicyFactory.GetRetryPolicy;
				set => Factory = value ? DefaultRetryPolicyFactory.GetRetryPolicy : null;
			}

			/// <summary>
			/// The default number of retry attempts.
			/// Default value: <c>5</c>.
			/// </summary>
			public static int DefaultMaxRetryCount
			{
				get => Options.MaxRetryCount;
				set => Options = Options with { MaxRetryCount = value };
			}

			/// <summary>
			/// The default maximum time delay between retries, must be nonnegative.
			/// Default value: 30 seconds.
			/// </summary>
			public static TimeSpan DefaultMaxDelay
			{
				get => Options.MaxDelay;
				set => Options = Options with { MaxDelay = value };
			}

			/// <summary>
			/// The default maximum random factor, must not be lesser than 1.
			/// Default value: <c>1.1</c>.
			/// </summary>
			public static double DefaultRandomFactor
			{
				get => Options.RandomFactor;
				set => Options = Options with { RandomFactor = value };
			}

			/// <summary>
			/// The default base for the exponential function used to compute the delay between retries, must be positive.
			/// Default value: <c>2</c>.
			/// </summary>
			public static double DefaultExponentialBase
			{
				get => Options.ExponentialBase;
				set => Options = Options with { ExponentialBase = value };
			}

			/// <summary>
			/// The default coefficient for the exponential function used to compute the delay between retries, must be nonnegative.
			/// Default value: 1 second.
			/// </summary>
			public static TimeSpan DefaultCoefficient
			{
				get => Options.Coefficient;
				set => Options = Options with { Coefficient = value };
			}
		}

		/// <summary>
		/// SQL generation global settings.
		/// </summary>
		[PublicAPI]
		public static class Sql
		{
			static volatile SqlOptions _options = new();

			/// <summary>
			/// Default <see cref="SqlOptions"/> options. Automatically synchronized with other settings in <see cref="Sql"/> class.
			/// </summary>
			public static SqlOptions Options
			{
				get => _options;
				set
				{
					_options = value;
					DataConnection.ResetDefaultOptions();
					DataConnection.ConnectionOptionsByConfigurationString.Clear();
				}
			}

			/// <summary>
			/// Format for association alias.
			/// <para>
			/// Default value: <c>"a_{0}"</c>.
			/// </para>
			/// <example>
			/// In the following query
			/// <code>
			/// var query = from child in db.Child
			///    select new
			///    {
			///       child.ChildID,
			///       child.Parent.Value1
			///    };
			/// </code>
			/// for association <c>Parent</c> will be generated association <c>A_Parent</c> in resulting SQL.
			/// <code>
			/// SELECT
			///	   [child].[ChildID],
			///	   [a_Parent].[Value1]
			/// FROM
			///	   [Child] [child]
			///       LEFT JOIN [Parent] [a_Parent] ON ([child].[ParentID] = [a_Parent].[ParentID])
			/// </code>
			/// </example>
			/// <remarks>
			/// Set this value to <c>null</c> to disable special alias generation queries.
			/// </remarks>
			/// </summary>
			public static string? AssociationAlias
			{
#if SUPPORTS_COMPOSITE_FORMAT
				get => AssociationAliasFormat?.Format;
				set
				{
					AssociationAliasFormat = string.IsNullOrEmpty(value) ? null : CompositeFormat.Parse(value);
				}
#else
				get => AssociationAliasFormat;
				set
				{
					AssociationAliasFormat = string.IsNullOrEmpty(value) ? null : value;
				}
#endif
			}

#if SUPPORTS_COMPOSITE_FORMAT
			internal static CompositeFormat? AssociationAliasFormat { get; private set; } = CompositeFormat.Parse("a_{0}");
#else
			internal static string? AssociationAliasFormat { get; private set; } = "a_{0}";
#endif

			/// <summary>
			/// Indicates whether SQL Builder should generate aliases for final projection.
			/// It is not required for correct query processing but simplifies SQL analysis.
			/// <para>
			/// Default value: <c>false</c>.
			/// </para>
			/// <example>
			/// For the query
			/// <code>
			/// var query = from child in db.Child
			///	   select new
			///	   {
			///       TrackId = child.ChildID,
			///	   };
			/// </code>
			/// When property is <c>true</c>
			/// <code>
			/// SELECT
			///	   [child].[ChildID] as [TrackId]
			/// FROM
			///	   [Child] [child]
			/// </code>
			/// Otherwise alias will be removed
			/// <code>
			/// SELECT
			///	   [child].[ChildID]
			/// FROM
			///	   [Child] [child]
			/// </code>
			/// </example>
			/// </summary>
			public static bool GenerateFinalAliases
			{
				get => Options.GenerateFinalAliases;
				set => Options = Options with { GenerateFinalAliases = value };
			}

			/// <summary>
			/// If <c>true</c>, linq2db will allow any constant expressions in ORDER BY clause.
			/// Default value: <c>false</c>.
			/// </summary>
			public static bool EnableConstantExpressionInOrderBy
			{
				get => Options.EnableConstantExpressionInOrderBy;
				set => Options = Options with { EnableConstantExpressionInOrderBy = value };
			}
		}
	}
}
