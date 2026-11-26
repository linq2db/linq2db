using System.Collections.Generic;
using System.Linq.Expressions;

using LinqToDB.Internal.Expressions;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.Linq
{
	sealed class QueryCacheCompareInfo
	{
		public Expression MainExpression { get; }

		public QueryCacheCompareInfo(Expression mainExpression, List<DynamicExpressionInfo>? dynamicAccessors, List<ValueCompareFunc>? comparisionFunctions, List<DependentValueCheck>? dependentValueChecks)
		{
			MainExpression       = mainExpression;
			DynamicAccessors     = dynamicAccessors;
			ComparisionFunctions = comparisionFunctions;
			DependentValueChecks = dependentValueChecks;
		}

		public delegate Expression ExpressionAccessorFunc(IDataContext dataContext,      MappingSchema mappingSchema);
		public delegate object?    ValueAccessorFunc(IQueryExpressions queryExpressions, IDataContext  dataContext, object?[]? compiledParameters);
		public delegate bool       ValueCompareFunc(IQueryExpressions queryExpressions, IDataContext  dataContext, object?[]? compiledParameters);

		public sealed record DynamicExpressionInfo(int ExpressionId, Expression Key, int KeyHashCode, Expression Used, MappingSchema MappingSchema, ExpressionAccessorFunc AccessorFunc);

		/// <summary>
		/// The dynamic accessors evaluated for one context in one cache lookup. The queries cached for one shape register their
		/// accessors under the same keys, and a key stands for one function of the context, as it does within a query: each
		/// runs once per lookup, not once per cached query. A lookup keeps the results only when it may compare more than one
		/// cached query.
		/// </summary>
		public struct DynamicAccessorResults(bool memoize)
		{
			readonly bool                                           _memoize = memoize;
			DynamicExpressionInfo?                                  _firstInfo;
			Expression?                                             _firstResult;
			List<(DynamicExpressionInfo info, Expression result)>? _more;

			public Expression Get(IDataContext dataContext, DynamicExpressionInfo info)
			{
				if (!_memoize)
					return info.AccessorFunc(dataContext, info.MappingSchema);

				if (_firstInfo != null)
				{
					if (IsSameAccessor(_firstInfo, info))
						return _firstResult!;

					if (_more != null)
					{
						foreach (var (evaluated, result) in _more)
						{
							if (IsSameAccessor(evaluated, info))
								return result;
						}
					}
				}

				var current = info.AccessorFunc(dataContext, info.MappingSchema);

				if (_firstInfo == null)
				{
					_firstInfo   = info;
					_firstResult = current;
				}
				else
				{
					(_more ??= new()).Add((info, current));
				}

				return current;
			}

			static bool IsSameAccessor(DynamicExpressionInfo x, DynamicExpressionInfo y)
			{
				return ReferenceEquals(x, y)
					|| x.KeyHashCode == y.KeyHashCode
					&& x.AccessorFunc.Method == y.AccessorFunc.Method
					&& x.MappingSchema.Equals(y.MappingSchema)
					&& ExpressionEqualityComparer.Instance.Equals(x.Key, y.Key);
			}
		}

		/// <summary>
		/// The value a <see cref="SqlQueryDependentAttribute"/> argument had when the query was built, for an argument the
		/// cached tree does not compare by value; <paramref name="Current"/> reads the argument of the tree being looked up.
		/// </summary>
		public sealed record DependentValueCheck(object? Value, ValueAccessorFunc Current, SqlQueryDependentAttribute Attribute);

		/// <summary>
		/// Contains functions for retrieving satellite expressions.
		/// </summary>
		public readonly List<DynamicExpressionInfo>? DynamicAccessors;

		/// <summary>
		/// Contains functions comparing a value the query was built with against the current one (typed, without boxing value types).
		/// </summary>
		public readonly List<ValueCompareFunc>?  ComparisionFunctions;

		/// <summary>
		/// Contains <see cref="SqlQueryDependentAttribute"/> argument values the main expression does not compare.
		/// </summary>
		public readonly List<DependentValueCheck>? DependentValueChecks;

		// DependentValueChecks do not count: an IQueryable that keeps its query runs them on each execution.
		public bool IsFastComparable => DynamicAccessors == null && ComparisionFunctions == null;
	}
}
