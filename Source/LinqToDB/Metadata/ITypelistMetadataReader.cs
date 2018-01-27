using System;
using System.Collections.Generic;

namespace LinqToDB.Metadata
{
	public interface ITypelistMetadataReader
	{
		IEnumerable<Type> GetMappedTypes();
	}
}
