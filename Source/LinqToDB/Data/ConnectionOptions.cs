using System;
using System.Data.Common;

namespace LinqToDB.Data
{
	using Common;
	using Common.Internal;
	using DataProvider;
	using Mapping;

	/// <param name="ConfigurationString">
	/// Gets configuration string name to use with <see cref="DataConnection"/> instance.
	/// </param>
	/// <param name="ConnectionString">
	/// The connection string, or <c>null</c> if a <see cref="DbConnection" /> was used instead of a connection string.
	/// </param>
	/// <param name="ProviderName">
	/// Gets provider name to use with <see cref="DataConnection"/> instance.
	/// </param>
	/// <param name="MappingSchema">
	/// Gets <see cref="MappingSchema"/> instance to use with <see cref="DataConnection"/> instance.
	/// </param>
	/// <param name="DbConnection">
	/// Gets <see cref="DbConnection"/> instance to use with <see cref="DataConnection"/> instance.
	/// </param>
	/// <param name="DisposeConnection">
	/// Gets <see cref="DbConnection"/> ownership status for <see cref="DataConnection"/> instance.
	/// If <c>true</c>, <see cref="DataConnection"/> will dispose provided connection on own dispose.
	/// </param>
	/// <param name="ConnectionFactory">
	/// Gets connection factory to use with <see cref="DataConnection"/> instance.
	/// </param>
	public sealed record ConnectionOptions
	(
		string?              ConfigurationString,
		string?              ConnectionString    = default,
		IDataProvider?       DataProvider        = default,
		string?              ProviderName        = default,
		MappingSchema?       MappingSchema       = default,
		DbConnection?        DbConnection        = default,
		DbTransaction?       DbTransaction       = default,
		bool                 DisposeConnection   = default,
		Func<DbConnection>?  ConnectionFactory   = default,
		Func<IDataProvider>? DataProviderFactory = default
	)
		: IOptionSet, IApplicable<DataConnection>, IApplicable<DataContext>
	{
		public ConnectionOptions() : this((string?)null)
		{
		}

		ConnectionOptions(ConnectionOptions original)
		{
			ConfigurationString = original.ConfigurationString;
			ConnectionString    = original.ConnectionString;
			DataProvider        = original.DataProvider;
			ProviderName        = original.ProviderName;
			MappingSchema       = original.MappingSchema;
			DbConnection        = original.DbConnection;
			DbTransaction       = original.DbTransaction;
			DisposeConnection   = original.DisposeConnection;
			ConnectionFactory   = original.ConnectionFactory;
			DataProviderFactory = original.DataProviderFactory;
		}

		int? _configurationID;
		int IConfigurationID.ConfigurationID => _configurationID ??= new IdentifierBuilder()
			.Add(ConfigurationString)
			.Add(ConnectionString)
			.Add(DataProvider?.ID)
			.Add(ProviderName)
			.Add(MappingSchema)
			.Add(DbConnection?.ConnectionString)
			.Add(DbTransaction?.Connection?.ConnectionString)
			.Add(DataProviderFactory)
			.CreateID();

		internal IDataProvider? SavedDataProvider;
		internal MappingSchema? SavedMappingSchema;
		internal string?        SavedConnectionString;
		internal string?        SavedConfigurationString;
		internal bool           SavedEnableAutoFluentMapping;

		void IApplicable<DataConnection>.Apply(DataConnection obj)
		{
			DataConnection.ConfigurationApplier.Apply(obj, this);
		}

		void IApplicable<DataContext>.Apply(DataContext obj)
		{
			DataContext.ConfigurationApplier.Apply(obj, this);
		}

		#region IEquatable implementation

		public bool Equals(ConnectionOptions? other)
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
