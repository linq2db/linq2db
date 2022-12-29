using System;

namespace LinqToDB.DataProvider
{
	using Common;
	using Common.Internal;
	using Data;

	/// <param name="BulkCopyType">
	/// BulkCopyType used by Data Provider by default.
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
		/// Default value: <c>OracleOptions(BulkCopyType.MultipleRows, AlternativeBulkCopy.InsertAll)</c>
		/// </summary>
		public static T Default { get; set; } = new();
	}
}
