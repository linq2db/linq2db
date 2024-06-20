using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using SqlProvider;

	[BuildsMethodCall("GetContext")]
	sealed class ContextParser : ISequenceBuilder
	{
		public static bool CanBuildMethod(MethodCallExpression call, BuildInfo info, ExpressionBuilder builder)
			=> true;

		public BuildSequenceResult BuildSequence(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			var call = (MethodCallExpression)buildInfo.Expression;
			return BuildSequenceResult.FromContext(new Context(builder.BuildSequence(new BuildInfo(buildInfo, call.Arguments[0]))));
		}

		public bool IsSequence(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			var call = (MethodCallExpression)buildInfo.Expression;
			return builder.IsSequence(new BuildInfo(buildInfo, call.Arguments[0]));
		}

		public sealed class Context : PassThroughContext
		{
			public Context(IBuildContext context) : base(context)
			{
			}

			public ISqlOptimizer? SqlOptimizer;

			public override void SetRunQuery<T>(Query<T> query, Expression expr)
			{
				query.DoNotCache = true;

				QueryRunner.SetNonQueryQuery(query);

				SqlOptimizer = query.SqlOptimizer;

				query.GetElement = (db, expr, ps, preambles) => this;
				base.SetRunQuery(query, expr);
			}

			public override IBuildContext Clone(CloningContext context)
			{
				return new Context(context.CloneContext(Context));
			}
		}
	}
}
