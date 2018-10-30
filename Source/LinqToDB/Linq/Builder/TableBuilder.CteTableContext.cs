using System;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using Extensions;
	using SqlQuery;

	partial class TableBuilder
	{
		static IBuildContext BuildCteContext(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			var methodCall = (MethodCallExpression)buildInfo.Expression;

			Expression bodyExpr;
			IQueryable query = null;
			string     name  = null;
			bool       isRecursive = false;

			switch (methodCall.Arguments.Count)
			{
				case 1 :
					bodyExpr = methodCall.Arguments[0].Unwrap();
					break;
				case 2 :
					bodyExpr = methodCall.Arguments[0].Unwrap();
					name     = methodCall.Arguments[1].EvaluateExpression() as string;
					break;
				case 3 :
					query    = methodCall.Arguments[0].EvaluateExpression() as IQueryable;
					bodyExpr = methodCall.Arguments[1].Unwrap();
					name     = methodCall.Arguments[2].EvaluateExpression() as string;
					isRecursive = true;
					break;
				default:
					throw new InvalidOperationException();
			}

			builder.RegisterCte(query, bodyExpr, () => new CteClause(null, bodyExpr.Type.GetGenericArgumentsEx()[0], isRecursive, name));

			var cte = builder.BuildCte(bodyExpr,
				cteClause =>
				{
					var info      = new BuildInfo(buildInfo, bodyExpr, new SelectQuery());
					var sequence  = builder.BuildSequence(info);

					if (cteClause == null)
						cteClause = new CteClause(sequence.SelectQuery, bodyExpr.Type.GetGenericArgumentsEx()[0], isRecursive, name);
					else
					{
						cteClause.Body = sequence.SelectQuery;
						cteClause.Name = name;
					}

					return Tuple.Create(cteClause, sequence);
				}
			);

			var cteBuildInfo = new BuildInfo(buildInfo, bodyExpr, buildInfo.SelectQuery);
			var cteContext   = new CteTableContext(builder, cteBuildInfo, cte.Item1, bodyExpr);

			// populate all fields
			if (isRecursive)
				cteContext.ConvertToSql(null, 0, ConvertFlags.All);

			return cteContext;
		}

		static CteTableContext BuildCteContextTable(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			var queryable    = (IQueryable)buildInfo.Expression.EvaluateExpression();
			var cteInfo      = builder.RegisterCte(queryable, null, () => new CteClause(null, queryable.ElementType, false, ""));
			var cteBuildInfo = new BuildInfo(buildInfo, cteInfo.Item3, buildInfo.SelectQuery);
			var cteContext   = new CteTableContext(builder, cteBuildInfo, cteInfo.Item1, cteInfo.Item3);

			return cteContext;
		}

		class CteTableContext : TableContext
		{
			private readonly CteClause     _cte;
			private readonly Expression    _cteExpression;
			private          IBuildContext _cteQueryContext;

			public CteTableContext(ExpressionBuilder builder, BuildInfo buildInfo, CteClause cte, Expression cteExpression)
				: base(builder, buildInfo, new SqlCteTable(builder.MappingSchema, cte))
			{
				_cte             = cte;
				_cteExpression   = cteExpression;
			}

			IBuildContext GetQueryContext()
			{
				return _cteQueryContext ?? (_cteQueryContext = Builder.GetCteContext(_cteExpression));
			}

			public override SqlInfo[] ConvertToSql(Expression expression, int level, ConvertFlags flags)
			{
				var queryContext = GetQueryContext();
				if (queryContext == null)
					return base.ConvertToSql(expression, level, flags);

				var baseInfos = base.ConvertToSql(null, 0, ConvertFlags.All);
				var infos     = queryContext.ConvertToIndex(expression, level, flags);

				var result = infos
					.Select(info =>
					{
						var cteField = _cte.RegisterFieldMapping(info.Sql, info.Index, () =>
						{
							var f = QueryHelper.GetUnderlyingField(baseInfos
								        .FirstOrDefault(bi => bi.CompareMembers(info))?.Sql) ??
							        QueryHelper.GetUnderlyingField(info.Sql);

							var newField = f == null
								? new SqlField { SystemType = info.Sql.SystemType, CanBeNull = info.Sql.CanBeNull }
								: new SqlField(f);

							return newField;
						});

						if (!SqlTable.Fields.TryGetValue(cteField.Name, out var field))
						{
							field = new SqlField(cteField);
							SqlTable.Add(field);
						}

						return new SqlInfo(info.MemberChain)
						{
							Sql = field,
						};
					})
					.ToArray();
				return result;
			}

			public override void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
			{
				var queryContext = GetQueryContext();
				if (queryContext == null)
					base.BuildQuery(query, queryParameter);
				else
				{
					var sqInfos = ConvertToIndex(null, 0, ConvertFlags.All);

					//TODO: igor-tkachev, review usage of Parent.IsExpression here.
					if (Parent != null && Parent.IsExpression(null, 0, RequestFor.Object).Result)
					{
						foreach (var info in sqInfos.OrderBy(i => i.Index))
						{
							Parent?.ConvertToParentIndex(info.Index, this);
						}

						queryContext.BuildQuery(query, queryParameter);
					}
					else
						base.BuildQuery(query, queryParameter);
				}
			}

			public override Expression BuildExpression(Expression expression, int level, bool enforceServerSide)
			{
				var queryContext = GetQueryContext();
				if (queryContext == null)
					return base.BuildExpression(expression, level, enforceServerSide);

				ConvertToIndex(null, 0, ConvertFlags.All);

				//TODO: igor-tkachev, review this.
				var result = Parent == null
					? queryContext.BuildExpression(expression, level, enforceServerSide)
					: base.BuildExpression(expression, level, enforceServerSide);

				return result;
			}

			public override SqlStatement GetResultStatement()
			{
				if (_cte.Fields.Count == 0)
				{
					ConvertToSql(null, 0, ConvertFlags.Key);
				}
				return base.GetResultStatement();
			}
		}
	}
}
