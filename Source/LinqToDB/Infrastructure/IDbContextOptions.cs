// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.EntityFrameworkCore.Infrastructure
{

	public interface IDbContextOptions
    {
        /// <summary>
        ///     Gets the extensions that store the configured options.
        /// </summary>
        IEnumerable<IDbContextOptionsExtension> Extensions { get; }

        /// <summary>
        ///     Gets the extension of the specified type. Returns null if no extension of the specified type is configured.
        /// </summary>
        /// <typeparam name="TExtension"> The type of the extension to get. </typeparam>
        /// <returns> The extension, or null if none was found. </returns>
        TExtension? FindExtension<TExtension>()
            where TExtension : class, IDbContextOptionsExtension;
    }
}
