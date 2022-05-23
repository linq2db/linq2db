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
		string?             ConfigurationString,
		string?             ConnectionString    = default,
		IDataProvider?      DataProvider        = default,
		string?             ProviderName        = default,
		DbConnection?       DbConnection        = default,
		DbTransaction?      DbTransaction       = default,
		bool                DisposeConnection   = default,
		Func<DbConnection>? ConnectionFactory   = default
	) : IOptionSet, IApplicable<DataConnection>, IApplicable<DataContext>
	{
		public ConnectionOptions() : this((string?)null)
		{
		}

		int? _configurationID;
		int IOptionSet.ConfigurationID => _configurationID ??= new IdentifierBuilder()
			.Add(ConfigurationString)
			.Add(ConnectionString)
			.Add(DataProvider?.ID)
			.Add(ProviderName)
			.Add(DbConnection?.ConnectionString)
			.Add(DbTransaction?.Connection?.ConnectionString)
			.CreateID();

		public IDataProvider? SavedDataProvider     { get; set; }
		public string?        SavedConnectionString { get; set; }

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
