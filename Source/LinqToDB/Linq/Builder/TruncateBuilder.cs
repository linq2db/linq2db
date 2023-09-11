using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using SqlQuery;

	sealed class TruncateBuilder : MethodCallBuilder
	{
		#region TruncateBuilder

		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return methodCall.IsQueryable("Truncate");
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence = (TableBuilder.TableContext)builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

			var reset = true;
			var arg   = methodCall.Arguments[1].Unwrap();

			if (arg.Type == typeof(bool))
				reset = (bool)arg.EvaluateExpression(builder.DataContext)!;

			sequence.Statement = new SqlTruncateTableStatement { Table = sequence.SqlTable, ResetIdentity = reset };

			return new TruncateContext(sequence);
		}

		#endregion

		#region TruncateContext

		sealed class TruncateContext : PassThroughContext
		{
			public TruncateContext(IBuildContext sequence)
				: base(sequence, sequence.SelectQuery)
			{
			}

			public override void SetRunQuery<T>(Query<T> query, Expression expr)
			{
				QueryRunner.SetNonQueryQuery(query);
			}

			public override IBuildContext Clone(CloningContext context)
			{
				return new TruncateContext(context.CloneContext(Context));
			}
		}

		#endregion
	}
}
