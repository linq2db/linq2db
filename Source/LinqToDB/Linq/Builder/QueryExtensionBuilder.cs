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
			var methodParams = methodCall.Method.GetParameters();
			var list         = new List<SqlQueryExtensionData>
			{
				new(".MethodName", methodCall, methodParams[0])
				{
					SqlExpression = new SqlValue(methodCall.Method.Name),
				}
			};

			for (var i = 1; i < methodCall.Arguments.Count; i++)
			{
				var arg  = methodCall.Arguments[i].Unwrap();
				var p    = methodParams[i];
				var name = p.Name;

				if (arg is LambdaExpression)
				{
					list.Add(new(name, arg, p));
				}
				else if (arg is NewArrayExpression ae)
				{
					list.Add(new($"{name}.Count", arg, p)
					{
						SqlExpression = new SqlValue(ae.Expressions.Count),
					});

					for (var j = 0; j < ae.Expressions.Count; j++)
						list.Add(new($"{name}.{j}", ae.Expressions[j], p, j));
				}
				else
				{
					var ex   = methodCall.Arguments[i];
					var attr = p.GetCustomAttributes(typeof(SqlQueryDependentAttribute), false).Cast<SqlQueryDependentAttribute>().FirstOrDefault();

					if (attr != null)
						ex = Expression.Constant(ex.EvaluateExpression());

					list.Add(new(name, ex, p));
				}
			}

			var dic = list.ToDictionary(t => t.Name);

			var sequence = builder.BuildSequence(new(buildInfo, methodCall.Arguments[0]));

			for (var i = 1; i < list.Count; i++)
			{
				var data = list[i];

				if (data.SqlExpression == null)
				{
					if (data.ParamsIndex >= 0)
					{
						data.SqlExpression = data.Expression.Unwrap() switch
						{
							LambdaExpression lex => builder.ConvertToExtensionSql(sequence, lex, null),
							var ex => builder.ConvertToSql(sequence, ex)
						};
					}
					else if (data.Expression is LambdaExpression le)
					{
						data.SqlExpression = builder.ConvertToExtensionSql(sequence, le, null);
					}
					else
					{
						data.SqlExpression = builder.ConvertToSql(sequence, data.Expression);
					}
				}

				/*
				var arg  = methodCall.Arguments[i].Unwrap();
				var name = methodParams[i].Name;

				if (arg is LambdaExpression le)
				{
					dic.Add(name, new SqlQueryExtensionData
					{
						Name          = name,
						Expression    = le,
						SqlExpression = builder.ConvertToExtensionSql(sequence, le, null)
					});
				}
				else if (arg is NewArrayExpression ae)
				{
					dic.Add($"{name}.Count", new SqlValue(ae.Expressions.Count));

					for (var j = 0; j < ae.Expressions.Count; j++)
					{
						dic.Add($"{name}.{j}", ae.Expressions[j].Unwrap() switch
						{
							LambdaExpression lex => builder.ConvertToExtensionSql(sequence, lex, null),
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
				*/
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
