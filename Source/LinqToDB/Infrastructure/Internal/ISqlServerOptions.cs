using System;

#pragma warning disable CS1574, CS1584, CS1581, CS1580

namespace LinqToDB.Infrastructure.Internal
{
    /// <summary>
    ///     <para>
    ///         Options set at the <see cref="IServiceProvider" /> singleton level to control
    ///         SQL Server specific options.
    ///     </para>
    ///     <para>
    ///         The service lifetime is <see cref="ServiceLifetime.Singleton" /> and multiple registrations
    ///         are allowed. This means a single instance of each service is used by many <see cref="IDataContext" />
    ///         instances. The implementation must be thread-safe.
    ///         This service cannot depend on services registered as <see cref="ServiceLifetime.Scoped" />.
    ///     </para>
    /// </summary>
    public interface ISqlServerOptions : ISingletonOptions
    {
        /// <summary>
        ///     Reflects the option set by <see cref="SqlServerDataContextOptionsBuilder.UseRowNumberForPaging" />.
        /// </summary>
        bool RowNumberPagingEnabled { get; }
    }
}
