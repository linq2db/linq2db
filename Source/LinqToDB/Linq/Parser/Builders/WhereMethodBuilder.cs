using System;
using System.Linq.Expressions;
using System.Reflection;
using LinqToDB.Expressions;
using LinqToDB.Linq.Parser.Clauses;

namespace LinqToDB.Linq.Parser.Builders
{
	public class WhereMethodBuilder : MethodCallBuilder
	{
		private static readonly MethodInfo[] _supported =
			{ ParsingMethods.Where };

		public override MethodInfo[] SupportedMethods()
		{
			return _supported;
		}

		public override Sequence BuildSequence(ModelTranslator builder, ParseBuildInfo parseBuildInfo, MethodCallExpression methodCallExpression)
		{
			var sequence = builder.BuildSequence(parseBuildInfo, methodCallExpression.Arguments[0]);

			var sr = builder.GetSourceReference(sequence);

			var lambda = (LambdaExpression)methodCallExpression.Arguments[1].Unwrap();
			var whereExpression = lambda.GetBody(sr);

			parseBuildInfo.Sequence.AddClause(new WhereClause(builder.ConvertExpression(whereExpression)));
			return parseBuildInfo.Sequence;
		}
	}
}
