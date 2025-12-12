using System;
using System.Linq;

using LinqToDB.Internal.Extensions;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.Linq.Builder
{
	internal sealed class EagerLoading
	{
		public static Type GetEnumerableElementType(Type type, MappingSchema mappingSchema)
		{
			if (!mappingSchema.IsCollectionType(type))
				// TODO: we depend on IsCollectionType implementation here, need schema-specific method instead
				return type.GetGenericArguments()[0];
			if (type.IsArray)
				return type.GetElementType()!;
			if (typeof(IGrouping<,>).IsSameOrParentOf(type))
				return type.GetGenericArguments()[1];
			return type.GetGenericArguments()[0];
		}
	}
}
