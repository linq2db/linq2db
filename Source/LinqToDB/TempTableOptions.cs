using LinqToDB.Data;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.Options;

namespace LinqToDB
{
	/// <summary>
	/// DataOptions sub-record carrying the global defaults for temp-table materialisation of
	/// inline-rows sources. Per-call configuration via
	/// <see cref="Linq.IAsQueryableExceptBuilder{T}.UseTempTable(System.Func{ITempTableConfigBuilder, ITempTableConfigBuilder})"/>
	/// overrides anything captured here.
	/// </summary>
	/// <param name="LocalCollections">
	/// Default applied to <see cref="LinqExtensions.AsQueryable{TElement}(System.Collections.Generic.IEnumerable{TElement},IDataContext)"/>
	/// and its configured-overload sibling. Configured via
	/// <c>DataOptionsExtensions.UseTempTablesForLocalCollections</c>.
	/// </param>
	/// <param name="Contains">
	/// Default applied to <c>Contains(largeCollection)</c> predicates. <strong>API placeholder
	/// in this PR</strong> — the Contains-side optimizer pass that consumes this default ships
	/// in a follow-up PR. Configured via <c>DataOptionsExtensions.UseTempTablesForContains</c>.
	/// </param>
	public sealed record TempTableOptions(
		TempTableSpec? LocalCollections = null,
		TempTableSpec? Contains         = null)
		: IOptionSet
	{
		// Hand-written copy constructor — suppresses the synthesized one so the cached
		// _configurationID below is NOT copied across `with` evolutions. The new instance must
		// recompute its ID against its (possibly updated) field values. Mirrors the pattern at
		// LinqOptions.cs:187. Without this, `existing with { LocalCollections = newSpec }` would
		// reuse the source's stale cached ID and break query-cache invalidation.
		TempTableOptions(TempTableOptions original)
		{
			LocalCollections = original.LocalCollections;
			Contains         = original.Contains;
		}

		#region Default Options

		static TempTableOptions _default = new();

		/// <summary>Gets default <see cref="TempTableOptions"/> instance.</summary>
		public static TempTableOptions Default
		{
			get => _default;
			set
			{
				_default = value;
				DataConnection.ResetDefaultOptions();
				DataConnection.ConnectionOptionsByConfigurationString.Clear();
			}
		}

		/// <inheritdoc />
		IOptionSet IOptionSet.Default => Default;

		#endregion

		#region IConfigurationID

		// The LINQ query cache keys cached Query<T> instances by DataOptions.ConfigurationID.
		// Every cache-affecting field of this sub-record must contribute to the ID — otherwise
		// changing UseTempTablesForLocalCollections / UseTempTablesForContains between two
		// executes would reuse the stale cached translation and silently ignore the new spec.
		int? _configurationID;
		int IConfigurationID.ConfigurationID
		{
			get
			{
				if (_configurationID == null)
				{
					using var idBuilder = new IdentifierBuilder();
					_configurationID = idBuilder
						.Add(LocalCollections?.Threshold)
						.Add(LocalCollections?.DisposeWithConnection)
						.Add(LocalCollections?.BulkCopyOptions)  // BulkCopyOptions itself is IConfigurationID
						.Add(Contains?.Threshold)
						.Add(Contains?.DisposeWithConnection)
						.Add(Contains?.BulkCopyOptions)
						.CreateID();
				}

				return _configurationID.Value;
			}
		}

		#endregion
	}
}
