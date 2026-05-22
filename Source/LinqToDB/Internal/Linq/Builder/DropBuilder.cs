using System;
using System.Linq.Expressions;

using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.SqlQuery;

namespace LinqToDB.Internal.Linq.Builder
{
	[BuildsMethodCall("Drop")]
	sealed class DropBuilder : MethodCallBuilder
	{
		#region DropBuilder

		public static bool CanBuildMethod(MethodCallExpression call)
			=> call.IsQueryable;

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			builder.PushDisabledQueryFilters([]);
			var sequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
			builder.PopDisabledFilter();

			var table = SequenceHelper.GetTableContext(sequence);

			if (table == null)
				return BuildSequenceResult.Error(methodCall, "Could not find table context for Drop operation.");

			var ifExists = false;

			if (methodCall.Arguments.Count == 2)
			{
				if (methodCall.Arguments[1].Type == typeof(bool))
				{
					ifExists = !(bool)builder.EvaluateExpression(methodCall.Arguments[1])!;
				}
			}

			table.SqlTable.Set(ifExists, TableOptions.DropIfExists);

			return BuildSequenceResult.FromContext(new DropContext(buildInfo.Parent, sequence, new SqlDropTableStatement(table.SqlTable)));
		}

		#endregion

		#region DropContext

		sealed class DropContext : SequenceContextBase
		{
			readonly SqlDropTableStatement _dropTableStatement;

			public DropContext(IBuildContext? parent, IBuildContext sequence,
				SqlDropTableStatement         dropTableStatement)
				: base(parent, sequence, null)
			{
				_dropTableStatement = dropTableStatement;
			}

			public override IBuildContext Clone(CloningContext context)
			{
				return new DropContext(null, context.CloneContext(Sequence), context.CloneElement(_dropTableStatement));
			}

			public override void SetRunQuery<T>(Query<T> query, Expression expr)
			{
				QueryRunner.SetNonQueryQuery(query);
			}

			public override IBuildContext? GetContext(Expression expression, BuildInfo buildInfo)
			{
				throw new NotSupportedException();
			}

			public override SqlStatement GetResultStatement()
			{
				return _dropTableStatement;
			}
		}

		#endregion
	}
}
