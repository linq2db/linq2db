// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Data.Common;
using System.Linq;
using System.Text;
using LinqToDB.DataProvider;

namespace Microsoft.EntityFrameworkCore.Infrastructure
{
    /// <summary>
    ///     <para>
    ///         Represents options managed by the relational database providers.
    ///         These options are set using <see cref="DbContextOptionsBuilder" />.
    ///     </para>
    ///     <para>
    ///         Instances of this class are designed to be immutable. To change an option, call one of the 'With...'
    ///         methods to obtain a new instance with the option changed.
    ///     </para>
    /// </summary>
    public abstract class RelationalOptionsExtension : IDbContextOptionsExtension
    {
        // NB: When adding new options, make sure to update the copy constructor below.

        private string?        _connectionString;
        private DbConnection?  _connection;
        private string?        _providerName;
        private IDataProvider? _dataProvider;
        private int?           _commandTimeout;
        private bool           _useRelationalNulls;

        /// <summary>
        ///     Creates a new set of options with everything set to default values.
        /// </summary>
        protected RelationalOptionsExtension()
        {
        }

        /// <summary>
        ///     Called by a derived class constructor when implementing the <see cref="Clone" /> method.
        /// </summary>
        /// <param name="copyFrom"> The instance that is being cloned. </param>
        protected RelationalOptionsExtension(RelationalOptionsExtension copyFrom)
        {
	        if (copyFrom == null)
	        {
		        throw new ArgumentNullException(nameof(copyFrom));
	        }

            _connectionString = copyFrom._connectionString;
            _connection = copyFrom._connection;
            _commandTimeout = copyFrom._commandTimeout;
            _useRelationalNulls = copyFrom._useRelationalNulls;
        }

        /// <summary>
        ///     Information/metadata about the extension.
        /// </summary>
        public abstract DbContextOptionsExtensionInfo Info { get; }

        /// <summary>
        ///     Override this method in a derived class to ensure that any clone created is also of that class.
        /// </summary>
        /// <returns> A clone of this instance, which can be modified before being returned as immutable. </returns>
        protected abstract RelationalOptionsExtension Clone();

        /// <summary>
        ///     The connection string, or <c>null</c> if a <see cref="DbConnection" /> was used instead of
        ///     a connection string.
        /// </summary>
        public virtual string? ConnectionString => _connectionString;

        /// <summary>
        ///     Creates a new instance with all options the same as for this instance, but with the given option changed.
        ///     It is unusual to call this method directly. Instead use <see cref="DbContextOptionsBuilder" />.
        /// </summary>
        /// <param name="connectionString"> The option to change. </param>
        /// <returns> A new instance with the option changed. </returns>
        public virtual RelationalOptionsExtension WithConnectionString(string connectionString)
        {
	        if (string.IsNullOrEmpty(connectionString))
	        {
		        throw new ArgumentException("Value cannot be null or empty.", nameof(connectionString));
	        }

            var clone = Clone();

            clone._connectionString = connectionString;

            return clone;
        }

        /// <summary>
        ///     The <see cref="DbConnection" />, or <c>null</c> if a connection string was used instead of
        ///     the full connection object.
        /// </summary>
        public virtual DbConnection? Connection => _connection;

        /// <summary>
        ///     Creates a new instance with all options the same as for this instance, but with the given option changed.
        ///     It is unusual to call this method directly. Instead use <see cref="DbContextOptionsBuilder" />.
        /// </summary>
        /// <param name="connection"> The option to change. </param>
        /// <returns> A new instance with the option changed. </returns>
        public virtual RelationalOptionsExtension WithConnection(DbConnection connection)
        {
	        if (connection == null)
	        {
		        throw new ArgumentNullException(nameof(connection));
	        }

            var clone = Clone();

            clone._connection = connection;

            return clone;
        }

        /// <summary>
        ///     Creates a new instance with all options the same as for this instance, but with the given option changed.
        ///     It is unusual to call this method directly. Instead use <see cref="DbContextOptionsBuilder" />.
        /// </summary>
        /// <param name="providerName"> The option to change. </param>
        /// <returns> A new instance with the option changed. </returns>
        public virtual RelationalOptionsExtension WithProvider(string providerName)
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
        ///     It is unusual to call this method directly. Instead use <see cref="DbContextOptionsBuilder" />.
        /// </summary>
        /// <param name="dataProvider"> The option to change. </param>
        /// <returns> A new instance with the option changed. </returns>
        public virtual RelationalOptionsExtension WithProvider(IDataProvider dataProvider)
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
        ///     The command timeout, or <c>null</c> if none has been set.
        /// </summary>
        public virtual int? CommandTimeout => _commandTimeout;

