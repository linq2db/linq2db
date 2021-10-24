using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

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
			var sequence     = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
			var arguments    = new ISqlExpression[methodCall.Arguments.Count];
			var parameters   = new ParameterInfo?[arguments.Length];
			var methodParams = methodCall.Method.GetParameters();

			arguments[0] = new SqlValue(methodCall.Method.Name);

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
					var p    = methodParams[i];
					var attr = p.GetCustomAttributes(typeof(SqlQueryDependentAttribute), false).Cast<SqlQueryDependentAttribute>().FirstOrDefault();

					if (attr != null)
						ex = Expression.Constant(ex.EvaluateExpression());

					expr = builder.ConvertToSql(sequence, ex);
				}

				arguments [i] = expr;
				parameters[i] = methodParams[i];
			}

			var attrs = Sql.QueryExtensionAttribute.GetExtensionAttributes(methodCall, builder.MappingSchema);

			foreach (var attr in attrs)
			{
				switch (attr.Scope)
				{
					case Sql.QueryExtensionScope.Table:
					{
						var table = SequenceHelper.GetTableContext(sequence) ?? throw new LinqToDBException($"Cannot get table context from {sequence.GetType()}");
						attr.ExtendTable(table.SqlTable, parameters, arguments);
						break;
					}
				}
			}

			return sequence;
		}

		protected override SequenceConvertInfo? Convert(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo, ParameterExpression? param)
		{
			return base.Convert(builder, methodCall, buildInfo, param);
		}
	}
}
