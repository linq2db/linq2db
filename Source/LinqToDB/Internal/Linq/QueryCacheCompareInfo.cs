using System.Collections.Generic;
using System.Linq.Expressions;

using LinqToDB.Mapping;

namespace LinqToDB.Internal.Linq
{
	class QueryCacheCompareInfo
	{
		public Expression MainExpression { get; }

		public QueryCacheCompareInfo(Expression mainExpression, List<DynamicExpressionInfo>? dynamicAccessors, List<(ValueAccessorFunc main, ValueAccessorFunc other)>? comparisionFunctions)
		{
			MainExpression       = mainExpression;
			DynamicAccessors     = dynamicAccessors;
			ComparisionFunctions = comparisionFunctions;
		}

		public delegate Expression ExpressionAccessorFunc(IDataContext dataContext,      MappingSchema mappingSchema);
		public delegate object?    ValueAccessorFunc(IQueryExpressions queryExpressions, IDataContext  dataContext, object?[]? compiledParameters);

		public record DynamicExpressionInfo(int ExpressionId, Expression Used, MappingSchema MappingSchema, ExpressionAccessorFunc AccessorFunc);

		/// <summary>
		/// Contains functions for retrieving satellite expressions.
		/// </summary>
		public readonly List<DynamicExpressionInfo>? DynamicAccessors;

		/// <summary>
		/// Contains functions for retrieving values for comparison.
		/// </summary>
		public readonly List<(ValueAccessorFunc main, ValueAccessorFunc other)>?  ComparisionFunctions;

		public bool IsFastComparable => DynamicAccessors == null && ComparisionFunctions == null;
	}
}
