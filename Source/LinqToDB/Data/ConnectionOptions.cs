using System;
using System.Data.Common;

using LinqToDB.Common;
using LinqToDB.Common.Internal;
using LinqToDB.DataProvider;
using LinqToDB.Interceptors;
using LinqToDB.Mapping;
using LinqToDB.Remote;

namespace LinqToDB.Data
{
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
	/// If <c>true</c>, <see cref="DataConnection"/> will dispose provided/created connection on own dispose.
	/// If <c>null</c>, <see cref="DataConnection"/> will dispose connection on own dispose only if it created by <see cref="DataConnection"/> instance.
	/// </param>
	/// <param name="ConnectionFactory">
	/// Gets connection factory to use with <see cref="DataConnection"/> instance. Accepts current context <see cref="DataOptions" /> settings.
	/// </param>
	/// <param name="DataProviderFactory">
	/// Gets <see cref="IDataProvider"/> factory to use with <see cref="DataConnection"/> instance.
	/// </param>
	/// <param name="ConnectionInterceptor">
	/// Connection interceptor to support connection configuration before or right after connection opened.
	/// </param>
	/// <param name="OnEntityDescriptorCreated">
	/// Action, called on entity descriptor creation.
	/// Allows descriptor modification.
	/// When not specified, application-wide callback <see cref="MappingSchema.EntityDescriptorCreatedCallback"/> called.
	/// </param>
	public sealed record ConnectionOptions
	(
		string?                                         ConfigurationString       = default,
		string?                                         ConnectionString          = default,
		IDataProvider?                                  DataProvider              = default,
		string?                                         ProviderName              = default,
		MappingSchema?                                  MappingSchema             = default,
		DbConnection?                                   DbConnection              = default,
		DbTransaction?                                  DbTransaction             = default,
		bool?                                           DisposeConnection         = default,
		Func<DataOptions,DbConnection>?                 ConnectionFactory         = default,
		Func<ConnectionOptions, IDataProvider>?         DataProviderFactory       = default,
		ConnectionOptionsConnectionInterceptor?         ConnectionInterceptor     = default,
		Action<MappingSchema, IEntityChangeDescriptor>? OnEntityDescriptorCreated = default

		// If you add another parameter here, don't forget to update
		// ConnectionOptions copy constructor and IConfigurationID.ConfigurationID.
	)
		: IOptionSet,
			IApplicable<DataConnection>, IApplicable<DataContext>, IApplicable<RemoteDataContextBase>,
			IReapplicable<DataConnection>, IReapplicable<DataContext>, IReapplicable<RemoteDataContextBase>
	{
		public ConnectionOptions() : this((string?)null)
		{
		}

		ConnectionOptions(ConnectionOptions original)
		{
			ConfigurationString       = original.ConfigurationString;
			ConnectionString          = original.ConnectionString;
			DataProvider              = original.DataProvider;
			ProviderName              = original.ProviderName;
			MappingSchema             = original.MappingSchema;
			DbConnection              = original.DbConnection;
			DbTransaction             = original.DbTransaction;
			DisposeConnection         = original.DisposeConnection;
			ConnectionFactory         = original.ConnectionFactory;
			DataProviderFactory       = original.DataProviderFactory;
			ConnectionInterceptor     = original.ConnectionInterceptor;
			OnEntityDescriptorCreated = original.OnEntityDescriptorCreated;
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
						.Add(ConnectionInterceptor)
						.Add(OnEntityDescriptorCreated)
						.CreateID();
				}

				return _configurationID.Value;
			}
		}

		static readonly ConnectionOptions _empty = new();

		IOptionSet IOptionSet.Default => _empty;

		internal IDataProvider? SavedDataProvider;
		internal MappingSchema? SavedMappingSchema;
		internal string?        SavedConnectionString;
		internal string?        SavedConfigurationString;
		internal bool           SavedEnableContextSchemaEdit;

		#region IApplicable implementation

		void IApplicable<DataConnection>.Apply(DataConnection obj)
		{
			DataConnection.ConfigurationApplier.Apply(obj, this);
		}

		void IApplicable<DataContext>.Apply(DataContext obj)
		{
			DataContext.ConfigurationApplier.Apply(obj, this);
		}

		void IApplicable<RemoteDataContextBase>.Apply(RemoteDataContextBase obj)
		{
			RemoteDataContextBase.ConfigurationApplier.Apply(obj, this);
		}

		#endregion

		#region IReapplicable implementation

		Action? IReapplicable<DataConnection>.Apply(DataConnection obj, object? previousObject)
		{
			return ((IConfigurationID)this).ConfigurationID == ((IConfigurationID?)previousObject)?.ConfigurationID
				? null
				: DataConnection.ConfigurationApplier.Reapply(obj, this, (ConnectionOptions?)previousObject);
		}

		Action? IReapplicable<DataContext>.Apply(DataContext obj, object? previousObject)
		{
			return ((IConfigurationID)this).ConfigurationID == ((IConfigurationID?)previousObject)?.ConfigurationID
				? null
				: DataContext.ConfigurationApplier.Reapply(obj, this, (ConnectionOptions?)previousObject);
		}

		Action? IReapplicable<RemoteDataContextBase>.Apply(RemoteDataContextBase obj, object? previousObject)
		{
			return ((IConfigurationID)this).ConfigurationID == ((IConfigurationID?)previousObject)?.ConfigurationID
				? null
				: RemoteDataContextBase.ConfigurationApplier.Reapply(obj, this, (ConnectionOptions?)previousObject);
		}

		#endregion

		#region IEquatable implementation

		public bool Equals(ConnectionOptions? other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;

			return ((IConfigurationID)this).ConfigurationID == ((IConfigurationID)other).ConfigurationID;
		}

		public override int GetHashCode()
		{
			return ((IConfigurationID)this).ConfigurationID;
		}

		#endregion
	}
}
