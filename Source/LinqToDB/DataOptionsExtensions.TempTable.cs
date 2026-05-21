using System;

using JetBrains.Annotations;

using LinqToDB.Internal.Common;

// ReSharper disable once CheckNamespace
namespace LinqToDB
{
	public static partial class DataOptionsExtensions
	{
		#region TempTableOptions

		/// <summary>
		/// Sets the default <see cref="TempTableSpec"/> applied to
		/// <see cref="LinqExtensions.AsQueryable{TElement}(System.Collections.Generic.IEnumerable{TElement},IDataContext)"/>
		/// and its configured-overload sibling when the caller does not chain
		/// <see cref="Linq.IAsQueryableExceptBuilder{T}.UseTempTable(System.Func{ITempTableConfigBuilder, ITempTableConfigBuilder})"/>.
		/// Per-call configuration on the AsQueryable chain wins per-property; this default fills in any
		/// fields the per-call chain leaves unset.
		/// </summary>
		[Pure]
		public static DataOptions UseTempTablesForLocalCollections(
			this DataOptions                                       options,
			Func<ITempTableConfigBuilder, ITempTableConfigBuilder> configure)
		{
			ArgumentNullException.ThrowIfNull(configure);

			var spec     = BuildTempTableSpec(configure);
			var existing = options.TempTableOptions;

			return options.WithOptions(existing with { LocalCollections = spec });
		}

		/// <summary>
		/// Sets the default <see cref="TempTableSpec"/> for <c>Contains(largeCollection)</c>
		/// predicates. <strong>API placeholder in this PR</strong> — the Contains-side optimizer
		/// pass that consumes this default ships in a follow-up PR. Setting it today populates
		/// the <see cref="TempTableOptions.Contains"/> slot and participates in the
		/// <see cref="DataOptions"/> cache key, but does not yet rewrite the emitted <c>IN (…)</c>
		/// predicate.
		/// </summary>
		[Pure]
		public static DataOptions UseTempTablesForContains(
			this DataOptions                                       options,
			Func<ITempTableConfigBuilder, ITempTableConfigBuilder> configure)
		{
			ArgumentNullException.ThrowIfNull(configure);

			var spec     = BuildTempTableSpec(configure);
			var existing = options.TempTableOptions;

			return options.WithOptions(existing with { Contains = spec });
		}

		static TempTableSpec BuildTempTableSpec(Func<ITempTableConfigBuilder, ITempTableConfigBuilder> configure)
		{
			var builder = new TempTableConfigBuilderImpl();
			configure(builder);
			return builder.Build();
		}

		#endregion
	}
}
