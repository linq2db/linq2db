using System;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using Common;
	using LinqToDB.Expressions;

	class OrderByBuilder : MethodCallBuilder
	{
		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			if (!methodCall.IsQueryable("OrderBy", "OrderByDescending", "ThenBy", "ThenByDescending", "ThenOrBy", "ThenOrByDescending"))
				return false;

			var body = ((LambdaExpression)methodCall.Arguments[1].Unwrap()).Body.Unwrap();

			if (body.NodeType == ExpressionType.MemberInit)
			{
				var mi = (MemberInitExpression)body;
				bool throwExpr;

				if (mi.NewExpression.Arguments.Count > 0 || mi.Bindings.Count == 0)
					throwExpr = true;
				else
					throwExpr = mi.Bindings.Any(b => b.BindingType != MemberBindingType.Assignment);

				if (throwExpr)
					throw new NotSupportedException($"Explicit construction of entity type '{body.Type}' in order by is not allowed.");
			}

			return true;
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

			if (sequence.SelectQuery.Select.TakeValue != null ||
				sequence.SelectQuery.Select.SkipValue != null ||
				sequence.SelectQuery.Select.IsDistinct /*&& !builder.DataContextInfo.SqlProviderFlags.IsDistinctOrderBySupported */)
				sequence = new SubQueryContext(sequence);

			var lambda  = (LambdaExpression)methodCall.Arguments[1].Unwrap();
			var sparent = sequence.Parent;
			var order   = new ExpressionContext(buildInfo.Parent, sequence, lambda);
			var body    = lambda.Body.Unwrap();
			var sql     = builder.ConvertExpressions(order, body, ConvertFlags.Key);

			builder.ReplaceParent(order, sparent);

			if (!methodCall.Method.Name.StartsWith("Then") && !Configuration.Linq.DoNotClearOrderBys)
				sequence.SelectQuery.OrderBy.Items.Clear();

			foreach (var expr in sql)
			{
				var e = builder.ConvertSearchCondition(sequence, expr.Sql);
				sequence.SelectQuery.OrderBy.Expr(e, methodCall.Method.Name.EndsWith("Descending"));
			}

			return sequence;
		}

		protected override SequenceConvertInfo Convert(
			ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo, ParameterExpression param)
		{
			return null;
		}
	}
}
