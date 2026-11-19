using System.Collections.Generic;
using System.Linq.Expressions;

using LinqToDB.Mapping;

namespace LinqToDB.Internal.Linq
{
	sealed class QueryCacheCompareInfo
	{
		public Expression MainExpression { get; }

		public QueryCacheCompareInfo(Expression mainExpression, List<DynamicExpressionInfo>? dynamicAccessors, List<(ValueAccessorFunc main, ValueAccessorFunc other)>? comparisionFunctions, List<DependentValueCheck>? dependentValueChecks)
		{
			MainExpression       = mainExpression;
			DynamicAccessors     = dynamicAccessors;
			ComparisionFunctions = comparisionFunctions;
			DependentValueChecks = dependentValueChecks;
		}

		public delegate Expression ExpressionAccessorFunc(IDataContext dataContext,      MappingSchema mappingSchema);
		public delegate object?    ValueAccessorFunc(IQueryExpressions queryExpressions, IDataContext  dataContext, object?[]? compiledParameters);

		public sealed record DynamicExpressionInfo(int ExpressionId, Expression Used, MappingSchema MappingSchema, ExpressionAccessorFunc AccessorFunc);

		/// <summary>
		/// The value a <see cref="SqlQueryDependentAttribute"/> argument had when the query was built, for an argument the
		/// cached tree holds as a placeholder; <paramref name="Current"/> reads the argument of the tree being looked up.
		/// </summary>
		public sealed record DependentValueCheck(object? Value, ValueAccessorFunc Current, SqlQueryDependentAttribute Attribute);

		/// <summary>
		/// Contains functions for retrieving satellite expressions.
		/// </summary>
		public readonly List<DynamicExpressionInfo>? DynamicAccessors;

		/// <summary>
		/// Contains functions for retrieving values for comparison.
		/// </summary>
		public readonly List<(ValueAccessorFunc main, ValueAccessorFunc other)>?  ComparisionFunctions;

		/// <summary>
		/// Contains <see cref="SqlQueryDependentAttribute"/> argument values the main expression does not compare.
		/// </summary>
		public readonly List<DependentValueCheck>? DependentValueChecks;

		public bool IsFastComparable => DynamicAccessors == null && ComparisionFunctions == null && DependentValueChecks == null;
	}
}
