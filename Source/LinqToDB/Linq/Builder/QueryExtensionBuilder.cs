using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder;

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

		var startIndex = methodCall.Object == null ? 1 : 0;

		for (var i = startIndex; i < methodCall.Arguments.Count; i++)
		{
			var arg  = methodCall.Arguments[i].Unwrap();
			var p    = methodParams[i];
			var name = p.Name!;

			if (arg is LambdaExpression)
			{
				list.Add(new(name, arg, p));
			}
			else if (arg is NewArrayExpression ae)
			{
				var attr = p.GetCustomAttributes(typeof(SqlQueryDependentAttribute), false).Cast<SqlQueryDependentAttribute>().FirstOrDefault();

				list.Add(new($"{name}.Count", arg, p)
				{
					SqlExpression = new SqlValue(ae.Expressions.Count),
				});

				for (var j = 0; j < ae.Expressions.Count; j++)
				{
					var ex = ae.Expressions[j];

					if (attr != null)
						ex = Expression.Constant(ex.EvaluateExpression());

					list.Add(new($"{name}.{j}", ex, p, j));
				}
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

		var attrs = Sql.QueryExtensionAttribute.GetExtensionAttributes(methodCall, builder.MappingSchema);

		var prevTablesInScope = builder.TablesInScope;

		if (attrs.Any(a => a.Scope == Sql.QueryExtensionScope.TablesInScopeHint))
			builder.TablesInScope = new();

		var sequence = builder.BuildSequence(new(buildInfo, methodCall.Object ?? methodCall.Arguments[0]));

		for (var i = startIndex; i < list.Count; i++)
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
		}

		List<SqlQueryExtension>? joinExtensions = null;

		foreach (var attr in attrs)
		{
			switch (attr.Scope)
			{
				case Sql.QueryExtensionScope.TableHint:
				case Sql.QueryExtensionScope.IndexHint:
				{
					var table = SequenceHelper.GetTableContext(sequence) ?? throw new LinqToDBException($"Cannot get table context from {sequence.GetType()}");
					attr.ExtendTable(table.SqlTable, list);
					break;
				}
				case Sql.QueryExtensionScope.TablesInScopeHint:
				{
					foreach (var table in builder.TablesInScope!)
						attr.ExtendTable(table.SqlTable, list);
					break;
				}
				case Sql.QueryExtensionScope.JoinHint:
				{
					attr.ExtendJoin(joinExtensions ??= new(), list);
					break;
				}
				case Sql.QueryExtensionScope.SubQueryHint:
				{
					attr.ExtendSubQuery(sequence.SelectQuery.SqlQueryExtensions ??= new(), list);
					break;
				}
				case Sql.QueryExtensionScope.QueryHint:
				{
					attr.ExtendQuery(builder.SqlQueryExtensions ??= new(), list);
					break;
				}
			}
		}

		builder.TablesInScope = prevTablesInScope;

		return joinExtensions != null ? new JoinHintContext(sequence, joinExtensions) : sequence;
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
