#if NET9_0_OR_GREATER

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

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

		static readonly MethodInfo _buildIndexMethodInfo = MemberHelper.MethodOfGeneric(() => BuildIndex<int>(null!));

		static Expression BuildIndex<T>(Expression sequenceExpression)
		{
			// Lower Index() to Select((item, index) => new ValueTuple<int, T>(index, item))
			// This reuses SelectBuilder's CounterContext which correctly generates ROW_NUMBER()
			// and handles WHERE, correlation, etc.
			//
			// When the source is IEnumerable (e.g. navigation property p.Children.OrderBy(...)),
			// we must use the Enumerable.Select overload. Otherwise use Queryable.Select.
			if (typeof(IQueryable<T>).IsAssignableFrom(sequenceExpression.Type))
			{
				return ExpressionHelpers.MakeCall(
					(IQueryable<T> source) => source.Select((item, index) => new ValueTuple<int, T>(index, item)),
					sequenceExpression);
			}

			return ExpressionHelpers.MakeCall(
				(IEnumerable<T> source) => source.Select((item, index) => new ValueTuple<int, T>(index, item)),
				sequenceExpression);
		}

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			if (!builder.DataContext.SqlProviderFlags.IsWindowFunctionsSupported)
			{
				return BuildSequenceResult.Error(methodCall, ErrorHelper.Error_RowNumber);
			}

			var sequenceExpression = methodCall.Arguments[0];
			var elementType        = methodCall.Method.GetGenericArguments()[0];

			var buildMethod = _buildIndexMethodInfo.MakeGenericMethod(elementType);
			var expression  = (Expression)buildMethod.InvokeExt(null, [sequenceExpression])!;

			return builder.TryBuildSequence(new BuildInfo(buildInfo, expression));
		}
	}
}

#endif
