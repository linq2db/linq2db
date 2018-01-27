using System;
using System.Collections.Generic;

namespace LinqToDB.Metadata
{
	/// <summary>
	/// Provides access to types, mapped by metadata reader. Supported only by metadata providers that know their types
	/// beforehand, like fluent metadata provider.
	/// </summary>
	public interface ITypeListMetadataReader : IMetadataReader
    {
		/// <summary>
		/// Returns list of types, mapped by metadata reader.
		/// </summary>
		/// <returns>List of mapped types.</returns>
		IEnumerable<Type> GetMappedTypes();
	}
}
