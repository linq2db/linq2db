using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Text;

namespace LinqToDB.Infrastructure
{
	using Data;
	using DataProvider;
	using Mapping;

	/// <summary>
	/// <para>
	/// Represents options managed by the core of linq2db, as opposed to those managed
	/// by database providers or extensions. These options are set using <see cref="DataContextOptionsBuilder" />.
	/// </para>
	/// <para>
	/// Instances of this class are designed to be immutable. To change an option, call one of the 'With...'
	/// methods to obtain a new instance with the option changed.
	/// </para>
	/// </summary>
	public class DataContextOptionsExtension : IDataContextOptionsExtension
	{
		DataContextOptionsExtensionInfo?    _info;
		string?                             _connectionString;
		string?                             _configurationString;
		DbConnection?                       _dbConnection;
		string?                             _providerName;
		IDataProvider?                      _dataProvider;
		int?                                _commandTimeout;
		bool                                _disposeConnection;
		MappingSchema?                      _mappingSchema;
		Func<DbConnection>?                 _connectionFactory;
		DbTransaction?                      _transaction;
		TraceLevel?                         _traceLevel;
		Action<TraceInfo>?                  _onTrace;
		Action<string?,string?,TraceLevel>? _writeTrace;

		/// <summary>
		/// Creates a new set of options with everything set to default values.
		/// </summary>
		public DataContextOptionsExtension()
		{
		}

		/// <summary>
		/// Called by a derived class constructor when implementing the <see cref="Clone" /> method.
		/// </summary>
		/// <param name="copyFrom"> The instance that is being cloned. </param>
		protected DataContextOptionsExtension(DataContextOptionsExtension copyFrom)
		{
			_connectionString    = copyFrom._connectionString;
			_configurationString = copyFrom._configurationString;
			_dbConnection        = copyFrom._dbConnection;
			_providerName        = copyFrom._providerName;
			_dataProvider        = copyFrom._dataProvider;
			_commandTimeout      = copyFrom._commandTimeout;
			_disposeConnection   = copyFrom._disposeConnection;
			_mappingSchema       = copyFrom._mappingSchema;
			_connectionFactory   = copyFrom._connectionFactory;
			_transaction         = copyFrom._transaction;
			_traceLevel          = copyFrom._traceLevel;
			_onTrace             = copyFrom._onTrace;
			_writeTrace          = copyFrom._writeTrace;
		}

		/// <summary>
		/// Information/metadata about the extension.
		/// </summary>
		public virtual DataContextOptionsExtensionInfo Info => _info ??= new ExtensionInfo(this);

		/// <summary>
		/// Override this method in a derived class to ensure that any clone created is also of that class.
		/// </summary>
		/// <returns>A clone of this instance, which can be modified before being returned as immutable.</returns>
		protected virtual DataContextOptionsExtension Clone() => new DataContextOptionsExtension(this);

		DataContextOptionsExtension SetValue(Action<DataContextOptionsExtension> setter)
		{
			var clone = Clone();

			setter(clone);

			return clone;
		}

		/// <summary>
		/// Gets <see cref="MappingSchema"/> instance to use with <see cref="DataConnection"/> instance.
		/// </summary>
		public virtual MappingSchema? MappingSchema => _mappingSchema;

		/// <summary>
		/// Gets <see cref="IDataProvider"/> instance to use with <see cref="DataConnection"/> instance.
		/// </summary>
		public virtual IDataProvider? DataProvider => _dataProvider;

		/// <summary>
		///     The connection string, or <c>null</c> if a <see cref="DbConnection" /> was used instead of
		///     a connection string.
		/// </summary>
		public virtual string? ConnectionString => _connectionString;

		/// <summary>
		/// Gets configuration string name to use with <see cref="DataConnection"/> instance.
		/// </summary>
		public virtual string? ConfigurationString => _configurationString;

		/// <summary>
		/// Gets provider name to use with <see cref="DataConnection"/> instance.
		/// </summary>
		public virtual string? ProviderName => _providerName;

		/// <summary>
		/// Gets <see cref="System.Data.Common.DbConnection"/> instance to use with <see cref="DataConnection"/> instance.
		/// </summary>
		public virtual DbConnection? DbConnection => _dbConnection;

