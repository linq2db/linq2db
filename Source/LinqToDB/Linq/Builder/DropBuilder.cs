using System;
using System.Linq.Expressions;

using LinqToDB.Expressions;
using LinqToDB.SqlQuery;

namespace LinqToDB.Linq.Builder
{
	[BuildsMethodCall("Drop")]
	sealed class DropBuilder : MethodCallBuilder
	{
		#region DropBuilder

		public static bool CanBuildMethod(MethodCallExpression call, BuildInfo info, ExpressionBuilder builder)
			=> call.IsQueryable();

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence = (TableBuilder.TableContext)builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

			var ifExists = false;

			if (methodCall.Arguments.Count == 2)
			{
				if (methodCall.Arguments[1].Type == typeof(bool))
				{
					ifExists = !(bool)builder.EvaluateExpression(methodCall.Arguments[1])!;
				}
			}

			sequence.SqlTable.Set(ifExists, TableOptions.DropIfExists);

			return BuildSequenceResult.FromContext(new DropContext(buildInfo.Parent, sequence, new SqlDropTableStatement(sequence.SqlTable)));
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
				throw new NotImplementedException();
			}

			public override SqlStatement GetResultStatement()
			{
				return _dropTableStatement;
			}
		}

		#endregion
	}
}
