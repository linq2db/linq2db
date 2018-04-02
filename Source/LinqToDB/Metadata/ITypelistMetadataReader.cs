using System;
using System.Collections.Generic;

namespace LinqToDB.Metadata
{
	public interface ITypeListMetadataReader
	{
		IEnumerable<Type> GetMappedTypes();
	}
}
