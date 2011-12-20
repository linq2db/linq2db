using System;
using System.Linq.Expressions;

using LinqToDB.Extensions;
using LinqToDB.SqlBuilder;

namespace LinqToDB.Data.Linq.Builder
{
	class ScalarSelectBuilder : ISequenceBuilder
	{
		public int BuildCounter { get; set; }

		public bool CanBuild(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			return
				buildInfo.Expression.NodeType == ExpressionType.Lambda &&
				((LambdaExpression)buildInfo.Expression).Parameters.Count == 0;
		}

		public IBuildContext BuildSequence(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			return new ScalarSelectContext(builder)
			{
				Parent     = buildInfo.Parent,
				Expression = buildInfo.Expression,
				SqlQuery   = buildInfo.SqlQuery
			};
		}

		public SequenceConvertInfo Convert(ExpressionBuilder builder, BuildInfo buildInfo, ParameterExpression param)
		{
			return null;
		}

		public bool IsSequence(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			return true;
		}

		class ScalarSelectContext : IBuildContext
		{
			public ScalarSelectContext(ExpressionBuilder builder)
			{
				Builder = builder;

				builder.Contexts.Add(this);
			}

#if DEBUG
			public string _sqlQueryText { get { return this.SqlQuery == null ? "" : this.SqlQuery.SqlText; } }
#endif

			public ExpressionBuilder Builder    { get; set; }
			public Expression        Expression { get; set; }
			public SqlQuery          SqlQuery   { get; set; }
			public IBuildContext     Parent     { get; set; }

			public void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
			{
				var expr   = BuildExpression(null, 0);
				var mapper = Builder.BuildMapper<T>(expr);

				query.SetQuery(mapper.Compile());
			}

			public Expression BuildExpression(Expression expression, int level)
			{
				if (expression == null)
					expression = ((LambdaExpression)Expression).Body.Unwrap();

				switch (expression.NodeType)
				{
					case ExpressionType.New:
					case ExpressionType.MemberInit:
						{
							var expr = Builder.BuildExpression(this, expression);

							if (SqlQuery.Select.Columns.Count == 0)
								SqlQuery.Select.Expr(new SqlValue(1));

							return expr;
						}

					default :
						{
							var expr = Builder.ConvertToSql(this, expression);
							var idx  = SqlQuery.Select.Add(expr);

							return Builder.BuildSql(expression.Type, idx);
						}
				}

			}

			public SqlInfo[] ConvertToSql(Expression expression, int level, ConvertFlags flags)
			{
				throw new NotImplementedException();
			}

			public SqlInfo[] ConvertToIndex(Expression expression, int level, ConvertFlags flags)
			{
				throw new NotImplementedException();
			}

			public IsExpressionResult IsExpression(Expression expression, int level, RequestFor requestFlag)
			{
				switch (requestFlag)
				{
					case RequestFor.Expression : return IsExpressionResult.True;
					default                    : return IsExpressionResult.False;
				}
			}

			public IBuildContext GetContext(Expression expression, int level, BuildInfo buildInfo)
			{
				throw new NotImplementedException();
			}

			public int ConvertToParentIndex(int index, IBuildContext context)
			{
				throw new NotImplementedException();
			}

			public void SetAlias(string alias)
			{
			}

			public ISqlExpression GetSubQuery(IBuildContext context)
			{
				return null;
			}
		}
	}
}
