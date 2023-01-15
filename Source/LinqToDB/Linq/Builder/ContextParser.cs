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

		public IBuildContext BuildSequence(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			var call = (MethodCallExpression)buildInfo.Expression;
			return new Context(builder.BuildSequence(new BuildInfo(buildInfo, call.Arguments[0])));
		}

		public SequenceConvertInfo? Convert(ExpressionBuilder builder, BuildInfo buildInfo, ParameterExpression? param)
			=> null;

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

			public override void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
			{
				query.DoNotCache = true;

				QueryRunner.SetNonQueryQuery(query);

				SqlOptimizer  = query.SqlOptimizer;

				query.GetElement = (db, expr, ps, preambles) => this;
			}

			public override IBuildContext Clone(CloningContext context)
			{
				return new Context(context.CloneContext(Context));
			}
		}
	}
}
