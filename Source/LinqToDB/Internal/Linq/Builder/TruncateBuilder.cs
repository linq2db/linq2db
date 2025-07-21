using System.Linq.Expressions;

using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.SqlQuery;

namespace LinqToDB.Internal.Linq.Builder
{
	[BuildsMethodCall("Truncate")]
	sealed class TruncateBuilder : MethodCallBuilder
	{
		#region TruncateBuilder

		public static bool CanBuildMethod(MethodCallExpression call)
			=> call.IsQueryable();

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence = (TableBuilder.TableContext)builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

			var reset = true;
			var arg   = methodCall.Arguments[1].Unwrap();

			if (arg.Type == typeof(bool))
				reset = (bool)builder.EvaluateExpression(arg)!;

			return BuildSequenceResult.FromContext(new TruncateContext(sequence, new SqlTruncateTableStatement { Table = sequence.SqlTable, ResetIdentity = reset }));
		}

		#endregion

		#region TruncateContext

		sealed class TruncateContext : PassThroughContext
		{
			readonly SqlTruncateTableStatement _truncateTableStatement;

			public TruncateContext(IBuildContext sequence, SqlTruncateTableStatement truncateTableStatement)
				: base(sequence, sequence.SelectQuery)
			{
				_truncateTableStatement = truncateTableStatement;
			}

			public override void SetRunQuery<T>(Query<T> query, Expression expr)
			{
				QueryRunner.SetNonQueryQuery(query);
			}

			public override IBuildContext Clone(CloningContext context)
			{
				return new TruncateContext(context.CloneContext(Context), context.CloneElement(_truncateTableStatement));
			}

			public override SqlStatement GetResultStatement()
			{
				return _truncateTableStatement;
			}
		}

		#endregion
	}
}
