using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;


namespace LinqToDB
{
	using Data;
	using Data.RetryPolicy;
	using Infrastructure;
	using Interceptors;

	/// <summary>
    ///     <para>
    ///         Provides a simple API surface for configuring <see cref="DataContextOptions" />. Databases (and other extensions)
    ///         typically define extension methods on this object that allow you to configure the database connection (and other
    ///         options) to be used for a context.
    ///     </para>
    /// </summary>
    public class DataContextOptionsBuilder : IDataContextOptionsBuilderInfrastructure
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
        ///         Replaces the internal linq2db implementation of a service contract with a different
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
        ///         Interceptors can be used to view, change, or suppress operations taken by linq2db.
        ///         See the specific implementations of <see cref="IInterceptor" /> for details. For example, 'ICommandInterceptor'.
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
        ///         Interceptors can be used to view, change, or suppress operations taken by linq2db.
        ///         See the specific implementations of <see cref="IInterceptor" /> for details. For example, 'ICommandInterceptor'.
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

        /// <summary>
        ///     <para>
        ///         Adds <see cref="IInterceptor" /> instance to those registered on the context.
        ///     </para>
        ///     <para>
        ///         Interceptors can be used to view, change, or suppress operations taken by linq2db.
        ///         See the specific implementations of <see cref="IInterceptor" /> for details. For example, 'ICommandInterceptor'.
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
        /// <param name="interceptor"> The interceptor to add. </param>
        /// <returns> The same builder instance so that multiple calls can be chained. </returns>
        public virtual DataContextOptionsBuilder WithInterceptor(IInterceptor interceptor)
	        => AddInterceptors(interceptor);

        public virtual DataContextOptionsBuilder WithOptions(Action<LinqOptionsBuilder> linqOptionsAction)
        {
	        if (linqOptionsAction == null)
	        {
		        throw new ArgumentNullException(nameof(linqOptionsAction));
	        }

	        linqOptionsAction.Invoke(new LinqOptionsBuilder(this));

	        return this;
        }

        public virtual DataContextOptionsBuilder UseRetryPolicy(Action<RetryPolicyOptionsBuilder> retryPolicyOptionsAction)
        {
	        if (retryPolicyOptionsAction == null)
	        {
		        throw new ArgumentNullException(nameof(retryPolicyOptionsAction));
	        }

	        retryPolicyOptionsAction.Invoke(new RetryPolicyOptionsBuilder(this));

	        return this;
        }

        public virtual DataContextOptionsBuilder UseRetryPolicy(IRetryPolicy retryPolicy)
        {
	        if (retryPolicy == null)
	        {
		        throw new ArgumentNullException(nameof(retryPolicy));
	        }

	        new RetryPolicyOptionsBuilder(this).WithRetryPolicy(retryPolicy);

	        return this;
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
        void IDataContextOptionsBuilderInfrastructure.AddOrUpdateExtension<TExtension>(TExtension extension)
        {
	        if (extension == null)
	        {
		        throw new ArgumentNullException(nameof(extension));
	        }

	        _options = _options.WithExtension(extension);
        }

        private DataContextOptionsBuilder WithOption(Func<CoreDataContextOptionsExtension, CoreDataContextOptionsExtension> withFunc)
        {
            ((IDataContextOptionsBuilderInfrastructure)this).AddOrUpdateExtension(
                withFunc(Options.FindExtension<CoreDataContextOptionsExtension>() ?? new CoreDataContextOptionsExtension()));

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
