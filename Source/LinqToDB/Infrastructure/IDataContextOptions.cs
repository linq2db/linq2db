using System;
using System.Collections.Generic;

namespace LinqToDB.Infrastructure
{
	public interface IDataContextOptions
	{
		/// <summary>
		/// Gets the extensions that store the configured options.
		/// </summary>
		IEnumerable<IDataContextOptionsExtension> Extensions { get; }

		/// <summary>
		/// Gets the extension of the specified type. Returns null if no extension of the specified type is configured.
		/// </summary>
		/// <typeparam name="TExtension"> The type of the extension to get. </typeparam>
		/// <returns> The extension, or null if none was found. </returns>
		TExtension? FindExtension<TExtension>()
			where TExtension : class, IDataContextOptionsExtension;
	}
}
