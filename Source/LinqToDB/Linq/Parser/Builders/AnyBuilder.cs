using System.Linq.Expressions;
using System.Reflection;
using LinqToDB.Expressions;
using LinqToDB.Linq.Parser.Clauses;

namespace LinqToDB.Linq.Parser.Builders
{
	public class AnyBuilder : MethodCallBuilder
	{
		private static readonly MethodInfo[] _supported =
			{ ParsingMethods.Any, ParsingMethods.AnyPredicate };

		public override MethodInfo[] SupportedMethods()
		{
			return _supported;
		}

		public override Sequence BuildSequence(ModelTranslator builder, ParseBuildInfo parseBuildInfo, MethodCallExpression methodCallExpression)
		{
			var sequence = builder.BuildSequence(new ParseBuildInfo(), methodCallExpression.Arguments[0]);
			parseBuildInfo.Sequence.AddClause(sequence);

			if (methodCallExpression.Arguments.Count > 1)
			{
				var sr = builder.GetSourceReference(sequence);

				var lambda = (LambdaExpression)methodCallExpression.Arguments[1].Unwrap();
				var where = lambda.GetBody(sr);

				sequence.AddClause(new WhereClause(builder.ConvertExpression(sequence, @where)));
			}

			sequence.AddClause(new AnyClause());
			return parseBuildInfo.Sequence;
		}
	}
}
