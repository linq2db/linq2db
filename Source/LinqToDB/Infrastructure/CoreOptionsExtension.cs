using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

namespace LinqToDB.Infrastructure
{
	using LinqToDB.Common.Internal;
	using Data;
	using DataProvider;
	using Interceptors;
	using Mapping;

    /// <summary>
    ///     <para>
    ///         Represents options managed by the core of Entity Framework, as opposed to those managed
    ///         by database providers or extensions. These options are set using <see cref="DataContextOptionsBuilder" />.
    ///     </para>
    ///     <para>
    ///         Instances of this class are designed to be immutable. To change an option, call one of the 'With...'
    ///         methods to obtain a new instance with the option changed.
    ///     </para>
    /// </summary>
    public class CoreOptionsExtension : IDbContextOptionsExtension
    {
        private IDictionary<Type, Type>?       _replacedServices;
        private DbContextOptionsExtensionInfo? _info;
        private IEnumerable<IInterceptor>?     _interceptors;

        private string?        _connectionString;
        private string?        _configurationString;
        private DbConnection?  _dbConnection;
        private string?        _providerName;
        private IDataProvider? _dataProvider;
        private int?           _commandTimeout;
        private bool           _disposeConnection;

        private          bool                _useRelationalNulls;

        private MappingSchema?                        _mappingSchema;
        private Func<DbConnection>?                   _connectionFactory;
        private DbTransaction?                        _transaction;
        private TraceLevel?                           _traceLevel;
        private Action<TraceInfo>?                    _onTrace;
        private Action<string?, string?, TraceLevel>? _writeTrace;


        /// <summary>
        ///     Creates a new set of options with everything set to default values.
        /// </summary>
        public CoreOptionsExtension()
        {
        }

        /// <summary>
        ///     Called by a derived class constructor when implementing the <see cref="Clone" /> method.
        /// </summary>
        /// <param name="copyFrom"> The instance that is being cloned. </param>
        protected CoreOptionsExtension(CoreOptionsExtension copyFrom)
        {
	        _interceptors        = copyFrom.Interceptors?.ToList();
            _connectionString    = copyFrom._connectionString;
            _configurationString = copyFrom._configurationString;
            _dbConnection          = copyFrom._dbConnection;
            _providerName        = copyFrom._providerName;
            _dataProvider        = copyFrom._dataProvider;
            _commandTimeout      = copyFrom._commandTimeout;
            _disposeConnection   = copyFrom._disposeConnection;
            _useRelationalNulls  = copyFrom._useRelationalNulls;
            _mappingSchema       = copyFrom._mappingSchema;
            _connectionFactory   = copyFrom._connectionFactory;
            _transaction         = copyFrom._transaction;
            _traceLevel          = copyFrom._traceLevel;
            _onTrace             = copyFrom._onTrace;
            _writeTrace          = copyFrom._writeTrace;

            if (copyFrom._replacedServices != null)
            {
                _replacedServices = new Dictionary<Type, Type>(copyFrom._replacedServices);
            }

            if (copyFrom.Interceptors != null)
            {
				//TODO:
	            _interceptors = copyFrom.Interceptors.ToList();
            }
        }

        /// <summary>
        ///     Information/metadata about the extension.
        /// </summary>
        public virtual DbContextOptionsExtensionInfo Info
            => _info ??= new ExtensionInfo(this);

        /// <summary>
        ///     Override this method in a derived class to ensure that any clone created is also of that class.
        /// </summary>
        /// <returns> A clone of this instance, which can be modified before being returned as immutable. </returns>
        protected virtual CoreOptionsExtension Clone() => new CoreOptionsExtension(this);

        /// <summary>
        ///     Creates a new instance with all options the same as for this instance, but with the given option changed.
        ///     It is unusual to call this method directly. Instead use <see cref="DataContextOptionsBuilder" />.
        /// </summary>
        /// <param name="serviceType"> The service contract. </param>
        /// <param name="implementationType"> The implementation type to use for the service. </param>
        /// <returns> A new instance with the option changed. </returns>
        public virtual CoreOptionsExtension WithReplacedService(Type serviceType, Type implementationType)
        {
            var clone = Clone();

            if (clone._replacedServices == null)
            {
                clone._replacedServices = new Dictionary<Type, Type>();
            }

            clone._replacedServices[serviceType] = implementationType;

            return clone;
        }

