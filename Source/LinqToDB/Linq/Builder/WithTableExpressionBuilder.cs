using System.Linq.Expressions;

using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.SqlQuery;

namespace LinqToDB.Linq.Builder
{
	[BuildsMethodCall(nameof(LinqExtensions.With), nameof(LinqExtensions.WithTableExpression))]
	sealed class WithTableExpressionBuilder : MethodCallBuilder
	{
		public static bool CanBuildMethod(MethodCallExpression call, BuildInfo info, ExpressionBuilder builder)
			=> call.IsQueryable();

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
			var table    = SequenceHelper.GetTableContext(sequence) ?? throw new LinqToDBException($"Cannot get table context from {sequence.GetType()}");
			var value    = builder.EvaluateExpression<string>(methodCall.Arguments[1]);

			table.SqlTable.SqlTableType   = SqlTableType.Expression;
			table.SqlTable.TableArguments = [];

			switch (methodCall.Method.Name)
			{
				case nameof(LinqExtensions.With)                : table.SqlTable.Expression = $"{{0}} {{1}} WITH ({value})"; break;
				case nameof(LinqExtensions.WithTableExpression) : table.SqlTable.Expression = value;                         break;
			}

			return BuildSequenceResult.FromContext(sequence);
		}
	}
}
