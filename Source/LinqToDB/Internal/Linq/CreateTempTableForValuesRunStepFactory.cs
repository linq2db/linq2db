using System;

using LinqToDB.Internal.Common;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.Linq
{
	/// <summary>
	/// Builds a generic <see cref="CreateTempTableForValuesRunStep{T}"/> sized to the
	/// <see cref="SqlValuesTable"/>'s shape. Dispatch:
	/// <list type="bullet">
	/// <item>Scalar element type → <see cref="ValueHolder{TInner}"/> wrapper — used by
	/// AsQueryable scalar sources and the scalar <c>UseTempTablesForContains</c>
	/// rewrite. <c>ValueHolder&lt;T&gt;.Value</c> carries the <c>[Column("item")]</c>
	/// annotation that lines up with the SQL builder's emission.</item>
	/// <item>Entity element type → the user's entity type directly. Used by AsQueryable
	/// entity sources and the multi-column entity <c>UseTempTablesForContains</c>
	/// rewrite. The temp table inherits the entity's <c>EntityDescriptor</c>, so any
	/// user-defined <c>ValueConverter</c> / <c>DataType</c> column overrides propagate
	/// to the BulkCopy and DDL — both sides of the EXISTS WHERE comparison see the same
	/// column type, including on strict-typing providers (PostgreSQL).</item>
	/// </list>
	/// </summary>
	internal static class CreateTempTableForValuesRunStepFactory
	{
		public static QueryRunStep Create(Query ownerQuery, SqlValuesTable valuesTable, string tempTableName, MappingSchema mappingSchema)
		{
			var elementType = valuesTable.TempTableElementType
				?? throw new InvalidOperationException("SqlValuesTable.TempTableElementType must be set before the SQL builder registers the temp-table run step (see EnumerableBuilder.BuildConfigured).");

			var isScalar = mappingSchema.IsScalarType(elementType);

			Type stepElementType;
			bool wrapScalarInValueHolder;

			if (isScalar)
			{
				stepElementType         = typeof(ValueHolder<>).MakeGenericType(elementType);
				wrapScalarInValueHolder = true;
			}
			else
			{
				// Entity / anonymous source — the temp table holds the user's element type
				// directly (AsQueryable entity, entity / composite-PK Contains rewrite).
				stepElementType         = elementType;
				wrapScalarInValueHolder = false;
			}

			var stepType = typeof(CreateTempTableForValuesRunStep<>).MakeGenericType(stepElementType);

			return ActivatorExt.CreateInstance<QueryRunStep>(
				stepType,
				ownerQuery,
				valuesTable,
				tempTableName,
				wrapScalarInValueHolder);
		}
	}
}
