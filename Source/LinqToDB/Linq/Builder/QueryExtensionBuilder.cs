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
			var methodParams = methodCall.Method.GetParameters();

			var dic = new Dictionary<string,ISqlExpression>
			{
				{ ".MethodName", new SqlValue(methodCall.Method.Name) }
			};

			for (var i = 1; i < methodCall.Arguments.Count; i++)
			{
				var arg  = methodCall.Arguments[i].Unwrap();
				var name = methodParams[i].Name!;

				if (arg is LambdaExpression le)
				{
					dic.Add(name, GetExpression(le));
				}
				else if (arg is NewArrayExpression ae)
				{
					dic.Add($"{name}.Count", new SqlValue(ae.Expressions.Count));

					for (var j = 0; j < ae.Expressions.Count; j++)
					{
						dic.Add($"{name}.{j}", ae.Expressions[j].Unwrap() switch
						{
							LambdaExpression lex => GetExpression(lex),
							var ex               => builder.ConvertToSql(sequence, ex)
						});
					}
				}
				else
				{
					var ex   = methodCall.Arguments[i];
					var p    = methodParams[i];
					var attr = p.GetCustomAttributes(typeof(SqlQueryDependentAttribute), false).Cast<SqlQueryDependentAttribute>().FirstOrDefault();

					if (attr != null)
						ex = Expression.Constant(ex.EvaluateExpression());

					dic.Add(name, builder.ConvertToSql(sequence, ex));
				}

				ISqlExpression GetExpression(LambdaExpression le)
				{
					var body = le.Body.Unwrap();

					if (le.Parameters.Count == 1)
					{
						var selector = new SelectContext(buildInfo.Parent, le, sequence);
						return builder.ConvertToSql(selector, body);
					}

					return builder.ConvertToSql(sequence, body);
				}
			}

			var attrs = Sql.QueryExtensionAttribute.GetExtensionAttributes(methodCall, builder.MappingSchema);

			List<SqlQueryExtension>? joinExtensions = null;

			foreach (var attr in attrs)
			{
				switch (attr.Scope)
				{
					case Sql.QueryExtensionScope.Table:
					{
						var table = SequenceHelper.GetTableContext(sequence) ?? throw new LinqToDBException($"Cannot get table context from {sequence.GetType()}");
						attr.ExtendTable(table.SqlTable, dic);
						break;
					}
					case Sql.QueryExtensionScope.Join:
					{
						attr.ExtendJoin(joinExtensions ??= new(), dic);
						break;
					}
					case Sql.QueryExtensionScope.Query:
					{
						attr.ExtendQuery(builder.SqlQueryExtensions ??= new(), dic);
						break;
					}
				}
			}

			return joinExtensions != null ? new JoinHintContext(sequence, joinExtensions) : sequence;
		}

		protected override SequenceConvertInfo? Convert(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo, ParameterExpression? param)
		{
			return base.Convert(builder, methodCall, buildInfo, param);
		}

		public class JoinHintContext : PassThroughContext
		{
			public JoinHintContext(IBuildContext context, List<SqlQueryExtension> extensions)
				: base(context)
			{
				Extensions = extensions;
			}

			public List<SqlQueryExtension> Extensions { get; }
		}
	}
}
