using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

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
	/// <param name="DataProvider">
	/// Gets optional <see cref="IDataProvider"/> implementation to use with connection.
	/// </param>
	/// <param name="ProviderName">
	/// Gets optional provider name to use with <see cref="DataConnection"/> instance.
	/// </param>
	/// <param name="MappingSchema">
	/// Gets optional <see cref="MappingSchema"/> instance to use with <see cref="DataConnection"/> instance.
	/// </param>
	/// <param name="DbConnection">
	/// Gets optional <see cref="DbConnection"/> instance to use with <see cref="DataConnection"/> instance.
	/// </param>
	/// <param name="DbTransaction">
	/// Gets optional <see cref="DbTransaction"/> instance to use with <see cref="DataConnection"/> instance.
	/// </param>
	/// <param name="DisposeConnection">
	/// Gets <see cref="DbConnection"/> ownership status for <see cref="DataConnection"/> instance.
	/// If <c>true</c>, <see cref="DataConnection"/> will dispose provided connection on own dispose.
	/// </param>
	/// <param name="ConnectionFactory">
	/// Gets connection factory to use with <see cref="DataConnection"/> instance. Accepts current context <see cref="DataOptions" /> settings.
	/// </param>
	/// <param name="DataProviderFactory">
	/// Gets <see cref="IDataProvider"/> factory to use with <see cref="DataConnection"/> instance.
	/// </param>
	/// <param name="AfterConnectionCreated">
	/// Action, executed for newly-created connection instance. Accepts created connection instance as parameter.
	/// </param>
	/// <param name="AfterConnectionCreatedAsync">
	/// Action, executed for newly-created connection instance from async execution path. Accepts created connection instance as parameter.
	/// If this option is not set, <paramref name="AfterConnectionCreated"/> synchronous action called.
	/// Use this option only if you need to perform async work from action, otherwise <paramref name="AfterConnectionCreated"/> is sufficient.
	/// </param>
	/// <param name="AfterConnectionOpened">
	/// Action, executed for connection instance after <see cref="DbConnection.Open"/> call.
	/// Also called after <see cref="DbConnection.OpenAsync(CancellationToken)"/> call if <paramref name="AfterConnectionOpenedAsync"/> action is not provided.
	/// Accepts connection instance as parameter.
	/// </param>
	/// <param name="AfterConnectionOpenedAsync">
	/// Action, executed for connection instance from async execution path after <see cref="DbConnection.OpenAsync(CancellationToken)"/> call.
	/// Accepts connection instance as parameter.
	/// If this option is not set, <paramref name="AfterConnectionOpened"/> synchronous action called.
	/// Use this option only if you need to perform async work from action, otherwise <paramref name="AfterConnectionOpened"/> is sufficient.
	/// </param>
	public sealed record ConnectionOptions
	(
		string?                                      ConfigurationString         = default,
		string?                                      ConnectionString            = default,
		IDataProvider?                               DataProvider                = default,
		string?                                      ProviderName                = default,
		MappingSchema?                               MappingSchema               = default,
		DbConnection?                                DbConnection                = default,
		DbTransaction?                               DbTransaction               = default,
		bool                                         DisposeConnection           = default,
		Func<DataOptions, DbConnection>?             ConnectionFactory           = default,
		Func<IDataProvider>?                         DataProviderFactory         = default,
		Action<DbConnection>?                        AfterConnectionCreated      = default,
		Func<DbConnection, CancellationToken, Task>? AfterConnectionCreatedAsync = default,
		Action<DbConnection>?                        AfterConnectionOpened       = default,
		Func<DbConnection, CancellationToken, Task>? AfterConnectionOpenedAsync  = default
	)
		: IOptionSet, IApplicable<DataConnection>, IApplicable<DataContext>
	{
		public ConnectionOptions() : this((string?)null)
		{
		}

		ConnectionOptions(ConnectionOptions original)
		{
			ConfigurationString         = original.ConfigurationString;
			ConnectionString            = original.ConnectionString;
			DataProvider                = original.DataProvider;
			ProviderName                = original.ProviderName;
			MappingSchema               = original.MappingSchema;
			DbConnection                = original.DbConnection;
			DbTransaction               = original.DbTransaction;
			DisposeConnection           = original.DisposeConnection;
			ConnectionFactory           = original.ConnectionFactory;
			DataProviderFactory         = original.DataProviderFactory;
			AfterConnectionCreated      = original.AfterConnectionCreated;
			AfterConnectionCreatedAsync = original.AfterConnectionCreatedAsync;
			AfterConnectionOpened       = original.AfterConnectionOpened;
			AfterConnectionOpenedAsync  = original.AfterConnectionOpenedAsync;
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
						.Add(ConfigurationString)
						.Add(ConnectionString)
						.Add(DataProvider?.ID)
						.Add(ProviderName)
						.Add(MappingSchema)
						.Add(DbConnection?.ConnectionString)
						.Add(DbTransaction?.Connection?.ConnectionString)
						.Add(DisposeConnection)
						.Add(ConnectionFactory)
						.Add(DataProviderFactory)
						.Add(AfterConnectionCreated)
						.Add(AfterConnectionCreatedAsync)
						.Add(AfterConnectionOpened)
						.Add(AfterConnectionOpenedAsync)
						.CreateID();
				}

				return _configurationID.Value;
			}
		}

		internal IDataProvider? SavedDataProvider;
		internal MappingSchema? SavedMappingSchema;
		internal string?        SavedConnectionString;
		internal string?        SavedConfigurationString;
		internal bool           SavedEnableContextSchemaEdit;

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
