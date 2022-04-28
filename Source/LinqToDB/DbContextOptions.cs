using System;
using System.Collections.Generic;
using LinqToDB;
using LinqToDB.Common.Internal;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Microsoft.EntityFrameworkCore
{
    /// <summary>
    ///     The options to be used by a <see cref="IDataContext" />.
    /// </summary>
    public abstract class DbContextOptions : IDbContextOptions
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="DbContextOptions" /> class. You normally use a <see cref="DbContextOptionsBuilder" />
        ///     to create instances of this class and it is not designed to be directly constructed in your application code.
        /// </summary>
        /// <param name="extensions"> The extensions that store the configured options. </param>
        protected DbContextOptions(
            IReadOnlyDictionary<Type, IDbContextOptionsExtension> extensions)
        {
            _extensions = extensions ?? throw new ArgumentNullException(nameof(extensions));
        }

        /// <summary>
        ///     Gets the extensions that store the configured options.
        /// </summary>
        public virtual IEnumerable<IDbContextOptionsExtension> Extensions => _extensions.Values;

        /// <summary>
        ///     Gets the extension of the specified type. Returns null if no extension of the specified type is configured.
        /// </summary>
        /// <typeparam name="TExtension"> The type of the extension to get. </typeparam>
        /// <returns> The extension, or null if none was found. </returns>
        public virtual TExtension? FindExtension<TExtension>()
            where TExtension : class, IDbContextOptionsExtension
        {
	        return _extensions.TryGetValue(typeof(TExtension), out var extension) ? (TExtension)extension : null;
        }

        /// <summary>
        ///     Gets the extension of the specified type. Throws if no extension of the specified type is configured.
        /// </summary>
        /// <typeparam name="TExtension"> The type of the extension to get. </typeparam>
        /// <returns> The extension. </returns>
        public virtual TExtension GetExtension<TExtension>()
            where TExtension : class, IDbContextOptionsExtension
        {
            var extension = FindExtension<TExtension>();
            if (extension == null)
            {
                throw new InvalidOperationException($"Options extension of type '{typeof(TExtension).ShortDisplayName()}' not found.");
            }

            return extension;
        }

        /// <summary>
        ///     Adds the given extension to the underlying options and creates a new
        ///     <see cref="DbContextOptions" /> with the extension added.
        /// </summary>
        /// <typeparam name="TExtension"> The type of extension to be added. </typeparam>
        /// <param name="extension"> The extension to be added. </param>
        /// <returns> The new options instance with the given extension added. </returns>
        public abstract DbContextOptions WithExtension<TExtension>(TExtension extension)
            where TExtension : class, IDbContextOptionsExtension;

        private readonly IReadOnlyDictionary<Type, IDbContextOptionsExtension> _extensions;

        /// <summary>
        ///     The type of context that these options are for. Will return <see cref="IDataContext" /> if the
        ///     options are not built for a specific derived context.
        /// </summary>
        public abstract Type ContextType { get; }

        /// <summary>
        ///     Specifies that no further configuration of this options object should occur.
        /// </summary>
        public virtual void Freeze() => IsFrozen = true;

        /// <summary>
        ///     Returns true if <see cref="Freeze" />. has been called.
        /// </summary>
        public virtual bool IsFrozen { get; private set; }
    }
}
