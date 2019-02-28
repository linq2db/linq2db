using System.Linq.Expressions;
using System.Reflection;
using LinqToDB.Expressions;
using LinqToDB.Linq.Parser.Clauses;

namespace LinqToDB.Linq.Parser.Builders
{
	public class SelectManyBuilder : MethodCallBuilder
	{
		private static readonly MethodInfo[] _supported =
			{ ParsingMethods.SelectManyMethod1, ParsingMethods.SelectManyMethod2 };

		public override MethodInfo[] SupportedMethods()
		{
			return _supported;
		}

		public override Sequence BuildSequence(ModelParser builder, ParseBuildInfo parseBuildInfo, MethodCallExpression methodCallExpression)
		{
			var bi = new ParseBuildInfo();
			var sequence = builder.BuildSequence(bi, methodCallExpression.Arguments[0]);

			var mainReference = builder.GetSourceReference(sequence);
			var lambda = (LambdaExpression)methodCallExpression.Arguments[1].Unwrap();

			var collectionExpression = lambda.GetBody(mainReference);

			var collection = builder.BuildSequence(bi, collectionExpression);
			var collectionReference = builder.GetSourceReference(collection);

			if (methodCallExpression.Arguments.Count > 2)
			{
				var selector = (LambdaExpression)methodCallExpression.Arguments[2].Unwrap();
				var selectorExpression = selector.GetBody(mainReference, collectionReference);
				var selectorClause = new ProjectionClause(selectorExpression.Type, selector.Parameters[0].Name, selectorExpression);

				bi.Sequence.AddClause(selectorClause);
			}

			parseBuildInfo.Sequence.AddClause(new SubQueryClause(bi.Sequence));
			return parseBuildInfo.Sequence;
		}
	}
}
