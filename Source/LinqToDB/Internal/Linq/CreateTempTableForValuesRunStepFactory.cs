using System;
using System.Reflection;

using LinqToDB.Internal.Common;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.Linq
{
	/// <summary>
	/// Builds a generic <see cref="CreateTempTableForValuesRunStep{T}"/> sized to the
	/// <see cref="SqlValuesTable"/>'s element type. Scalars (int, string, …) are wrapped in
	/// <see cref="ValueHolder{TInner}"/> internally because <c>CreateTable&lt;T&gt;</c> needs an
	/// entity type with at least one mapped column. The column name <c>item</c> matches the
	/// implicit alias the inline-VALUES scalar path already uses.
	/// </summary>
	internal static class CreateTempTableForValuesRunStepFactory
	{
		public static QueryRunStep Create(Query ownerQuery, SqlValuesTable valuesTable, MappingSchema mappingSchema)
		{
			var elementType = ResolveElementType(valuesTable);

			var isScalar        = mappingSchema.IsScalarType(elementType);
			var stepElementType = isScalar
				? typeof(ValueHolder<>).MakeGenericType(elementType)
				: elementType;

			var stepType = typeof(CreateTempTableForValuesRunStep<>).MakeGenericType(stepElementType);

			return ActivatorExt.CreateInstance<QueryRunStep>(
				stepType,
				ownerQuery,
				valuesTable,
				isScalar);
		}

		// SqlValuesTable carries its element type via the table parameter — but it doesn't expose
		// one as a property. We recover it from the Source ISqlExpression's SystemType (the
		// IEnumerable<T> the parameter holds) or fall back to the field types if available.
		static Type ResolveElementType(SqlValuesTable valuesTable)
		{
			if (valuesTable.Source?.SystemType is { } sourceType)
			{
				if (sourceType.IsGenericType)
				{
					foreach (var iface in sourceType.GetInterfaces())
					{
						if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(System.Collections.Generic.IEnumerable<>))
							return iface.GetGenericArguments()[0];
					}

					if (sourceType.GetGenericTypeDefinition() == typeof(System.Collections.Generic.IEnumerable<>))
						return sourceType.GetGenericArguments()[0];
				}
			}

			throw new InvalidOperationException("Cannot resolve element type for SqlValuesTable temp-table materialization.");
		}
	}
}
