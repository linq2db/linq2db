using System;
using System.Diagnostics;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using SqlQuery;

	sealed class ScalarSelectBuilder : ISequenceBuilder
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
			return new ScalarSelectContext(builder, buildInfo.Expression.UnwrapLambda().Body, buildInfo.SelectQuery);
		}

		public SequenceConvertInfo? Convert(ExpressionBuilder builder, BuildInfo buildInfo, ParameterExpression? param)
		{
			return null;
		}

		public bool IsSequence(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			return true;
		}

		public Expression Expand(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			return buildInfo.Expression;
		}

		[DebuggerDisplay("{BuildContextDebuggingHelper.GetContextInfo(this)}")]
		sealed class ScalarSelectContext : BuildContextBase
		{
			public override Expression Expression => Body;

			public Expression Body { get; }

			public ScalarSelectContext(ExpressionBuilder builder, Expression body, SelectQuery selectQuery) : base(builder, body.Type, selectQuery)
			{
				Body = body;
			}

			public override Expression MakeExpression(Expression path, ProjectFlags flags)
			{
				if (SequenceHelper.IsSameContext(path, this))
				{
					var expression = Body.Unwrap();
					return expression;
				}

				return path;
			}

			public override IBuildContext Clone(CloningContext context)
			{
				return new ScalarSelectContext(Builder, context.CloneExpression(Body), context.CloneElement(SelectQuery));
			}

			public override void SetRunQuery<T>(Query<T> query, Expression expr)
			{
				var mapper = Builder.BuildMapper<T>(SelectQuery, expr);

				QueryRunner.SetRunQuery(query, mapper);
			}

			public override SqlStatement GetResultStatement()
			{
				return new SqlSelectStatement(SelectQuery);
			}
		}
	}
}
