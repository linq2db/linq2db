using System;
using System.Linq.Expressions;
using LinqToDB.Extensions;
using LinqToDB.Linq.Generator;
using LinqToDB.Linq.Parser.Clauses;

namespace LinqToDB.Linq.Parser.Builders
{
	public abstract class BaseSetBuilder : MethodCallBuilder
	{
		public override Sequence BuildSequence(ModelTranslator builder, ParseBuildInfo parseBuildInfo, MethodCallExpression methodCallExpression)
		{
			var seq1Expr = methodCallExpression.Arguments[0];
			var seq2Expr = methodCallExpression.Arguments[1];

			var sequence1 = builder.BuildSequence(new ParseBuildInfo(), seq1Expr);
			var sequence2 = builder.BuildSequence(new ParseBuildInfo(), seq2Expr);

			var setClause = CreateSetClause(seq1Expr.Type.GetGenericArgumentsEx()[0], "", sequence1, sequence2);

			// introduced to replace original set clause source
			var setQuerySource = new SetQuerySource(setClause);

			parseBuildInfo.Sequence.AddClause(setClause);
			parseBuildInfo.Sequence.AddClause(setQuerySource);

			return parseBuildInfo.Sequence;
		}

		protected abstract BaseSetClause CreateSetClause(Type itemType, string itemName, Sequence sequence1,
			Sequence sequence2);
	}
}
