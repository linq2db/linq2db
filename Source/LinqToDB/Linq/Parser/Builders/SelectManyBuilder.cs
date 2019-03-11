using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using LinqToDB.Expressions;
using LinqToDB.Linq.Parser.Clauses;

namespace LinqToDB.Linq.Parser.Builders
{
	public class SelectManyBuilder : MethodCallBuilder
	{
		private static readonly MethodInfo[] _supported =
			{ ParsingMethods.SelectMany, ParsingMethods.SelectManyProjection };

		public override MethodInfo[] SupportedMethods()
		{
			return _supported;
		}

		public override Sequence BuildSequence(ModelTranslator builder, ParseBuildInfo parseBuildInfo, MethodCallExpression methodCallExpression)
		{
			var sourceSequence = builder.BuildSequence(new ParseBuildInfo(), methodCallExpression.Arguments[0]);

			var sourceReference = builder.GetSourceReference(sourceSequence);
			var lambda = (LambdaExpression)methodCallExpression.Arguments[1].Unwrap();

			var collectionExpression = lambda.GetBody(sourceReference);

			var collection = builder.BuildSequence(new ParseBuildInfo(), collectionExpression);
			var lastClause = collection.Clauses.Last();
			if (lastClause is WhereClause where)
			{
				// consider to make join
			}

			var collectionReference = builder.GetSourceReference(collection);

			var joinType = SqlJoinType.Inner;

			parseBuildInfo.Sequence.AddClause(sourceSequence);
			parseBuildInfo.Sequence.AddClause(collection);

			if (methodCallExpression.Arguments.Count > 2)
			{
				var selector = (LambdaExpression)methodCallExpression.Arguments[2].Unwrap();
				var selectorExpression = selector.GetBody(sourceReference, collectionReference);
				var selectorClause = new SelectClause(selectorExpression);
				builder.RegisterSource(selectorClause);

				parseBuildInfo.Sequence.AddClause(selectorClause);
			}

			return parseBuildInfo.Sequence;
		}
	}
}
