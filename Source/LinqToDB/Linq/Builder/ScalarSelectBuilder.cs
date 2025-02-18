using System.Diagnostics;
using System.Linq.Expressions;

using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;
using LinqToDB.Reflection;

namespace LinqToDB.Linq.Builder
{
	[BuildsMethodCall("Select")]
	sealed class ScalarSelectBuilder : MethodCallBuilder
	{

		public static bool CanBuildMethod(MethodCallExpression call, BuildInfo info, ExpressionBuilder builder) 
			=> call.IsSameGenericMethod(Methods.LinqToDB.Select);

		public override bool IsSequence(ExpressionBuilder builder, BuildInfo buildInfo) => true;

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return BuildSequenceResult.FromContext(new ScalarSelectContext(builder.GetTranslationModifier(), builder, methodCall.Arguments[1].UnwrapLambda().Body, buildInfo.SelectQuery));
		}

		[DebuggerDisplay("{BuildContextDebuggingHelper.GetContextInfo(this)}")]
		sealed class ScalarSelectContext : BuildContextBase
		{
			public override MappingSchema MappingSchema => Builder.MappingSchema;
			public override Expression    Expression    => Body;

			public Expression Body { get; }

			public ScalarSelectContext(TranslationModifier translationModifier, ExpressionBuilder builder, Expression body, SelectQuery selectQuery) 
				: base(translationModifier, builder, body.Type, selectQuery)
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
				return new ScalarSelectContext(TranslationModifier, Builder, context.CloneExpression(Body), context.CloneElement(SelectQuery));
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
