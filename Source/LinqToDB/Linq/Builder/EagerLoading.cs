using System;
using System.Linq;

namespace LinqToDB.Linq.Builder
{
	using Extensions;
	using Mapping;

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
