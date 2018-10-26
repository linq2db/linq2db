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

				var baseInfos = base.ConvertToSql(expression, level, flags);
				var subInfos  = queryContext.ConvertToIndex(expression, level, flags);

				var pairs = subInfos
					.Where(si => si.MemberChain.Count > 0)
					.Select(si => new
					{
						SubInfo = si,
						BaseInfo = baseInfos.FirstOrDefault(bi =>
							bi.MemberChain.Count > 0 &&
							si.MemberChain[si.MemberChain.Count - 1] == bi.MemberChain[bi.MemberChain.Count - 1])
					})
					.ToArray();

				foreach (var pair in pairs)
				{
					var field = pair.BaseInfo?.Sql as SqlField;
					if (field == null)
					{
						field = pair.SubInfo.Sql as SqlField;
						if (field == null)
						{
							if (pair.SubInfo.Sql is SqlColumn column)
								field = column.Expression as SqlField;
						}
					}

					if (field != null)
						_cte.RegisterFieldMapping(field);

				}

				return baseInfos;
			}
		}
	}
}
