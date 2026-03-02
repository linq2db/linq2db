#if NET9_0_OR_GREATER

using System;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Expressions;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Reflection;

namespace LinqToDB.Internal.Linq.Builder
{
	[BuildsMethodCall(nameof(Queryable.Index))]
	sealed class IndexBuilder : MethodCallBuilder
	{
		public static bool CanBuildMethod(MethodCallExpression call)
			=> call.IsSameGenericMethod(Methods.Enumerable.Index, Methods.Queryable.Index);

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			if (!builder.DataContext.SqlProviderFlags.IsWindowFunctionsSupported)
			{
				return BuildSequenceResult.Error(methodCall, ErrorHelper.Error_RowNumber);
			}

			var sequenceExpression = methodCall.Arguments[0];
			var elementType = methodCall.Method.GetGenericArguments()[0];

			// Extract OrderBy part - Index() requires ordering to be deterministic in SQL
			var orderByPart = WindowFunctionHelpers.ExtractOrderByPart(sequenceExpression, out var nonOrderedPart);
			if (orderByPart.Length == 0)
				return BuildSequenceResult.Error(sequenceExpression, ErrorHelper.Error_IndexRequiresOrderBy);

			// Build sequence from non-ordered part first
			var sequenceBuildResult = builder.TryBuildSequence(new BuildInfo(buildInfo, nonOrderedPart));
			if (sequenceBuildResult.BuildContext == null)
				return sequenceBuildResult;

			var sequence = sequenceBuildResult.BuildContext;

			// Prepare the orderBy expressions within the sequence context
			var preparedOrderBy = orderByPart
				.Select(o => (SequenceHelper.PrepareBody(o.lambda, sequence), o.isDescending))
				.ToArray();

			// Build ROW_NUMBER() using the prepared order expressions
			var rowNumberCall = WindowFunctionHelpers.BuildRowNumber(Array.Empty<Expression>(), preparedOrderBy);

			// Build the transformation
			var resultType = typeof(ValueTuple<,>).MakeGenericType(typeof(int), elementType);
			var itemParam = Expression.Parameter(elementType, "item");

			// ROW_NUMBER() is 1-based, Index() is 0-based by default
			var convertToInt = Expression.Convert(rowNumberCall, typeof(int));

			// (rowNumber - 1)
			Expression adjustedIndex = Expression.Subtract(convertToInt, Expression.Constant(1));

			// Build: new ValueTuple<int, T>(index, item)
			var tupleConstructor = resultType.GetConstructor(new[] { typeof(int), elementType })!;
			var newTuple = Expression.New(tupleConstructor, adjustedIndex, itemParam);

			var selectLambda = Expression.Lambda(newTuple, itemParam);

			// Create a ContextRefExpression to represent the sequence query
			var contextRef = new ContextRefExpression(typeof(IQueryable<>).MakeGenericType(elementType), sequence);

			// Build the Select call expression manually
			var selectMethod = Methods.Queryable.Select.MakeGenericMethod(elementType, resultType);
			var selectExpression = Expression.Call(selectMethod, contextRef, Expression.Quote(selectLambda));

			// Ensure stable enumeration order by ordering by the computed index (Item1 of the tuple)
			var tupleParam = Expression.Parameter(resultType, "t");
			var indexSelector = Expression.Lambda(
				Expression.PropertyOrField(tupleParam, "Item1"),
				tupleParam);
			var orderedExpression = Expression.Call(
				typeof(Queryable),
				nameof(Queryable.OrderBy),
				new[] { resultType, typeof(int) },
				selectExpression,
				Expression.Quote(indexSelector));
			return builder.TryBuildSequence(new BuildInfo(buildInfo, orderedExpression));
		}
	}
}

#endif
