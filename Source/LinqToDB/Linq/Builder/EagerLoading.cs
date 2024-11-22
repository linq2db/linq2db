using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using Extensions;
	using Mapping;
	using SqlQuery;

	internal sealed class EagerLoading
	{
		public static Type GetEnumerableElementType(Type type, MappingSchema mappingSchema)
		{
			if (!IsEnumerableType(type, mappingSchema))
				return type;
			if (type.IsArray)
				return type.GetElementType()!;
			if (typeof(IGrouping<,>).IsSameOrParentOf(type))
				return type.GetGenericArguments()[1];
			return type.GetGenericArguments()[0];
		}

		public static bool IsEnumerableType(Type type, MappingSchema mappingSchema)
		{
			if (mappingSchema.IsScalarType(type))
				return false;
			if (!typeof(IEnumerable<>).IsSameOrParentOf(type))
				return false;
			return true;
		}
	}
}
