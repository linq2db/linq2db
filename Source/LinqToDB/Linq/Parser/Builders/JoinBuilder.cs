using System.Linq.Expressions;
using System.Reflection;
using LinqToDB.Expressions;
using LinqToDB.Linq.Parser.Clauses;

namespace LinqToDB.Linq.Parser.Builders
{
	public class JoinBuilder : MethodCallBuilder
	{
		private static readonly MethodInfo[] _supported =
			{ ParsingMethods.JoinMethod };

		public override MethodInfo[] SupportedMethods()
		{
			return _supported;
		}

		public override Sequence BuildSequence(ModelParser builder, ParseBuildInfo parseBuildInfo, MethodCallExpression methodCallExpression)
		{
			var innerSequence = builder.BuildSequence(new ParseBuildInfo(), methodCallExpression.Arguments[0]);
			var outerSequence = builder.BuildSequence(new ParseBuildInfo(), methodCallExpression.Arguments[1]);

			var iqs = builder.GetSourceReference(innerSequence);
			var oqs = builder.GetSourceReference(outerSequence);

			parseBuildInfo.Sequence.AddClause(outerSequence);

			var innerKey    = ((LambdaExpression)methodCallExpression.Arguments[2].Unwrap()).GetBody(iqs);
			var outerKey    = ((LambdaExpression)methodCallExpression.Arguments[3].Unwrap()).GetBody(oqs);

			var selectorLambda = (LambdaExpression)methodCallExpression.Arguments[4].Unwrap();
			var selector = selectorLambda.GetBody(iqs, oqs);

			var joinedType = selector.Type;

			parseBuildInfo.Sequence.AddClause(
				new JoinClause(
					selectorLambda.Parameters[1].Name,
					joinedType,
					outerSequence.GetQuerySource(), innerKey, outerKey));

			parseBuildInfo.Sequence.AddClause(new SelectClause(selector));

			return parseBuildInfo.Sequence;
		}
	}
}
