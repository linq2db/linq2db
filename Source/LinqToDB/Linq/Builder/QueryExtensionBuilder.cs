using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using SqlQuery;

	class QueryExtensionBuilder : MethodCallBuilder
	{
		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return Sql.QueryExtensionAttribute.GetExtensionAttributes(methodCall, builder.MappingSchema).Length > 0;
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence   = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
			var parameters = new List<ISqlExpression>(methodCall.Arguments.Count - 1);

			for (var i = 1; i < methodCall.Arguments.Count; i++)
			{
				ISqlExpression expr;

				if (methodCall.Arguments[i].Unwrap() is LambdaExpression le)
				{
					var body = le.Body.Unwrap();

					if (le.Parameters.Count == 1)
					{
						var selector = new SelectContext(buildInfo.Parent, le, sequence);
						expr = builder.ConvertToSql(selector, body);
					}
					else
					{
						expr = builder.ConvertToSql(sequence, body);
					}
				}
				else
				{
					var ex   = methodCall.Arguments[i];
					var p    = methodCall.Method.GetParameters()[i];
					var attr = p.GetCustomAttributes(typeof(SqlQueryDependentAttribute), false).Cast<SqlQueryDependentAttribute>().FirstOrDefault();

					if (attr != null)
						ex = Expression.Constant(ex.EvaluateExpression());

					expr = builder.ConvertToSql(sequence, ex);
				}

				parameters.Add(expr);
			}

			var attrs = Sql.QueryExtensionAttribute.GetExtensionAttributes(methodCall, builder.MappingSchema);

			foreach (var attr in attrs)
			{
				switch (attr.Scope)
				{
					case Sql.QueryExtensionScope.Table:
					{
						var table = SequenceHelper.GetTableContext(sequence) ?? throw new LinqToDBException($"Cannot get table context from {sequence.GetType()}");

						break;
					}
				}
			}

			return sequence;
		}
	}
}
