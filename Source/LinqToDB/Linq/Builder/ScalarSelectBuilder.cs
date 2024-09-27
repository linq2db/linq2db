using System.Diagnostics;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using Mapping;
	using SqlQuery;

	[BuildsExpression(ExpressionType.Lambda)]
	sealed class ScalarSelectBuilder : ISequenceBuilder
	{
		public static bool CanBuild(Expression expr, BuildInfo info, ExpressionBuilder builder)
			=> ((LambdaExpression)expr).Parameters.Count == 0;

		public BuildSequenceResult BuildSequence(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			return BuildSequenceResult.FromContext(new ScalarSelectContext(builder, buildInfo.Expression.UnwrapLambda().Body, buildInfo.SelectQuery));
		}

		public bool IsSequence(ExpressionBuilder builder, BuildInfo buildInfo)
			=> true;

		[DebuggerDisplay("{BuildContextDebuggingHelper.GetContextInfo(this)}")]
		sealed class ScalarSelectContext : BuildContextBase
		{
			public override MappingSchema MappingSchema => Builder.MappingSchema;
			public override Expression    Expression    => Body;

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
