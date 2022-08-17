using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using SqlQuery;

	class DropBuilder : MethodCallBuilder
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
					ifExists = !(bool)methodCall.Arguments[1].EvaluateExpression()!;
				}
			}

			sequence.SqlTable.Set(ifExists, TableOptions.DropIfExists);
			sequence.Statement = new SqlDropTableStatement(sequence.SqlTable);

			return new DropContext(buildInfo.Parent, sequence);
		}

		#endregion

		#region DropContext

		class DropContext : SequenceContextBase
		{
			public DropContext(IBuildContext? parent, IBuildContext sequence)
				: base(parent, sequence, null)
			{
			}

			public override void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
			{
				QueryRunner.SetNonQueryQuery(query);
			}

			public override Expression BuildExpression(Expression? expression, int level, bool enforceServerSide)
			{
				return ThrowHelper.ThrowNotImplementedException<Expression>();
			}

			public override SqlInfo[] ConvertToSql(Expression? expression, int level, ConvertFlags flags)
			{
				return ThrowHelper.ThrowNotImplementedException<SqlInfo[]>();
			}

			public override SqlInfo[] ConvertToIndex(Expression? expression, int level, ConvertFlags flags)
			{
				return ThrowHelper.ThrowNotImplementedException<SqlInfo[]>();
			}

			public override IsExpressionResult IsExpression(Expression? expression, int level, RequestFor requestFlag)
			{
				return ThrowHelper.ThrowNotImplementedException<IsExpressionResult>();
			}

			public override IBuildContext GetContext(Expression? expression, int level, BuildInfo buildInfo)
			{
				return ThrowHelper.ThrowNotImplementedException<IBuildContext>();
			}

			public override SqlStatement GetResultStatement()
			{
				return Sequence.GetResultStatement();
			}
		}

		#endregion
	}
}
