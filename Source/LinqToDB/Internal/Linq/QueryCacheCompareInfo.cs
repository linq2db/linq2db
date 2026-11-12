using System.Collections.Generic;
using System.Linq.Expressions;

using LinqToDB.Mapping;

namespace LinqToDB.Internal.Linq
{
	sealed class QueryCacheCompareInfo
	{
		public Expression MainExpression { get; }

		public QueryCacheCompareInfo(Expression mainExpression, List<DynamicExpressionInfo>? dynamicAccessors, List<ValueCompareFunc>? comparisionFunctions)
		{
			MainExpression       = mainExpression;
			DynamicAccessors     = dynamicAccessors;
			ComparisionFunctions = comparisionFunctions;
		}

		public delegate Expression ExpressionAccessorFunc(IDataContext dataContext,      MappingSchema mappingSchema);
		public delegate bool       ValueCompareFunc(IQueryExpressions queryExpressions, IDataContext  dataContext, object?[]? compiledParameters);

		public sealed record DynamicExpressionInfo(int ExpressionId, Expression Used, MappingSchema MappingSchema, ExpressionAccessorFunc AccessorFunc);

		/// <summary>
		/// Contains functions for retrieving satellite expressions.
		/// </summary>
		public readonly List<DynamicExpressionInfo>? DynamicAccessors;

		/// <summary>
		/// Contains functions comparing a value the query was built with against the current one (typed, without boxing value types).
		/// </summary>
		public readonly List<ValueCompareFunc>?  ComparisionFunctions;

		public bool IsFastComparable => DynamicAccessors == null && ComparisionFunctions == null;
	}
}
