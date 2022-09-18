using System.Linq.Expressions;
using LinqToDB.Expressions;

namespace LinqToDB.Linq.Builder
{
	using SqlQuery;

	class InsertOrUpdateBuilder : MethodCallBuilder
	{
		#region InsertOrUpdateBuilder

		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return methodCall.IsQueryable("InsertOrUpdate");
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

			var insertOrUpdateStatement = new SqlInsertOrUpdateStatement(sequence.SelectQuery);
			sequence.Statement = insertOrUpdateStatement;

			UpdateBuilder.BuildSetter(
				builder,
				buildInfo,
				(LambdaExpression)methodCall.Arguments[1].Unwrap(),
				sequence,
				insertOrUpdateStatement.Insert.Items,
				sequence);

			var updateExpr = methodCall.Arguments[2].Unwrap();
			if (!updateExpr.IsNullValue())
				UpdateBuilder.BuildSetter(
					builder,
					buildInfo,
					(LambdaExpression)updateExpr,
					sequence,
					insertOrUpdateStatement.Update.Items,
					sequence);

			insertOrUpdateStatement.Insert.Into  = ((TableBuilder.TableContext)sequence).SqlTable;
			insertOrUpdateStatement.Update.Table = ((TableBuilder.TableContext)sequence).SqlTable;
			insertOrUpdateStatement.SelectQuery.From.Tables.Clear();
			insertOrUpdateStatement.SelectQuery.From.Table(insertOrUpdateStatement.Update.Table);

			if (methodCall.Arguments.Count == 3)
			{
				var table = insertOrUpdateStatement.Insert.Into;
				var keys  = table.GetKeys(false);

				if (keys.Count == 0)
					ThrowHelper.ThrowLinqException($"InsertOrUpdate method requires the '{table.NameForLogging}' table to have a primary key.");

				var q =
				(
					from k in keys
					join i in insertOrUpdateStatement.Insert.Items on k equals i.Column
					select new { k, i }
				).ToList();

				var missedKey = keys.Except(q.Select(i => i.k)).FirstOrDefault();

				if (missedKey != null)
					ThrowHelper.ThrowLinqException(
						$"InsertOrUpdate method requires the '{table.NameForLogging}.{((SqlField)missedKey).Name}' field to be included in the insert setter.");

				insertOrUpdateStatement.Update.Keys.AddRange(q.Select(i => i.i));
			}
			else
			{
				UpdateBuilder.BuildSetter(
					builder,
					buildInfo,
					(LambdaExpression)methodCall.Arguments[3].Unwrap(),
					sequence,
					insertOrUpdateStatement.Update.Keys,
					sequence);
			}

			return new InsertOrUpdateContext(buildInfo.Parent, sequence);
		}

		#endregion

		#region UpdateContext

		class InsertOrUpdateContext : SequenceContextBase
		{
			public InsertOrUpdateContext(IBuildContext? parent, IBuildContext sequence)
				: base(parent, sequence, null)
			{
			}

			public override void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
			{
				if (Builder.DataContext.SqlProviderFlags.IsInsertOrUpdateSupported)
					QueryRunner.SetNonQueryQuery(query);
				else
					QueryRunner.MakeAlternativeInsertOrUpdate(query);
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
		}

		#endregion
	}
}
