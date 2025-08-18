using System;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.SqlQuery;

namespace LinqToDB.Internal.Linq.Builder
{
	[BuildsMethodCall("OrderBy", "OrderByDescending", "ThenBy", "ThenByDescending", "ThenOrBy", "ThenOrByDescending")]
	sealed class OrderByBuilder : MethodCallBuilder
	{
		public static bool CanBuildMethod(MethodCallExpression call)
		{
			if (!call.IsQueryable())
				return false;

			var body = call.Arguments[1].UnwrapLambda().Body.Unwrap();
			if (body.NodeType == ExpressionType.MemberInit)
			{
				var mi = (MemberInitExpression)body;
				if (mi.NewExpression.Arguments.Count > 0 || 
					mi.Bindings.Count == 0 ||
					mi.Bindings.Any(b => b.BindingType != MemberBindingType.Assignment))
				{
					throw new NotSupportedException($"Explicit construction of entity type '{body.Type}' in order by is not allowed.");
				}
			}

			return true;
		}

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequenceArgument = methodCall.Arguments[0];
			var sequenceResult   = builder.TryBuildSequence(new BuildInfo(buildInfo, sequenceArgument));

			if (sequenceResult.BuildContext == null)
				return sequenceResult;

			if (buildInfo.IgnoreOrderBy)
				return sequenceResult;

			var sequence = sequenceResult.BuildContext;

			var isNewOrder = methodCall.Method.Name.StartsWith(nameof(Queryable.OrderBy)) && !builder.DataContext.Options.LinqOptions.DoNotClearOrderBys;

			if (isNewOrder)
				sequence = new SubQueryContext(sequence);

			if (builder.DataContext.Options.LinqOptions.DoNotClearOrderBys)
			{
				var prevSequence = sequence;

				if (sequence is not SubQueryContext)
					sequence = new SubQueryContext(sequence);

				if (builder.DataContext.Options.LinqOptions.DoNotClearOrderBys)
				{
					var isValid = true;
					while (true)
					{
						if (prevSequence.SelectQuery.Select.HasModifier)
						{
							isValid = false;
							break;
						}

						if (!prevSequence.SelectQuery.OrderBy.IsEmpty)
							break;

						if (prevSequence is SubQueryContext { IsSelectWrapper: true } subQuery)
						{
							prevSequence = subQuery.SubQuery;
						}
						else if (prevSequence is SelectContext { InnerContext: not null } selectContext)
						{
							prevSequence = selectContext.InnerContext;
						}
						else
							break;
					}

					if (isValid && !prevSequence.SelectQuery.OrderBy.IsEmpty && sequence.SelectQuery != prevSequence.SelectQuery)
					{
						sequence.SelectQuery.OrderBy.Items.AddRange(prevSequence.SelectQuery.OrderBy.Items.Select(x => x.Clone()));
					}
				}
			}

			var lambda = methodCall.Arguments[1].UnwrapLambda();
			var body   = SequenceHelper.PrepareBody(lambda, sequence).Unwrap();

			Expression sqlExpr;
			bool       byIndex;

			if (body is MethodCallExpression mc && mc.Method.DeclaringType == typeof(Sql) && mc.Method.Name == nameof(Sql.Ordinal))
			{
				sqlExpr = builder.BuildSqlExpression(sequence, mc.Arguments[0], BuildPurpose.Sql, BuildFlags.ForKeys);
				byIndex = true;
			}
			else
			{
				sqlExpr = builder.BuildSqlExpression(sequence, body, BuildPurpose.Sql, BuildFlags.ForKeys);
				byIndex = false;
			}

			if (!SequenceHelper.IsSqlReady(sqlExpr))
			{
				if (sqlExpr is SqlErrorExpression errorExpr)
					return BuildSequenceResult.Error(sqlExpr, errorExpr.Message);
				return BuildSequenceResult.Error(methodCall);
			}

			var placeholders = ExpressionBuilder.CollectDistinctPlaceholders(sqlExpr, false);

			foreach (var placeholder in placeholders)
			{
				var orderSql = placeholder.Sql;

				var isPositioned = byIndex;

				if (QueryHelper.IsConstant(placeholder.Sql))
				{
					if (builder.DataOptions.SqlOptions.EnableConstantExpressionInOrderBy && orderSql is SqlValue { Value: int position })
					{
						// Dangerous way to set oder ordinal position. Used for legacy software support.
						if (position <= 0)
							return BuildSequenceResult.Error(sequenceArgument, $"Invalid Index '{position.ToString(CultureInfo.InvariantCulture)}' for positioned OrderBy. Should be in range 1..N.");

						orderSql     = new SqlFragment(position.ToString(CultureInfo.InvariantCulture));
						isPositioned = false;
					}
					else
					{
						// we do not need sorting by immutable values, like "Some", Func("Some"), "Some1" + "Some2". It does nothing for ordering
						continue;
					}
				}

				sequence.SelectQuery.OrderBy.Expr(orderSql, methodCall.Method.Name.EndsWith("Descending"), isPositioned);
			}

			return BuildSequenceResult.FromContext(sequence);
		}
	}
}
