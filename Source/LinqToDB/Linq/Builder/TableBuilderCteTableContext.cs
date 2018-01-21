using System;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using SqlQuery;

	partial class TableBuilder
	{
		class CteTableContext : TableContext
		{
			private readonly CteClause     _cte;
			private readonly Expression    _cteExpression;
			private          IBuildContext _cteQueryContext;

			private bool     _calculatingIndex;

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

			public override Expression BuildExpression(Expression expression, int level, bool enforceServerSide)
			{
				var queryContext = GetQueryContext();
				if (queryContext == null)
					return base.BuildExpression(expression, level, enforceServerSide);
				return queryContext.BuildExpression(expression, level, enforceServerSide);
			}

			public override void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
			{
				var queryContext = GetQueryContext();
				if (queryContext == null)
					base.BuildQuery(query, queryParameter);
				else
					queryContext.BuildQuery(query, queryParameter);
			}

			public override SqlInfo[] ConvertToSql(Expression expression, int level, ConvertFlags flags)
			{
				var info = base.ConvertToSql(expression, level, flags);

				if (!_calculatingIndex)
				{
					_calculatingIndex = true;

					var queryContext = GetQueryContext();
					// If Field is needed we have to populate it in CTE
					if (queryContext != null)
					{
						var subInfo = queryContext.ConvertToSql(expression, level, flags);
						if (subInfo.Any(si => si.Index < 0))
							subInfo = queryContext.ConvertToIndex(expression, level, flags);

						for (int i = 0; i < info.Length; i++)
						{
							_cte.RegisterFieldMapping((SqlField)info[i].Sql, subInfo[i].Index);
						}
					}

					_calculatingIndex = false;
				}

				return info;
			}
		}
	}
}