        /// <summary>
        ///     Creates a new instance with all options the same as for this instance, but with the given option changed.
        ///     It is unusual to call this method directly. Instead use <see cref="DbContextOptionsBuilder" />.
        /// </summary>
        /// <param name="commandTimeout"> The option to change. </param>
        /// <returns> A new instance with the option changed. </returns>
        public virtual RelationalOptionsExtension WithCommandTimeout(int? commandTimeout)
        {
            if (commandTimeout.HasValue
                && commandTimeout <= 0)
            {
                throw new InvalidOperationException("Invalid CommandTimeout");
            }

            var clone = Clone();

            clone._commandTimeout = commandTimeout;

            return clone;
        }

        /// <summary>
        ///     Indicates whether or not to use relational database semantics when comparing null values. By default,
        ///     Entity Framework will use C# semantics for null values, and generate SQL to compensate for differences
        ///     in how the database handles nulls.
        /// </summary>
        public virtual bool UseRelationalNulls => _useRelationalNulls;

        /// <summary>
        ///     Creates a new instance with all options the same as for this instance, but with the given option changed.
        ///     It is unusual to call this method directly. Instead use <see cref="DbContextOptionsBuilder" />.
        /// </summary>
        /// <param name="useRelationalNulls"> The option to change. </param>
        /// <returns> A new instance with the option changed. </returns>
        public virtual RelationalOptionsExtension WithUseRelationalNulls(bool useRelationalNulls)
        {
            var clone = Clone();

            clone._useRelationalNulls = useRelationalNulls;

            return clone;
        }

        /// <summary>
        ///     Finds an existing <see cref="RelationalOptionsExtension" /> registered on the given options
        ///     or throws if none has been registered. This is typically used to find some relational
        ///     configuration when it is known that a relational provider is being used.
        /// </summary>
        /// <param name="options"> The context options to look in. </param>
        /// <returns> The extension. </returns>
        public static RelationalOptionsExtension Extract(IDbContextOptions options)
        {
	        if (options == null)
	        {
		        throw new ArgumentNullException(nameof(options));
	        }

            var relationalOptionsExtensions
                = options.Extensions
                    .OfType<RelationalOptionsExtension>()
                    .ToList();

            if (relationalOptionsExtensions.Count == 0)
            {
                throw new InvalidOperationException("No provider configured.");
            }

            if (relationalOptionsExtensions.Count > 1)
            {
                throw new InvalidOperationException("Multiple providers configured.");
            }

            return relationalOptionsExtensions[0];
        }

        /// <summary>
        ///     Adds the services required to make the selected options work. This is used when there
        ///     is no external <see cref="IServiceProvider" /> and EF is maintaining its own service
        ///     provider internally. This allows database providers (and other extensions) to register their
        ///     required services when EF is creating an service provider.
        /// </summary>
        public abstract void ApplyServices();

        /// <summary>
        ///     Gives the extension a chance to validate that all options in the extension are valid.
        ///     Most extensions do not have invalid combinations and so this will be a no-op.
        ///     If options are invalid, then an exception should be thrown.
        /// </summary>
        /// <param name="options"> The options being validated. </param>
        public virtual void Validate(IDbContextOptions options)
        {
        }

        public virtual string?        ProviderName => _providerName;
        public virtual IDataProvider? DataProvider => _dataProvider;


        /// <summary>
        ///     Information/metadata for a <see cref="RelationalOptionsExtension" />.
        /// </summary>
        protected abstract class RelationalExtensionInfo : DbContextOptionsExtensionInfo
        {
            private string? _logFragment;

            /// <summary>
            ///     Creates a new <see cref="RelationalExtensionInfo" /> instance containing
            ///     info/metadata for the given extension.
            /// </summary>
            /// <param name="extension"> The extension. </param>
            protected RelationalExtensionInfo(IDbContextOptionsExtension extension)
                : base(extension)
            {
            }

            /// <summary>
            ///     The extension for which this instance contains metadata.
            /// </summary>
            public new virtual RelationalOptionsExtension Extension
                => (RelationalOptionsExtension)base.Extension;

            /// <summary>
            ///     True, since this is a database provider base class.
            /// </summary>
            public override bool IsDatabaseProvider => true;

            /// <summary>
            ///     Returns a hash code created from any options that would cause a new <see cref="IServiceProvider" />
            ///     to be needed. Most extensions do not have any such options and should return zero.
            /// </summary>
            /// <returns> A hash over options that require a new service provider when changed. </returns>
            public override long GetServiceProviderHashCode() => 0;

            /// <summary>
            ///     A message fragment for logging typically containing information about
            ///     any useful non-default options that have been configured.
            /// </summary>
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
        }
    }
}
