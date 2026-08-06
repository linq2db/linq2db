using System.Diagnostics;
using System.Linq.Expressions;

using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Reflection;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.Linq.Builder
{
	[BuildsMethodCall("Select")]
	sealed class ScalarSelectBuilder : MethodCallBuilder
	{

		public static bool CanBuildMethod(MethodCallExpression call)
			=> call.IsSameGenericMethod(Methods.LinqToDB.Select);

		public override bool IsSequence(ExpressionBuilder builder, BuildInfo buildInfo) => true;

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var selectQuery = buildInfo.SelectQuery;

			return BuildSequenceResult.FromContext(new ScalarSelectContext(builder.GetTranslationModifier(), builder, methodCall.Arguments[1].UnwrapLambda().Body, selectQuery));
		}

		[DebuggerDisplay("{BuildContextDebuggingHelper.GetContextInfo(this)}")]
		sealed class ScalarSelectContext : BuildContextBase
		{
			public override MappingSchema MappingSchema   => Builder.MappingSchema;
			public override Expression    Expression      => Body;

			// db.Select(() => expr) is a single scalar row (SELECT expr) — it must scalarize to (SELECT expr)
			// when used in a scalar context (e.g. a window ORDER BY key), not be treated as a collection.
			public override bool          IsSingleElement => true;

			public Expression Body { get; }

			public ScalarSelectContext(TranslationModifier translationModifier, ExpressionBuilder builder, Expression body, SelectQuery selectQuery) 
				: base(translationModifier, builder, body.Type, selectQuery)
			{
				Body = body;

				// Keep the wrapping SELECT: once MakeExpression materializes the body as (SELECT body), the optimizer
				// would otherwise inline a no-FROM scalar subquery back to a bare constant — invalid as a window ORDER BY
				// key on SQL Server. Set here (not in the builder) so the flag survives Clone(), which does not copy it.
				SelectQuery.DoNotRemove = true;
			}

			public override Expression MakeExpression(Expression path, ProjectFlags flags)
			{
				if (SequenceHelper.IsSameContext(path, this))
				{
					var expression = Body.Unwrap();

					// When projected for SQL, promote the body to a column of THIS context's SelectQuery and return a
					// placeholder pointing to the whole SelectQuery — a (SELECT body) scalar subquery — instead of
					// inlining the body. Inlining a constant body yields a bare constant, invalid as a window ORDER BY
					// key on SQL Server. Owner is null: the outer UpdateNesting anchors it.
					if (flags.IsSql())
					{
						var bodySql = Builder.BuildSqlExpression(this, expression);
						if (bodySql is SqlPlaceholderExpression)
						{
							Builder.ToColumns(this, bodySql);
							return ExpressionBuilder.CreatePlaceholder((SelectQuery?)null, SelectQuery, path);
						}
					}

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
