using System;
using System.Collections.Generic;
using System.Globalization;
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
			var sequenceArgument = methodCall.Arguments[0];
			var sequenceResult   = builder.TryBuildSequence(new BuildInfo(buildInfo, sequenceArgument));

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

			var byIndex = false;

			List<SqlPlaceholderExpression> placeholders;
			while (true)
			{
				Expression sqlExpr;

				var body = SequenceHelper.PrepareBody(lambda, sequence).Unwrap();

				if (body is MethodCallExpression mc && mc.Method.DeclaringType == typeof(Sql) && mc.Method.Name == nameof(Sql.OrderIndex))
				{
					sqlExpr = builder.ConvertToSqlExpr(sequence, mc.Arguments[0], ProjectFlags.SQL);
					byIndex = true;
				}
				else
				{
					sqlExpr = builder.ConvertToSqlExpr(sequence, body, ProjectFlags.SQL);
					byIndex = false;
				}

				if (!SequenceHelper.IsSqlReady(sqlExpr))
				{
					if (sqlExpr is SqlErrorExpression errorExpr)
						return BuildSequenceResult.Error(methodCall, errorExpr.Message);
					return BuildSequenceResult.Error(methodCall);
				}

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

						orderSql     = new SqlExpression(typeof(int), position.ToString(CultureInfo.InvariantCulture));
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
