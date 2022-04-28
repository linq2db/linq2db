using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using LinqToDB;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Microsoft.EntityFrameworkCore
{
    /// <summary>
    ///     The options to be used by a <see cref="IDataContext" />. 
    ///     to create instances of this class and it is not designed to be directly constructed in your application code.
    /// </summary>
    /// <typeparam name="TContext"> The type of the context these options apply to. </typeparam>
    public class DbContextOptions<TContext> : DbContextOptions
        where TContext : IDataContext
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="DbContextOptions{TContext}" /> class.
        ///     to create instances of this class and it is not designed to be directly constructed in your application code.
        /// </summary>
        public DbContextOptions()
            : base(new Dictionary<Type, IDbContextOptionsExtension>())
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="DbContextOptions{TContext}" /> class. 
        /// </summary>
        /// <param name="extensions"> The extensions that store the configured options. </param>
        public DbContextOptions(
            IReadOnlyDictionary<Type, IDbContextOptionsExtension> extensions)
            : base(extensions)
        {
        }

        /// <summary>
        ///     Adds the given extension to the underlying options and creates a new
        ///     <see cref="DbContextOptions" /> with the extension added.
        /// </summary>
        /// <typeparam name="TExtension"> The type of extension to be added. </typeparam>
        /// <param name="extension"> The extension to be added. </param>
        /// <returns> The new options instance with the given extension added. </returns>
        public override DbContextOptions WithExtension<TExtension>(TExtension extension)
        {
	        if (extension == null)
	        {
		        throw new ArgumentNullException(nameof(extension));
	        }

	        var extensions = Extensions.ToDictionary(p => p.GetType(), p => p);
            extensions[typeof(TExtension)] = extension;

			/*
			// propagate to base
            var current = typeof(TExtension);
            while (current.BaseType != null && current.BaseType != typeof(object))
            {
	            current = current.BaseType;
				if (extensions.ContainsKey(current))
					break;
				extensions[current] = extension;
            }
            */

            return new DbContextOptions<TContext>(extensions);
        }

        /// <summary>
        ///     The type of context that these options are for (<typeparamref name="TContext" />).
        /// </summary>
        public override Type ContextType => typeof(TContext);
    }
}
