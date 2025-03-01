using System;
using System.Data.Common;

using LinqToDB.Common;
using LinqToDB.Configuration;
using LinqToDB.DataProvider;
using LinqToDB.Interceptors;
using LinqToDB.Internal.Common;
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
	/// If <c>true</c>, <see cref="DataConnection"/> will dispose provided connection on own dispose.
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
		bool                                            DisposeConnection         = default,
		Func<DataOptions, DbConnection>?                ConnectionFactory         = default,
		Func<ConnectionOptions, IDataProvider>?         DataProviderFactory       = default,
		ConnectionOptionsConnectionInterceptor?         ConnectionInterceptor     = default,
		Action<MappingSchema, IEntityChangeDescriptor>? OnEntityDescriptorCreated = default
		// If you add another parameter here, don't forget to update
		// ConnectionOptions copy constructor and IConfigurationID.ConfigurationID.
	)
		: IOptionSet, IApplicable<DataConnection>, IApplicable<DataContext>, IApplicable<RemoteDataContextBase>
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

		void IApplicable<RemoteDataContextBase>.Apply(RemoteDataContextBase obj)
		{
			RemoteDataContextBase.ConfigurationApplier.Apply(obj, this);
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
