using System;

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
		public static QueryRunStep Create(Query ownerQuery, SqlValuesTable valuesTable, string tempTableName, MappingSchema mappingSchema)
		{
			var elementType = valuesTable.TempTableElementType
				?? throw new InvalidOperationException("SqlValuesTable.TempTableElementType must be set before the SQL builder registers the temp-table run step (see EnumerableBuilder.BuildConfigured).");

			var isScalar        = mappingSchema.IsScalarType(elementType);
			var stepElementType = isScalar
				? typeof(ValueHolder<>).MakeGenericType(elementType)
				: elementType;

			var stepType = typeof(CreateTempTableForValuesRunStep<>).MakeGenericType(stepElementType);

			return ActivatorExt.CreateInstance<QueryRunStep>(
				stepType,
				ownerQuery,
				valuesTable,
				tempTableName,
				isScalar);
		}
	}
}
