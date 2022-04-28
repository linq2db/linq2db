using System;

#pragma warning disable CS1574, CS1584, CS1581, CS1580

namespace LinqToDB.Infrastructure
{
    /// <summary>
    ///     <para>
    ///         Implemented by any class that represents options that can only be set at the
    ///         <see cref="IServiceProvider" /> singleton level.
    ///     </para>
    ///     <para>
    ///         The service lifetime is <see cref="ServiceLifetime.Singleton" /> and multiple registrations
    ///         are allowed. This means a single instance of each service is used by many <see cref="DbContext" />
    ///         instances. The implementation must be thread-safe.
    ///         This service cannot depend on services registered as <see cref="ServiceLifetime.Scoped" />.
    ///     </para>
    /// </summary>
    public interface ISingletonOptions
    {
        /// <summary>
        ///     Initializes the singleton options from the given <see cref="IDataContextOptions" />.
        /// </summary>
        void Initialize(IDataContextOptions options);

        /// <summary>
        ///     Validates that the options in given <see cref="IDataContextOptions" /> have not
        ///     changed when compared to the options already set here, and throws if they have.
        /// </summary>
        void Validate(IDataContextOptions options);
    }
}
