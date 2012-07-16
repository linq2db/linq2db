using System;

namespace LinqToDB.Metadata
{
	public interface IMetadataReader
	{
		T[] GetAttributes<T>(Type type)                    where T : Attribute;
		T[] GetAttributes<T>(Type type, string memberName) where T : Attribute;
	}
}
