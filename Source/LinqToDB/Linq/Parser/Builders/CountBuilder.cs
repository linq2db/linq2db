using System.Linq.Expressions;
using System.Reflection;
using LinqToDB.Expressions;
using LinqToDB.Linq.Parser.Clauses;

namespace LinqToDB.Linq.Parser.Builders
{
	public class CountBuilder : MethodCallBuilder
	{
		private static readonly MethodInfo[] _supported =
			{ ParsingMethods.Count, ParsingMethods.CountPredicate };

		public override MethodInfo[] SupportedMethods()
		{
			return _supported;
		}

		public override Sequence BuildSequence(ModelTranslator builder, ParseBuildInfo parseBuildInfo, MethodCallExpression methodCallExpression)
		{
			var sequence = builder.BuildSequence(new ParseBuildInfo(), methodCallExpression.Arguments[0]);
			parseBuildInfo.Sequence.AddClause(sequence);
			Expression filterExpression = null;
			if (methodCallExpression.Arguments.Count > 1)
			{
				var sourceReference = builder.GetSourceReference(sequence);
				filterExpression = ((LambdaExpression)methodCallExpression.Arguments[1].Unwrap()).GetBody(sourceReference);
				filterExpression = builder.ConvertExpression(filterExpression);
			}
			sequence.AddClause(new CountClause(filterExpression, methodCallExpression.Method.ReturnType));
			return parseBuildInfo.Sequence;
		}
	}
}
