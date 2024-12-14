using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using Extensions;
	using SqlQuery;

	[BuildsExpression(ExpressionType.Call)]
	sealed class QueryExtensionBuilder : MethodCallBuilder
	{
		public static bool CanBuild(Expression expr, BuildInfo info, ExpressionBuilder builder)
			=> Sql.QueryExtensionAttribute.GetExtensionAttributes(expr, builder.MappingSchema).Length > 0;

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
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
					list.Add(new($"{name}.Count", arg, p)
					{
						SqlExpression = new SqlValue(ae.Expressions.Count),
					});

					for (var j = 0; j < ae.Expressions.Count; j++)
					{
						var ex = ae.Expressions[j];

						list.Add(new(FormattableString.Invariant($"{name}.{j}"), ex, p, j));
					}
				}
				else
				{
					var ex   = methodCall.Arguments[i];

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
						var converted = data.Expression.Unwrap() switch
						{
							LambdaExpression lex => builder.BuildSqlExpression(sequence, SequenceHelper.PrepareBody(lex, sequence)),
							var ex => builder.BuildSqlExpression(sequence, ex)
						};

						if (converted is SqlPlaceholderExpression placeholder)
							data.SqlExpression = placeholder.Sql;
						else
							return BuildSequenceResult.Error(methodCall);
					}
					else if (data.Expression is LambdaExpression le)
					{
						var converted = builder.ConvertToExtensionSql(sequence, le, null, null);

						if (converted is SqlPlaceholderExpression placeholder)
							data.SqlExpression = placeholder.Sql;
						else
							return BuildSequenceResult.Error(methodCall);
					}
					else
					{
						//TODO: These QueryExtensions needs tp be rewritten completely. Workaround over DateTime.

						var isQueryDepended = data.Parameter.GetAttributes<SqlQueryDependentAttribute>().Length > 0;

						var additionalFlags = isQueryDepended ? BuildFlags.None : BuildFlags.ForceParameter;

						var converted = builder.BuildSqlExpression(sequence, data.Expression, BuildPurpose.Sql, buildFlags : additionalFlags);

						if (converted is SqlPlaceholderExpression placeholder)
							data.SqlExpression = placeholder.Sql;
						else
							return BuildSequenceResult.Error(methodCall);
					}
				}
			}

			List<SqlQueryExtension>? joinExtensions = null;

			foreach (var attr in attrs)
			{
				switch (attr.Scope)
				{
					case Sql.QueryExtensionScope.TableHint    :
					case Sql.QueryExtensionScope.IndexHint    :
					case Sql.QueryExtensionScope.TableNameHint:
					{
						var table = SequenceHelper.GetTableOrCteContext(sequence) ?? throw new LinqToDBException($"Cannot get table context from {sequence.GetType()}");
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
						if (sequence is SetOperationBuilder.SetOperationContext { SubQuery.SelectQuery : { HasSetOperators: true } q })
							attr.ExtendSubQuery(q.SetOperators[^1].SelectQuery.SqlQueryExtensions ??= new(), list);
						else
						{
							var queryToUpdate = sequence.SelectQuery;
							if (sequence is AsSubqueryContext { SelectQuery.IsSimple: true } subquery)
							{
								queryToUpdate = subquery.SubQuery.SelectQuery;
							}

							if (!queryToUpdate.IsSimple)
							{
								sequence      = new SubQueryContext(sequence);
								queryToUpdate = sequence.SelectQuery;
							}

							attr.ExtendSubQuery(queryToUpdate.SqlQueryExtensions ??= new(), list);
						}
						break;
					}
					case Sql.QueryExtensionScope.QueryHint:
					{
						attr.ExtendQuery(builder.SqlQueryExtensions ??= new(), list);
						break;
					}
					case Sql.QueryExtensionScope.None:
					{
						break;
					}
				}
			}

			builder.TablesInScope = prevTablesInScope;

			return BuildSequenceResult.FromContext(joinExtensions != null ? new JoinHintContext(sequence, joinExtensions) : sequence);
		}

		public sealed class JoinHintContext : PassThroughContext
		{
			public JoinHintContext(IBuildContext context, List<SqlQueryExtension> extensions)
				: base(context)
			{
				Extensions = extensions;
			}

			public List<SqlQueryExtension> Extensions { get; }

			public override IBuildContext Clone(CloningContext context)
			{
				return new JoinHintContext(context.CloneContext(Context),
					Extensions.Select(e => new SqlQueryExtension()
					{
						Configuration = e.Configuration,
						Arguments     = e.Arguments.ToDictionary(a => a.Key, a => context.CloneElement(a.Value)),
						BuilderType   = e.BuilderType,
						Scope         = e.Scope
					}).ToList());
			}
		}
	}
}
