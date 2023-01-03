using System;
using System.Collections.Generic;

namespace LinqToDB.DataProvider
{
	using Common;
	using Common.Internal;
	using Data;

	/// <param name="BulkCopyType">
	/// Default bulk copy mode, used by <see cref="DataConnectionExtensions.BulkCopy{T}(DataConnection, IEnumerable{T})"/>
	/// methods, if mode is not specified explicitly.
	/// </param>
	public abstract record DataProviderOptions<T>
	(
		BulkCopyType BulkCopyType
	)
		: IOptionSet
		where T : DataProviderOptions<T>, new()
	{
		protected DataProviderOptions() : this(BulkCopyType.Default)
		{
		}

		protected DataProviderOptions(DataProviderOptions<T> original)
		{
			BulkCopyType = original.BulkCopyType;
		}

		int? _configurationID;
		int IConfigurationID.ConfigurationID => _configurationID ??= CreateID(new IdentifierBuilder().Add(BulkCopyType)).CreateID();

		protected abstract IdentifierBuilder CreateID(IdentifierBuilder builder);

		/// <summary>
		/// Default options.
		/// </summary>
		public static T Default { get; set; } = new();
	}
}
