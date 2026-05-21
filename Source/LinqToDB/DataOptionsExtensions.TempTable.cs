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
		/// Sets the default <see cref="TempTableSpec"/> applied to
		/// <c>localCollection.Contains(value)</c> predicates where the local collection is
		/// materialised on the client (<see cref="System.Collections.Generic.List{T}"/>, array,
		/// <see cref="System.Collections.Generic.HashSet{T}"/>, etc.). When the runtime collection
		/// size exceeds <see cref="TempTableSpec.Threshold"/>, the predicate is rewritten to read
		/// from a temp table BULK-inserted before the main query:
		/// <list type="bullet">
		/// <item>Scalar element type → <c>IN (SELECT item FROM &lt;temp&gt;)</c>.</item>
		/// <item>Entity / composite-PK element type → <c>EXISTS (SELECT 1 FROM &lt;temp&gt; t
		/// WHERE t.&lt;pk0&gt; = outer.&lt;pk0&gt; AND …)</c>, using the entity's own
		/// column conversions / DataType overrides on both sides of the comparison.</item>
		/// </list>
		/// Below the threshold the predicate stays on the regular inline <c>IN</c> / OR-AND
		/// chain. Silently inert on providers that don't support runtime temp tables. Only the
		/// parameter-backed path is rewritten — compile-time literal arrays
		/// (<c>new[] { 1, 2, 3 }.Contains(col)</c>) stay inline. Anonymous-composite shapes
		/// (<c>list.Contains(new { col1, col2 })</c>) stay inline regardless of this setting
		/// (no <c>EntityDescriptor</c> is available to drive a typed temp table). Temp-table
		/// creation requires a local <see cref="LinqToDB.Data.DataConnection"/> /
		/// <c>DataContext</c>; the rewrite isn't supported on remote LinqService contexts.
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
