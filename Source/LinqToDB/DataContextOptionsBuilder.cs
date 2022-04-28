using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;


namespace LinqToDB
{
	using Data;
	using DataProvider;
	using Infrastructure;
	using Interceptors;
	using Mapping;

    /// <summary>
    ///     <para>
    ///         Provides a simple API surface for configuring <see cref="DataContextOptions" />. Databases (and other extensions)
    ///         typically define extension methods on this object that allow you to configure the database connection (and other
    ///         options) to be used for a context.
    ///     </para>
    /// </summary>
    public class DataContextOptionsBuilder : IDbContextOptionsBuilderInfrastructure
    {
        private DataContextOptions _options;

        /// <summary>
        ///     Initializes a new instance of the <see cref="DataContextOptionsBuilder" /> class with no options set.
        /// </summary>
        public DataContextOptionsBuilder()
            : this(new DataContextOptions<DataConnection>())
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="DataContextOptionsBuilder" /> class to further configure
        ///     a given <see cref="DataContextOptions" />.
        /// </summary>
        /// <param name="options"> The options to be configured. </param>
        public DataContextOptionsBuilder(DataContextOptions options)
        {
	        if (options == null)
	        {
		        throw new ArgumentNullException(nameof(options));
	        }

            _options = options;
        }

        /// <summary>
        ///     Gets the options being configured.
        /// </summary>
        public virtual DataContextOptions Options => _options;

        /// <summary>
        ///     <para>
        ///         Gets a value indicating whether any options have been configured.
        ///     </para>
        /// </summary>
        public virtual bool IsConfigured => _options.Extensions.Any(e => e.Info.IsDatabaseProvider);

        /// <summary>
        ///     <para>
        ///         Replaces the internal Entity Framework implementation of a service contract with a different
        ///         implementation.
        ///     </para>
        ///     <para>
        ///         The replacement service gets the same scope as the EF service that it is replacing.
        ///     </para>
        /// </summary>
        /// <typeparam name="TService"> The type (usually an interface) that defines the contract of the service to replace. </typeparam>
        /// <typeparam name="TImplementation"> The new implementation type for the service. </typeparam>
        /// <returns> The same builder instance so that multiple calls can be chained. </returns>
        public virtual DataContextOptionsBuilder ReplaceService<TService, TImplementation>()
            where TImplementation : TService
            => WithOption(e => e.WithReplacedService(typeof(TService), typeof(TImplementation)));

        /// <summary>
        ///     <para>
        ///         Adds <see cref="IInterceptor" /> instances to those registered on the context.
        ///     </para>
        ///     <para>
        ///         Interceptors can be used to view, change, or suppress operations taken by Entity Framework.
        ///         See the specific implementations of <see cref="IInterceptor" /> for details. For example, 'IDbCommandInterceptor'.
        ///     </para>
        ///     <para>
        ///         A single interceptor instance can implement multiple different interceptor interfaces. I will be registered as
        ///         an interceptor for all interfaces that it implements.
        ///     </para>
        ///     <para>
        ///         Extensions can also register multiple <see cref="IInterceptor" />s in the internal service provider.
        ///         If both injected and application interceptors are found, then the injected interceptors are run in the
        ///         order that they are resolved from the service provider, and then the application interceptors are run
        ///         in the order that they were added to the context.
        ///     </para>
        ///     <para>
        ///         Calling this method multiple times will result in all interceptors in every call being added to the context.
        ///         Interceptors added in a previous call are not overridden by interceptors added in a later call.
        ///     </para>
        /// </summary>
        /// <param name="interceptors"> The interceptors to add. </param>
        /// <returns> The same builder instance so that multiple calls can be chained. </returns>
        public virtual DataContextOptionsBuilder AddInterceptors(IEnumerable<IInterceptor> interceptors)
            => WithOption(e => e.WithInterceptors(interceptors));

        /// <summary>
        ///     <para>
        ///         Adds <see cref="IInterceptor" /> instances to those registered on the context.
        ///     </para>
        ///     <para>
        ///         Interceptors can be used to view, change, or suppress operations taken by Entity Framework.
        ///         See the specific implementations of <see cref="IInterceptor" /> for details. For example, 'IDbCommandInterceptor'.
        ///     </para>
        ///     <para>
        ///         Extensions can also register multiple <see cref="IInterceptor" />s in the internal service provider.
        ///         If both injected and application interceptors are found, then the injected interceptors are run in the
        ///         order that they are resolved from the service provider, and then the application interceptors are run
        ///         in the order that they were added to the context.
        ///     </para>
        ///     <para>
        ///         Calling this method multiple times will result in all interceptors in every call being added to the context.
        ///         Interceptors added in a previous call are not overridden by interceptors added in a later call.
        ///     </para>
        /// </summary>
        /// <param name="interceptors"> The interceptors to add. </param>
        /// <returns> The same builder instance so that multiple calls can be chained. </returns>
        public virtual DataContextOptionsBuilder AddInterceptors(params IInterceptor[] interceptors)
            => AddInterceptors((IEnumerable<IInterceptor>)interceptors);


        public virtual DataContextOptionsBuilder UseConnectionString(string providerName, string connectionString)
        {
	        return WithOption(e => e.WithProviderName(providerName).WithConnectionString(connectionString));
        }

        public virtual DataContextOptionsBuilder UseConnectionString(IDataProvider dataProvider, string connectionString)
        {
	        return WithOption(e => e.WithDataProvider(dataProvider).WithConnectionString(connectionString));
        }

        public virtual DataContextOptionsBuilder UseConnectionString(string connectionString)
        {
	        return WithOption(e => e.WithConnectionString(connectionString));
        }

