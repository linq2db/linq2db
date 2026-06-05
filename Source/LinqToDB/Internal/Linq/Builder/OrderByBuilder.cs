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
			if (!call.IsQueryable)
				return false;

			// Only the 2-argument key-selector overloads and the linq2db 3-argument Sql.NullsPosition overloads
			// are translatable. Decline anything else (e.g. the BCL OrderBy/ThenBy overloads taking an
			// IComparer<TKey>, which has no SQL equivalent) instead of translating it as if the extra argument
			// were absent.
			var parameters = call.Method.GetParameters();
			if (parameters.Length > 2 && parameters[2].ParameterType != typeof(Sql.NullsPosition))
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

			var isNewOrder = methodCall.Method.Name.StartsWith(nameof(Queryable.OrderBy), StringComparison.Ordinal) && !builder.DataContext.Options.LinqOptions.ConcatenateOrderBy;

			if (isNewOrder)
				sequence = new SubQueryContext(sequence);

			if (builder.DataContext.Options.LinqOptions.ConcatenateOrderBy)
			{
				var prevSequence = sequence;

				if (sequence is not SubQueryContext)
					sequence = new SubQueryContext(sequence);

				if (builder.DataContext.Options.LinqOptions.ConcatenateOrderBy)
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

			if (body is MethodCallExpression mc && mc.Method.DeclaringType == typeof(Sql) && string.Equals(mc.Method.Name, nameof(Sql.Ordinal), StringComparison.Ordinal))
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

			// The new OrderBy/ThenBy overloads accept an explicit Sql.NullsPosition as the third argument.
			// (The BCL IComparer<TKey> 3-arg overloads are rejected up front in CanBuildMethod.)
			var nullsSpecified = methodCall.Arguments.Count > 2
				&& methodCall.Method.GetParameters()[2].ParameterType == typeof(Sql.NullsPosition);
			var nullsPosition = nullsSpecified
				? (Sql.NullsPosition)builder.EvaluateExpression(methodCall.Arguments[2])!
				: Sql.NullsPosition.None;

			// When no position is specified, fall back to the configured default. An explicit Sql.NullsPosition.None
			// is honored as-is (opt out of the default) and must not be treated as "unspecified".
			var defaultNulls = builder.DataOptions.SqlOptions.DefaultNullsPosition;

			var placeholders = ExpressionBuilder.CollectDistinctPlaceholders(sqlExpr, false);

			var addedOrderBy = false;

			foreach (var placeholder in placeholders)
			{
				var orderSql = placeholder.Sql;

				var isPositioned = byIndex;
				var itemNulls    = nullsPosition;

				if (QueryHelper.IsConstant(placeholder.Sql))
				{
					if (builder.DataOptions.SqlOptions.EnableConstantExpressionInOrderBy && orderSql is SqlValue { Value: int position })
					{
						// Dangerous way to set oder ordinal position. Used for legacy software support.
						if (position <= 0)
							return BuildSequenceResult.Error(sequenceArgument, $"Invalid Index '{position.ToString(CultureInfo.InvariantCulture)}' for positioned OrderBy. Should be in range 1..N.");

						orderSql     = new SqlFragment(position.ToString(CultureInfo.InvariantCulture));
						isPositioned = false;
						// NULLS position on a literal ordinal index is meaningless.
						itemNulls    = Sql.NullsPosition.None;
					}
					else
					{
						// we do not need sorting by immutable values, like "Some", Func("Some"), "Some1" + "Some2". It does nothing for ordering
						continue;
					}
				}
				else if (!nullsSpecified && !byIndex)
				{
					// No explicit position on a normal ordering key — apply the configured default.
					itemNulls = defaultNulls;
				}

				sequence.SelectQuery.OrderBy.Expr(orderSql, methodCall.Method.Name.EndsWith("Descending", StringComparison.Ordinal), isPositioned, itemNulls);
				addedOrderBy = true;
			}

			// Record the prepared OrderBy body on the builder so eager-load strategies (CteUnion)
			// can recover user-visible ordering. Skip positional Sql.Ordinal (no meaningful expression
			// for window-function OVER) and constant-only orderings (no SQL items emitted).
			if (addedOrderBy && !byIndex)
			{
				var name       = methodCall.Method.Name;
				var descending = name.EndsWith("Descending", StringComparison.Ordinal);
				var resetOrder = name.StartsWith(nameof(Queryable.OrderBy), StringComparison.Ordinal);
				builder.RegisterOrderBy(body, descending, nullsSpecified ? nullsPosition : defaultNulls, reset: resetOrder);
			}

			return BuildSequenceResult.FromContext(sequence);
		}
	}
}
