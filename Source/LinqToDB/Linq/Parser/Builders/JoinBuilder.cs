using System.Linq.Expressions;
using System.Reflection;
using LinqToDB.Expressions;
using LinqToDB.Linq.Parser.Clauses;

namespace LinqToDB.Linq.Parser.Builders
{
	public class JoinBuilder : MethodCallBuilder
	{
		private static readonly MethodInfo[] _supported =
			{ ParsingMethods.Join };

		public override MethodInfo[] SupportedMethods()
		{
			return _supported;
		}

		public override Sequence BuildSequence(ModelTranslator builder, ParseBuildInfo parseBuildInfo, MethodCallExpression methodCallExpression)
		{
			var outerSequence = builder.BuildSequence(new ParseBuildInfo(), methodCallExpression.Arguments[0]);
			var innerSequence = builder.BuildSequence(new ParseBuildInfo(), methodCallExpression.Arguments[1]);

			var oqs = builder.GetSourceReference(outerSequence);
			var iqs = builder.GetSourceReference(innerSequence);

			parseBuildInfo.Sequence.AddClause(outerSequence);

			var outerKey    = ((LambdaExpression)methodCallExpression.Arguments[2].Unwrap()).GetBody(oqs);
			var innerKey    = ((LambdaExpression)methodCallExpression.Arguments[3].Unwrap()).GetBody(iqs);

			var selectorLambda = (LambdaExpression)methodCallExpression.Arguments[4].Unwrap();
			var selector = selectorLambda.GetBody(oqs, iqs);

			var joinedType = selector.Type;

			parseBuildInfo.Sequence.AddClause(
				new 
					JoinClause(
					selectorLambda.Parameters[1].Name,
					joinedType,
					innerSequence.GetQuerySource(), builder.ConvertExpression(outerKey), builder.ConvertExpression(innerKey)));

			var selectClause = new SelectClause(builder.ConvertExpression(selector));
			parseBuildInfo.Sequence.AddClause(selectClause);
			builder.RegisterSource(selectClause);

			return parseBuildInfo.Sequence;
		}
	}
}