		/// <summary>
		/// Gets <see cref="DbConnection"/> ownership status for <see cref="DataConnection"/> instance.
		/// If <c>true</c>, <see cref="DataConnection"/> will dispose provided connection on own dispose.
		/// </summary>
		public virtual bool DisposeConnection => _disposeConnection;

		/// <summary>
		///     The command timeout, or <c>null</c> if none has been set.
		/// </summary>
		public virtual int? CommandTimeout => _commandTimeout;

		/// <summary>
		/// Gets custom trace method to use with <see cref="DataConnection"/> instance.
		/// </summary>
		public virtual Action<TraceInfo>? OnTrace => _onTrace;

		/// <summary>
		/// Gets custom trace level to use with <see cref="DataConnection"/> instance.
		/// </summary>
		public virtual TraceLevel? TraceLevel => _traceLevel;

		/// <summary>
		/// Gets custom trace writer to use with <see cref="DataConnection"/> instance.
		/// </summary>
		public virtual Action<string?,string?,TraceLevel>? WriteTrace => _writeTrace;

		/// <summary>
		/// Gets connection factory to use with <see cref="DataConnection"/> instance.
		/// </summary>
		public Func<DbConnection>? ConnectionFactory => _connectionFactory;

		public DbTransaction? DbTransaction => _transaction;

		/// <summary>
		/// Creates a new instance with all options the same as for this instance, but with the given option changed.
		/// It is unusual to call this method directly. Instead use <see cref="DataContextOptionsBuilder" />.
		/// </summary>
		/// <param name="connectionString"> The option to change. </param>
		/// <returns> A new instance with the option changed. </returns>
		public virtual DataContextOptionsExtension WithConnectionString(string? connectionString)
		{
			return SetValue(o => o._connectionString = connectionString);
		}

		/// <summary>
		/// Creates a new instance with all options the same as for this instance, but with the given option changed.
		/// It is unusual to call this method directly. Instead use <see cref="DataContextOptionsBuilder" />.
		/// </summary>
		/// <param name="configurationString"> The option to change. </param>
		/// <returns> A new instance with the option changed. </returns>
		public virtual DataContextOptionsExtension WithConfigurationString(string? configurationString)
		{
			return SetValue(o => o._configurationString = configurationString);
		}

		/// <summary>
		/// Creates a new instance with all options the same as for this instance, but with the given option changed.
		/// It is unusual to call this method directly. Instead use <see cref="DataContextOptionsBuilder" />.
		/// </summary>
		/// <param name="connection"> The option to change. </param>
		/// <returns> A new instance with the option changed. </returns>
		public virtual DataContextOptionsExtension WithConnection(DbConnection? connection)
		{
			return SetValue(o => o._dbConnection = connection);
		}

		/// <summary>
		/// Creates a new instance with all options the same as for this instance, but with the given option changed.
		/// It is unusual to call this method directly. Instead use <see cref="DataContextOptionsBuilder" />.
		/// </summary>
		/// <param name="disposeConnection"> The option to change. </param>
		/// <returns> A new instance with the option changed. </returns>
		public virtual DataContextOptionsExtension WithDisposeConnection(bool disposeConnection)
		{
			return SetValue(o => o._disposeConnection = disposeConnection);
		}

		/// <summary>
		/// Creates a new instance with all options the same as for this instance, but with the given option changed.
		/// It is unusual to call this method directly. Instead use <see cref="DataContextOptionsBuilder" />.
		/// </summary>
		/// <param name="providerName"> The option to change. </param>
		/// <returns> A new instance with the option changed. </returns>
		public virtual DataContextOptionsExtension WithProviderName(string? providerName)
		{
			return SetValue(o => o._providerName = providerName);
		}

		/// <summary>
		/// Creates a new instance with all options the same as for this instance, but with the given option changed.
		/// It is unusual to call this method directly. Instead use <see cref="DataContextOptionsBuilder" />.
		/// </summary>
		/// <param name="dataProvider"> The option to change. </param>
		/// <returns> A new instance with the option changed. </returns>
		public virtual DataContextOptionsExtension WithDataProvider(IDataProvider? dataProvider)
		{
			return SetValue(o => o._dataProvider = dataProvider);
		}

		/// <summary>
		/// Creates a new instance with all options the same as for this instance, but with the given option changed.
		/// It is unusual to call this method directly. Instead use <see cref="DataContextOptionsBuilder" />.
		/// </summary>
		/// <param name="mappingSchema"> The option to change. </param>
		/// <returns> A new instance with the option changed. </returns>
		public virtual DataContextOptionsExtension WithMappingSchema(MappingSchema? mappingSchema)
		{
			return SetValue(o => o._mappingSchema = mappingSchema);
		}

