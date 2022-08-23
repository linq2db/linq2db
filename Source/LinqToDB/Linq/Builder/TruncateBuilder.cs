using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using SqlQuery;

	class TruncateBuilder : MethodCallBuilder
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
				reset = (bool)arg.EvaluateExpression()!;

			sequence.Statement = new SqlTruncateTableStatement { Table = sequence.SqlTable, ResetIdentity = reset };

			return new TruncateContext(buildInfo.Parent, sequence);
		}

		#endregion

		#region TruncateContext

		class TruncateContext : SequenceContextBase
		{
			public TruncateContext(IBuildContext? parent, IBuildContext sequence)
				: base(parent, sequence, null)
			{
			}

			public override void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
			{
				QueryRunner.SetNonQueryQuery(query);
			}

			public override void SetRunQuery<T>(Query<T> query, Expression expr)
			{
				QueryRunner.SetNonQueryQuery(query);
			}

			public override Expression BuildExpression(Expression? expression, int level, bool enforceServerSide)
			{
				throw new NotImplementedException();
			}

			public override SqlInfo[] ConvertToSql(Expression? expression, int level, ConvertFlags flags)
			{
				throw new NotImplementedException();
			}

			public override SqlInfo[] ConvertToIndex(Expression? expression, int level, ConvertFlags flags)
			{
				throw new NotImplementedException();
			}

			public override IBuildContext Clone(CloningContext context)
			{
				return new TruncateContext(null, context.CloneContext(Sequence));
			}

			public override IsExpressionResult IsExpression(Expression? expression, int level, RequestFor requestFlag)
			{
				throw new NotImplementedException();
			}

			public override IBuildContext GetContext(Expression? expression, int level, BuildInfo buildInfo)
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
