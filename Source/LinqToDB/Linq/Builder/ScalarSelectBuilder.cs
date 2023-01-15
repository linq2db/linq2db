using System;
using System.Diagnostics;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using SqlQuery;

	[BuildsExpression(ExpressionType.Lambda)]
	sealed class ScalarSelectBuilder : ISequenceBuilder
	{
		public static bool CanBuild(Expression expr, BuildInfo info, ExpressionBuilder builder)
			=> ((LambdaExpression)expr).Parameters.Count == 0;

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
			=> null;

		public bool IsSequence(ExpressionBuilder builder, BuildInfo buildInfo)
			=> true;

		[DebuggerDisplay("{BuildContextDebuggingHelper.GetContextInfo(this)}")]
		sealed class ScalarSelectContext : IBuildContext
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
				throw new NotImplementedException();
			}

			public Expression BuildExpression(Expression? expression, int level, bool enforceServerSide)
			{
				throw new NotImplementedException();
			}

			public SqlInfo[] ConvertToSql(Expression? expression, int level, ConvertFlags flags)
			{
				throw new NotImplementedException();
			}

			public SqlInfo[] ConvertToIndex(Expression? expression, int level, ConvertFlags flags)
			{
				throw new NotImplementedException();
			}

			public Expression MakeExpression(Expression path, ProjectFlags flags)
			{
				if (SequenceHelper.IsSameContext(path, this))
				{
					var expression = ((LambdaExpression)Expression!).Body.Unwrap();
					return expression;
				}

				return path;
			}

			public IBuildContext Clone(CloningContext context)
			{
				return new ScalarSelectContext(Builder);
			}

			public void SetRunQuery<T>(Query<T> query, Expression expr)
			{
				var mapper = Builder.BuildMapper<T>(SelectQuery, expr);

				QueryRunner.SetRunQuery(query, mapper);
			}

			public IsExpressionResult IsExpression(Expression? expression, int level, RequestFor requestFlag)
			{
				throw new NotImplementedException();
			}

			public IBuildContext? GetContext(Expression? expression, int level, BuildInfo buildInfo)
			{
				return null;
			}

			public int ConvertToParentIndex(int index, IBuildContext context)
			{
				throw new NotImplementedException();
			}

			public void SetAlias(string? alias)
			{
			}

			public ISqlExpression? GetSubQuery(IBuildContext context)
			{
				return null;
			}

			public SqlStatement GetResultStatement()
			{
				return Statement ??= new SqlSelectStatement(SelectQuery);
			}

			public void CompleteColumns()
			{
			}
		}
	}
}