		/// <summary>
		/// Creates a new instance with all options the same as for this instance, but with the given option changed.
		/// It is unusual to call this method directly. Instead use <see cref="DataContextOptionsBuilder" />.
		/// </summary>
		/// <param name="connectionFactory"> The option to change. </param>
		/// <returns> A new instance with the option changed. </returns>
		public virtual DataContextOptionsExtension WithConnectionFactory(Func<DbConnection>? connectionFactory)
		{
			return SetValue(o => o._connectionFactory = connectionFactory);
		}

		/// <summary>
		/// Creates a new instance with all options the same as for this instance, but with the given option changed.
		/// It is unusual to call this method directly. Instead use <see cref="DataContextOptionsBuilder" />.
		/// </summary>
		/// <param name="transaction"> The option to change. </param>
		/// <returns> A new instance with the option changed. </returns>
		public virtual DataContextOptionsExtension WithTransaction(DbTransaction? transaction)
		{
			return SetValue(o => o._transaction = transaction);
		}

		/// <summary>
		/// Creates a new instance with all options the same as for this instance, but with the given option changed.
		/// It is unusual to call this method directly. Instead use <see cref="DataContextOptionsBuilder" />.
		/// </summary>
		/// <param name="traceLevel"> The option to change. </param>
		/// <returns> A new instance with the option changed. </returns>
		public virtual DataContextOptionsExtension WithTraceLevel(TraceLevel traceLevel)
		{
			return SetValue(o => o._traceLevel = traceLevel);
		}

		/// <summary>
		/// Creates a new instance with all options the same as for this instance, but with the given option changed.
		/// It is unusual to call this method directly. Instead use <see cref="DataContextOptionsBuilder" />.
		/// </summary>
		/// <param name="onTrace"> The option to change. </param>
		/// <returns> A new instance with the option changed. </returns>
		public virtual DataContextOptionsExtension WithTracing(Action<TraceInfo>? onTrace)
		{
			return SetValue(o => o._onTrace = onTrace);
		}

		/// <summary>
		/// Creates a new instance with all options the same as for this instance, but with the given option changed.
		/// It is unusual to call this method directly. Instead use <see cref="DataContextOptionsBuilder" />.
		/// </summary>
		/// <param name="write"> The option to change. </param>
		/// <returns> A new instance with the option changed. </returns>
		public virtual DataContextOptionsExtension WriteTraceWith(Action<string?, string?, TraceLevel>? write)
		{
			return SetValue(o => o._writeTrace = write);
		}

		/// <summary>
		/// The options set from the <see cref="DataContextOptionsBuilder.ReplaceService{TService,TImplementation}" /> method.
		/// </summary>

		public void ApplyServices()
		{
		}

		/// <summary>
		/// Gives the extension a chance to validate that all options in the extension are valid.
		/// If options are invalid, then an exception will be thrown.
		/// </summary>
		/// <param name="options"> The options being validated. </param>
		public virtual void Validate(IDataContextOptions options)
		{
		}

		sealed class ExtensionInfo : DataContextOptionsExtensionInfo
		{
			long?   _serviceProviderHash;
			string? _logFragment;

			public ExtensionInfo(DataContextOptionsExtension extension)
				: base(extension)
			{
			}

			new DataContextOptionsExtension Extension => (DataContextOptionsExtension)base.Extension;

			public override bool IsDatabaseProvider => false;

			public override string LogFragment
			{
				get
				{
					if (_logFragment == null)
					{
						var builder = new StringBuilder();

						if (Extension._commandTimeout != null)
						{
							builder.Append("CommandTimeout=").Append(Extension._commandTimeout).Append(' ');
						}

						_logFragment = builder.ToString();
					}

					return _logFragment;
				}
			}

			public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
			{
				if (debugInfo == null)
					throw new ArgumentNullException(nameof(debugInfo));
			}

			public override long GetServiceProviderHashCode()
			{
				if (_serviceProviderHash == null)
				{
					var hashCode = 0L;

					_serviceProviderHash = hashCode;
				}

				return _serviceProviderHash.Value;
			}
		}
	}
}
