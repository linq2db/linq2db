using System;
using System.Linq;

using LinqToDB.Extensions;
using LinqToDB.Internal.Extensions;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.Linq.Builder
{
	internal sealed class EagerLoading
	{
		public static Type GetEnumerableElementType(Type type, MappingSchema mappingSchema)
		{
			if (!mappingSchema.IsCollectionType(type))
				return type;
			if (type.IsArray)
				return type.GetElementType()!;
			if (typeof(IGrouping<,>).IsSameOrParentOf(type))
				return type.GetGenericArguments()[1];
			return type.GetGenericArguments()[0];
		}
	}
}
