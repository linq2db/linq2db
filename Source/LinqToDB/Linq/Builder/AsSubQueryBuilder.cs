using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using SqlQuery;

	sealed class AsSubQueryBuilder : MethodCallBuilder
	{
		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return methodCall.IsQueryable(nameof(LinqExtensions.AsSubQuery));
		}

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence         = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
			
			sequence.SelectQuery.DoNotRemove = true;
			if (methodCall.Arguments.Count > 1)
				sequence.SelectQuery.QueryName = (string?)builder.EvaluateExpression(methodCall.Arguments[1]);

			sequence = new AsSubqueryContext(sequence);

			return BuildSequenceResult.FromContext(sequence);
		}
	}

	class AsSubqueryContext : SubQueryContext
	{
		public AsSubqueryContext(IBuildContext subQuery, SelectQuery selectQuery, bool addToSql) : base(subQuery, selectQuery, addToSql)
		{
		}

		public AsSubqueryContext(IBuildContext subQuery, bool addToSql = true) : base(subQuery, addToSql)
		{
		}

		public override IBuildContext Clone(CloningContext context)
		{
			var selectQuery = context.CloneElement(SelectQuery);
			return new AsSubqueryContext(context.CloneContext(SubQuery), selectQuery, false);
		}
	}
}
