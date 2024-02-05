using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using SqlQuery;
	using LinqToDB.Expressions;

	sealed class OrderByBuilder : MethodCallBuilder
	{
		private static readonly string[] MethodNames = { "OrderBy", "OrderByDescending", "ThenBy", "ThenByDescending", "ThenOrBy", "ThenOrByDescending" };

		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			if (!methodCall.IsQueryable(MethodNames))
				return false;

			var body = methodCall.Arguments[1].UnwrapLambda().Body.Unwrap();

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

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequenceResult = builder.TryBuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

			if (sequenceResult.BuildContext == null)
				return sequenceResult;

			var sequence = sequenceResult.BuildContext;

			var wrapped = false;

			if (sequence.SelectQuery.Select.HasModifier)
			{
				sequence = new SubQueryContext(sequence);
				wrapped = true;
			}

			var isContinuousOrder = !sequence.SelectQuery.OrderBy.IsEmpty && methodCall.Method.Name.StartsWith("Then");
			var lambda  = (LambdaExpression)methodCall.Arguments[1].Unwrap();

			List<SqlPlaceholderExpression> placeholders;

			while (true)
			{
				var body = SequenceHelper.PrepareBody(lambda, sequence).Unwrap();

				var sqlExpr = builder.BuildSqlExpression(sequence, body, ProjectFlags.SQL);

				if (!SequenceHelper.IsSqlReady(sqlExpr))
					return BuildSequenceResult.Error(methodCall);

				placeholders = ExpressionBuilder.CollectDistinctPlaceholders(sqlExpr);

				// Do not create subquery for ThenByExtensions
				//
				if (wrapped || isContinuousOrder)
					break;

				// handle situation when order by uses complex field
				//
				var isComplex = false;

				foreach (var placeholder in placeholders)
				{
					// immutable expressions will be removed later
					//
					var isImmutable = QueryHelper.IsConstant(placeholder.Sql);
					if (isImmutable)
						continue;

					// possible we have to extend this list
					//
					isComplex = null != placeholder.Sql.Find(e => e.ElementType == QueryElementType.SqlQuery || e.ElementType == QueryElementType.SqlFunction);
					if (isComplex)
						break;
				}

				if (!isComplex)
					break;

				sequence = new SubQueryContext(sequence);
				wrapped = true;
			}

			if (!isContinuousOrder && !builder.DataContext.Options.LinqOptions.DoNotClearOrderBys)
				sequence.SelectQuery.OrderBy.Items.Clear();

			foreach (var expr in placeholders)
			{
				// we do not need sorting by immutable values, like "Some", Func("Some"), "Some1" + "Some2". It does nothing for ordering
				// IT: Actually it does. See ORDER BY ordinal position.
				//
				if (!builder.DataOptions.SqlOptions.EnableConstantExpressionInOrderBy && QueryHelper.IsConstant(expr.Sql))
					continue;

				sequence.SelectQuery.OrderBy.Expr(expr.Sql, methodCall.Method.Name.EndsWith("Descending"));
			}

			return BuildSequenceResult.FromContext(sequence);
		}
	}
}