        /// <summary>
        ///     Creates a new instance with all options the same as for this instance, but with the given option changed.
        ///     It is unusual to call this method directly. Instead use <see cref="DataContextOptionsBuilder" />.
        /// </summary>
        /// <param name="interceptors"> The option to change. </param>
        /// <returns> A new instance with the option changed. </returns>
        public virtual CoreOptionsExtension WithInterceptors(IEnumerable<IInterceptor> interceptors)
        {
	        if (interceptors == null)
	        {
		        throw new ArgumentNullException(nameof(interceptors));
	        }

	        var clone = Clone();

            clone._interceptors = _interceptors == null
                ? interceptors
                : _interceptors.Concat(interceptors);

            return clone;
        }

        /// <summary>
        ///     Creates a new instance with all options the same as for this instance, but with the given option changed.
        ///     It is unusual to call this method directly. Instead use <see cref="DataContextOptionsBuilder" />.
        /// </summary>
        /// <param name="interceptor"> The option to change. </param>
        /// <returns> A new instance with the option changed. </returns>
        public virtual CoreOptionsExtension WithInterceptor(IInterceptor interceptor)
        {
	        if (interceptor == null)
	        {
		        throw new ArgumentNullException(nameof(interceptor));
	        }

	        var clone = Clone();

	        clone._interceptors = _interceptors == null
		        ? new[] { interceptor }
		        : _interceptors.Concat(new[] { interceptor });

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
        public virtual string? ConfigurationString => _connectionString;

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
        public virtual Action<string?, string?, TraceLevel>? WriteTrace => _writeTrace;

        /// <summary>
        /// Gets connection factory to use with <see cref="DataConnection"/> instance.
        /// </summary>
        public Func<DbConnection>? ConnectionFactory => _connectionFactory;

        public DbTransaction? DbTransaction => _transaction;

        /// <summary>
        ///     Indicates whether or not to use relational database semantics when comparing null values. By default,
        ///     Entity Framework will use C# semantics for null values, and generate SQL to compensate for differences
        ///     in how the database handles nulls.
        /// </summary>
        public virtual bool UseRelationalNulls => _useRelationalNulls;

        /// <summary>
        ///     Creates a new instance with all options the same as for this instance, but with the given option changed.
        ///     It is unusual to call this method directly. Instead use <see cref="DataContextOptionsBuilder" />.
        /// </summary>
        /// <param name="connectionString"> The option to change. </param>
        /// <returns> A new instance with the option changed. </returns>
        public virtual CoreOptionsExtension WithConnectionString(string connectionString)
        {
	        if (string.IsNullOrEmpty(connectionString))
	        {
		        throw new ArgumentException("Value cannot be null or empty.", nameof(connectionString));
	        }

	        var clone = Clone();

	        clone._connectionString = connectionString;

	        return clone;
        }

        public virtual CoreOptionsExtension WithConfigurationString(string? configurationString)
        {
	        var clone = Clone();

	        clone._configurationString = configurationString;

	        return clone;
        }

        /// <summary>
        ///     Creates a new instance with all options the same as for this instance, but with the given option changed.
        ///     It is unusual to call this method directly. Instead use <see cref="DataContextOptionsBuilder" />.
        /// </summary>
        /// <param name="connection"> The option to change. </param>
        /// <returns> A new instance with the option changed. </returns>
        public virtual CoreOptionsExtension WithConnection(DbConnection connection)
        {
	        if (connection == null)
	        {
		        throw new ArgumentNullException(nameof(connection));
	        }

            var clone = Clone();

            clone._dbConnection = connection;

            return clone;
        }

        /// <summary>
        ///     Creates a new instance with all options the same as for this instance, but with the given option changed.
        ///     It is unusual to call this method directly. Instead use <see cref="DataContextOptionsBuilder" />.
        /// </summary>
        /// <param name="disposeConnection"> The option to change. </param>
        /// <returns> A new instance with the option changed. </returns>
        public virtual CoreOptionsExtension WithDisposeConnection(bool disposeConnection)
        {
	        var clone = Clone();

	        clone._disposeConnection = disposeConnection;

	        return clone;
        }


        /// <summary>
        ///     Creates a new instance with all options the same as for this instance, but with the given option changed.
        ///     It is unusual to call this method directly. Instead use <see cref="DataContextOptionsBuilder" />.
        /// </summary>
        /// <param name="providerName"> The option to change. </param>
        /// <returns> A new instance with the option changed. </returns>
        public virtual CoreOptionsExtension WithProviderName(string providerName)
        {
	        if (providerName == null)
	        {
		        throw new ArgumentNullException(nameof(providerName));
	        }

	        var clone = Clone();

	        clone._providerName = providerName;

	        return clone;
        }

        /// <summary>
        ///     Creates a new instance with all options the same as for this instance, but with the given option changed.
        ///     It is unusual to call this method directly. Instead use <see cref="DataContextOptionsBuilder" />.
        /// </summary>
        /// <param name="dataProvider"> The option to change. </param>
        /// <returns> A new instance with the option changed. </returns>
        public virtual CoreOptionsExtension WithDataProvider(IDataProvider dataProvider)
        {
	        if (dataProvider == null)
	        {
		        throw new ArgumentNullException(nameof(dataProvider));
	        }

	        var clone = Clone();

	        clone._dataProvider = dataProvider;

	        return clone;
        }

        /// <summary>
        ///     Creates a new instance with all options the same as for this instance, but with the given option changed.
        ///     It is unusual to call this method directly. Instead use <see cref="DataContextOptionsBuilder" />.
        /// </summary>
        /// <param name="mappingSchema"> The option to change. </param>
        /// <returns> A new instance with the option changed. </returns>
        public virtual CoreOptionsExtension WithMappingSchema(MappingSchema mappingSchema)
        {
	        if (mappingSchema == null)
	        {
		        throw new ArgumentNullException(nameof(mappingSchema));
	        }

	        var clone = Clone();

	        clone._mappingSchema = mappingSchema;

	        return clone;
        }

        /// <summary>
        ///     Creates a new instance with all options the same as for this instance, but with the given option changed.
        ///     It is unusual to call this method directly. Instead use <see cref="DataContextOptionsBuilder" />.
        /// </summary>
        /// <param name="connectionFactory"> The option to change. </param>
        /// <returns> A new instance with the option changed. </returns>
        public virtual CoreOptionsExtension WithConnectionFactory(Func<DbConnection> connectionFactory)
        {
	        if (connectionFactory == null)
	        {
		        throw new ArgumentNullException(nameof(connectionFactory));
	        }

	        var clone = Clone();

	        clone._connectionFactory = connectionFactory;

	        return clone;
        }

        /// <summary>
        ///     Creates a new instance with all options the same as for this instance, but with the given option changed.
        ///     It is unusual to call this method directly. Instead use <see cref="DataContextOptionsBuilder" />.
        /// </summary>
        /// <param name="transaction"> The option to change. </param>
        /// <returns> A new instance with the option changed. </returns>
        public virtual CoreOptionsExtension WithTransaction(DbTransaction transaction)
        {
	        if (transaction == null)
	        {
		        throw new ArgumentNullException(nameof(transaction));
	        }

	        var clone = Clone();

	        clone._transaction = transaction;

	        return clone;
        }

        /// <summary>
        ///     Creates a new instance with all options the same as for this instance, but with the given option changed.
        ///     It is unusual to call this method directly. Instead use <see cref="DataContextOptionsBuilder" />.
        /// </summary>
        /// <param name="traceLevel"> The option to change. </param>
        /// <returns> A new instance with the option changed. </returns>
        public CoreOptionsExtension WithTraceLevel(TraceLevel traceLevel)
        {
	        var clone = Clone();

	        clone._traceLevel = traceLevel;

	        return clone;
        }

        /// <summary>
        ///     Creates a new instance with all options the same as for this instance, but with the given option changed.
        ///     It is unusual to call this method directly. Instead use <see cref="DataContextOptionsBuilder" />.
        /// </summary>
        /// <param name="onTrace"> The option to change. </param>
        /// <returns> A new instance with the option changed. </returns>
        public virtual CoreOptionsExtension WithTracing(Action<TraceInfo> onTrace)
        {
	        if (onTrace == null)
	        {
		        throw new ArgumentNullException(nameof(onTrace));
	        }

	        var clone = Clone();

	        clone._onTrace = onTrace;

	        return clone;
        }

        public virtual CoreOptionsExtension WriteTraceWith(Action<string?, string?, TraceLevel> write)
        {
	        if (write == null)
	        {
		        throw new ArgumentNullException(nameof(write));
	        }

	        var clone = Clone();

	        clone._writeTrace = write;

	        return clone;
        }

        /// <summary>
        ///     Creates a new instance with all options the same as for this instance, but with the given option changed.
        ///     It is unusual to call this method directly. Instead use <see cref="DataContextOptionsBuilder" />.
        /// </summary>
        /// <param name="useRelationalNulls"> The option to change. </param>
        /// <returns> A new instance with the option changed. </returns>
        public virtual CoreOptionsExtension WithUseRelationalNulls(bool useRelationalNulls)
        {
	        var clone = Clone();

	        clone._useRelationalNulls = useRelationalNulls;

	        return clone;
        }


        /// <summary>
        ///     The options set from the <see cref="DataContextOptionsBuilder.ReplaceService{TService,TImplementation}" /> method.
        /// </summary>
        public virtual IReadOnlyDictionary<Type, Type>? ReplacedServices => (IReadOnlyDictionary<Type, Type>?)_replacedServices;

        public virtual IEnumerable<IInterceptor>? Interceptors => _interceptors;

        public void ApplyServices()
        {
        }

        /// <summary>
        ///     Gives the extension a chance to validate that all options in the extension are valid.
        ///     If options are invalid, then an exception will be thrown.
        /// </summary>
        /// <param name="options"> The options being validated. </param>
        public virtual void Validate(IDataContextOptions options)
        {
        }

        private sealed class ExtensionInfo : DbContextOptionsExtensionInfo
        {
            private long?   _serviceProviderHash;
            private string? _logFragment;

            public ExtensionInfo(CoreOptionsExtension extension)
                : base(extension)
            {
            }

            private new CoreOptionsExtension Extension
                => (CoreOptionsExtension)base.Extension;

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

		                if (Extension._useRelationalNulls)
		                {
			                builder.Append("UseRelationalNulls ");
		                }

		                _logFragment = builder.ToString();
	                }

	                return _logFragment;
                }
            }

            public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
            {
	            if (debugInfo == null)
	            {
		            throw new ArgumentNullException(nameof(debugInfo));
	            }

                if (Extension._replacedServices != null)
                {
                    foreach (var replacedService in Extension._replacedServices)
                    {
                        debugInfo["Core:" + nameof(DataContextOptionsBuilder.ReplaceService) + ":" + replacedService.Key.DisplayName()]
                            = replacedService.Value.GetHashCode().ToString(CultureInfo.InvariantCulture);
                    }
                }
            }

            public override long GetServiceProviderHashCode()
            {
                if (_serviceProviderHash == null)
                {
                    var hashCode = 0L;

                    if (Extension._replacedServices != null)
                    {
                        hashCode = Extension._replacedServices.Aggregate(hashCode, (t, e) => (t * 397) ^ e.Value.GetHashCode());
                    }

                    _serviceProviderHash = hashCode;
                }

                return _serviceProviderHash.Value;
            }
        }

        
    }
}
