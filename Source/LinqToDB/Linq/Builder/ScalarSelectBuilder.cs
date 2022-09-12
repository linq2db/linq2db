using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using SqlQuery;

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
				Parent      = buildInfo.Parent,
				Expression  = buildInfo.Expression,
				SelectQuery = buildInfo.SelectQuery
			};
		}

		public SequenceConvertInfo? Convert(ExpressionBuilder builder, BuildInfo buildInfo, ParameterExpression? param)
		{
			return null;
		}

		public bool IsSequence(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			return true;
		}

		[DebuggerDisplay("{BuildContextDebuggingHelper.GetContextInfo(this)}")]
		class ScalarSelectContext : IBuildContext
		{
			public ScalarSelectContext(ExpressionBuilder builder)
			{
				Builder = builder;

				builder.Contexts.Add(this);
#if DEBUG
				ContextId = builder.GenerateContextId();
#endif
			}

#if DEBUG
			public string SqlQueryText => SelectQuery == null ? "" : SelectQuery.SqlText;
			public string Path          => this.GetPath();
			public int    ContextId     { get; }
#endif

			public ExpressionBuilder Builder     { get; }
			public Expression?       Expression  { get; set; }
			public SelectQuery       SelectQuery { get; set; } = null!;
			public SqlStatement?     Statement   { get; set; }
			public IBuildContext?    Parent      { get; set; }

			public void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
			{
				var expr   = BuildExpression(null, 0, false);
				var mapper = Builder.BuildMapper<T>(expr);

				QueryRunner.SetRunQuery(query, mapper);
			}

			public Expression BuildExpression(Expression? expression, int level, bool enforceServerSide)
			{
				expression ??= ((LambdaExpression)Expression!).Body.Unwrap();

				switch (expression.NodeType)
				{
					case ExpressionType.New:
					case ExpressionType.MemberInit:
						{
							var expr = Builder.BuildSqlExpression(new Dictionary<Expression, Expression>(), this, expression, ProjectFlags.Expression);

							if (SelectQuery.Select.Columns.Count == 0)
								SelectQuery.Select.Expr(new SqlValue(1));

							return expr;
						}

					default :
						{
							var expr = Builder.ConvertToSql(this, expression);
							var idx  = SelectQuery.Select.Add(expr);

							return Builder.BuildSql(expression.Type, idx, expr);
						}
				}

			}

			public SqlInfo[] ConvertToSql(Expression? expression, int level, ConvertFlags flags)
			{
				return ThrowHelper.ThrowNotImplementedException<SqlInfo[]>();
			}

			public SqlInfo[] ConvertToIndex(Expression? expression, int level, ConvertFlags flags)
			{
				return ThrowHelper.ThrowNotImplementedException<SqlInfo[]>();
			}

			public Expression MakeExpression(Expression path, ProjectFlags flags)
			{
				throw new NotImplementedException();
			}

			public IBuildContext Clone(CloningContext context)
			{
				throw new NotImplementedException();
			}

			public void SetRunQuery<T>(Query<T> query, Expression expr)
			{
				var mapper = Builder.BuildMapper<T>(expr);

				QueryRunner.SetRunQuery(query, mapper);
			}

			public IsExpressionResult IsExpression(Expression? expression, int level, RequestFor requestFlag)
			{
				return requestFlag switch
				{
					RequestFor.Expression => IsExpressionResult.True,
					_                     => IsExpressionResult.False,
				};
			}

			public IBuildContext? GetContext(Expression? expression, int level, BuildInfo buildInfo)
			{
				return ThrowHelper.ThrowNotImplementedException<IBuildContext?>();
			}

			public int ConvertToParentIndex(int index, IBuildContext context)
			{
				return Parent?.ConvertToParentIndex(index, context) ?? index;
			}

			public void SetAlias(string? alias)
			{
			}

			public ISqlExpression? GetSubQuery(IBuildContext context)
			{
				return null;
			}

			public virtual SqlStatement GetResultStatement()
			{
				return Statement ??= new SqlSelectStatement(SelectQuery);
			}

			public void CompleteColumns()
			{
			}
		}
	}
}