        public virtual DataContextOptionsBuilder UseConfigurationString(string? configurationString)
        {
	        return WithOption(e => e.WithConfigurationString(configurationString));
        }

        public virtual DataContextOptionsBuilder UseConfigurationString(string? configurationString, MappingSchema mappingSchema)
        {
	        return WithOption(e => e.WithConfigurationString(configurationString).WithMappingSchema(mappingSchema));
        }

        public virtual DataContextOptionsBuilder UseConnection(DbConnection connection)
        {
	        return WithOption(e => e.WithConnection(connection));
        }

        public virtual DataContextOptionsBuilder UseConnection(IDataProvider dataProvider, DbConnection connection)
        {
	        return WithOption(e => e.WithDataProvider(dataProvider).WithConnection(connection));
        }

        public virtual DataContextOptionsBuilder UseConnection(IDataProvider dataProvider, DbConnection connection, bool disposeConnection)
        {
	        return WithOption(e => e.WithDataProvider(dataProvider).WithConnection(connection).WithDisposeConnection(disposeConnection));
        }

        public virtual DataContextOptionsBuilder UseProvider(string providerName)
        {
	        return WithOption(e => e.WithProviderName(providerName));
        }

        public virtual DataContextOptionsBuilder UseDataProvider(IDataProvider dataProvider)
        {
	        return WithOption(e => e.WithDataProvider(dataProvider));
        }

        public virtual DataContextOptionsBuilder UseMappingSchema(MappingSchema mappingSchema)
        {
	        return WithOption(e => e.WithMappingSchema(mappingSchema));
        }

        public virtual DataContextOptionsBuilder UseConnectionFactory(Func<DbConnection> connectionFactory)
        {
	        return WithOption(e => e.WithConnectionFactory(connectionFactory));
        }

        public virtual DataContextOptionsBuilder UseConnectionFactory(IDataProvider dataProvider, Func<DbConnection> connectionFactory)
        {
	        return WithOption(e => e.WithDataProvider(dataProvider).WithConnectionFactory(connectionFactory));
        }

        public virtual DataContextOptionsBuilder UseTransaction(IDataProvider dataProvider, DbTransaction transaction)
        {
	        return WithOption(e => e.WithDataProvider(dataProvider).WithTransaction(transaction));
        }

        /// <summary>
        /// Configure the database to use specified trace level.
        /// </summary>
        /// <returns>The builder instance so calls can be chained.</returns>
        public virtual DataContextOptionsBuilder WithTraceLevel(TraceLevel traceLevel)
        {
	        return WithOption(e => e.WithTraceLevel(traceLevel));
        }

        /// <summary>
        /// Configure the database to use the specified callback for logging or tracing.
        /// </summary>
        /// <param name="onTrace">Callback, may not be called depending on the trace level.</param>
        /// <returns>The builder instance so calls can be chained.</returns>
        public virtual DataContextOptionsBuilder WithTracing(Action<TraceInfo> onTrace)
        {
	        return WithOption(e => e.WithTracing(onTrace));
        }

        /// <summary>
        /// Configure the database to use the specified trace level and callback for logging or tracing.
        /// </summary>
        /// <param name="traceLevel">Trace level to use.</param>
        /// <param name="onTrace">Callback, may not be called depending on the trace level.</param>
        /// <returns>The builder instance so calls can be chained.</returns>
        public virtual DataContextOptionsBuilder WithTracing(TraceLevel traceLevel, Action<TraceInfo> onTrace)
        {
	        return WithOption(e => e.WithTracing(onTrace).WithTraceLevel(traceLevel));
        }

        /// <summary>
        /// Configure the database to use the specified a string trace callback.
        /// </summary>
        /// <param name="write">Callback, may not be called depending on the trace level.</param>
        /// <returns>The builder instance so calls can be chained.</returns>
        public DataContextOptionsBuilder WriteTraceWith(Action<string?, string?, TraceLevel> write)
        {
	        return WithOption(e => e.WriteTraceWith(write));
        }

        /// <summary>
        ///     <para>
        ///         Adds the given extension to the options. If an existing extension of the same type already exists, it will be replaced.
        ///     </para>
        ///     <para>
        ///         This method is intended for use by extension methods to configure the context. It is not intended to be used in
        ///         application code.
        ///     </para>
        /// </summary>
        /// <typeparam name="TExtension"> The type of extension to be added. </typeparam>
        /// <param name="extension"> The extension to be added. </param>
        void IDbContextOptionsBuilderInfrastructure.AddOrUpdateExtension<TExtension>(TExtension extension)
        {
	        if (extension == null)
	        {
		        throw new ArgumentNullException(nameof(extension));
	        }

	        _options = _options.WithExtension(extension);
        }

        private DataContextOptionsBuilder WithOption(Func<CoreOptionsExtension, CoreOptionsExtension> withFunc)
        {
            ((IDbContextOptionsBuilderInfrastructure)this).AddOrUpdateExtension(
                withFunc(Options.FindExtension<CoreOptionsExtension>() ?? new CoreOptionsExtension()));

            return this;
        }

        #region Hidden System.Object members

        /// <summary>
        ///     Returns a string that represents the current object.
        /// </summary>
        /// <returns> A string that represents the current object. </returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override string? ToString() => base.ToString();

        /// <summary>
        ///     Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj"> The object to compare with the current object. </param>
        /// <returns> true if the specified object is equal to the current object; otherwise, false. </returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object? obj) => base.Equals(obj);

        /// <summary>
        ///     Serves as the default hash function.
        /// </summary>
        /// <returns> A hash code for the current object. </returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode() => base.GetHashCode();

        #endregion
    }
}
