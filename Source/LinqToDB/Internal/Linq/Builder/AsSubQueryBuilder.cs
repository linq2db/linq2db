using System.Linq.Expressions;

using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.SqlQuery;

namespace LinqToDB.Internal.Linq.Builder
{
	[BuildsMethodCall(nameof(LinqExtensions.AsSubQuery))]
	sealed class AsSubQueryBuilder : MethodCallBuilder
	{
		public static bool CanBuildMethod(MethodCallExpression call, BuildInfo info, ExpressionBuilder builder)
			=> call.IsQueryable();

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

		public override Expression MakeExpression(Expression path, ProjectFlags flags)
		{
			if (flags.IsRoot() || flags.IsAggregationRoot())
				return path;

			return base.MakeExpression(path, flags);
		}

		public override IBuildContext Clone(CloningContext context)
		{
			var selectQuery = context.CloneElement(SelectQuery);
			return new AsSubqueryContext(context.CloneContext(SubQuery), selectQuery, false);
		}
	}
}
