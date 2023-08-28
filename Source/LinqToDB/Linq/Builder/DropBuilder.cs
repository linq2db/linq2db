using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using SqlQuery;

	sealed class DropBuilder : MethodCallBuilder
	{
		#region DropBuilder

		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return methodCall.IsQueryable("Drop");
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence = (TableBuilder.TableContext)builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

			var ifExists = false;

			if (methodCall.Arguments.Count == 2)
			{
				if (methodCall.Arguments[1].Type == typeof(bool))
				{
					ifExists = !(bool)methodCall.Arguments[1].EvaluateExpression(builder.DataContext)!;
				}
			}

			sequence.SqlTable.Set(ifExists, TableOptions.DropIfExists);
			sequence.Statement = new SqlDropTableStatement(sequence.SqlTable);

			return new DropContext(buildInfo.Parent, sequence);
		}

		#endregion

		#region DropContext

		sealed class DropContext : SequenceContextBase
		{
			public DropContext(IBuildContext? parent, IBuildContext sequence)
				: base(parent, sequence, null)
			{
			}

			public override IBuildContext Clone(CloningContext context)
			{
				return new DropContext(null, context.CloneContext(Sequence));
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
				return Sequence.GetResultStatement();
			}
		}

		#endregion
	}
}
