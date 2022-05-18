using System;
using System.Data.Common;

namespace LinqToDB.Data
{
	using Common.Internal;
	using DataProvider;
	using Infrastructure;

	/// <param name="ConfigurationString">
	/// Gets configuration string name to use with <see cref="DataConnection"/> instance.
	/// </param>
	/// <param name="ConnectionString">
	/// The connection string, or <c>null</c> if a <see cref="DbConnection" /> was used instead of a connection string.
	/// </param>
	/// <param name="ProviderName">
	/// Gets provider name to use with <see cref="DataConnection"/> instance.
	/// </param>
	public sealed record DataConnectionOptions(
		string?        ConfigurationString,
		string?        ConnectionString,
		IDataProvider? DataProvider,
		string?        ProviderName
	) : IOptionSet, IApplicable<DataConnection>
	{
		public DataConnectionOptions() : this(
			ConfigurationString : null,
			ConnectionString    : null,
			DataProvider        : null,
			ProviderName        : null)
		{
		}

		int? _configurationID;
		int IOptionSet.ConfigurationID => _configurationID ??= new IdentifierBuilder()
			.Add(ConfigurationString)
			.Add(ConnectionString)
			//.Add(DataProvider?.ID)
			.Add(ProviderName)
			.CreateID();

		public IDataProvider? SavedDataProvider     { get; set; }
		public string?        SavedConnectionString { get; set; }


		void IApplicable<DataConnection>.Apply(DataConnection dataConnection)
		{
			DataConnection.ConfigurationApplier.Apply(dataConnection, this);
		}

		#region IEquatable implementation

		public bool Equals(DataConnectionOptions? other)
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
