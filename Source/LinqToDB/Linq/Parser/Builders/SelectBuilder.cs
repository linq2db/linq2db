using System;
using System.Linq.Expressions;
using System.Reflection;
using LinqToDB.Expressions;
using LinqToDB.Linq.Parser.Clauses;

namespace LinqToDB.Linq.Parser.Builders
{
	public class SelectBuilder : MethodCallBuilder
	{
		private static readonly MethodInfo[] _supported =
			{ ParsingMethods.Select };

		public override MethodInfo[] SupportedMethods()
		{
			return _supported;
		}

		public override Sequence BuildSequence(ModelTranslator builder, ParseBuildInfo parseBuildInfo, MethodCallExpression methodCallExpression)
		{
			var sequence = builder.BuildSequence(new ParseBuildInfo(), methodCallExpression.Arguments[0]);

			var sr = builder.GetSourceReference(sequence);

			var lambda = (LambdaExpression)methodCallExpression.Arguments[1].Unwrap();
			var projectionExpression = builder.ConvertExpression(sequence, lambda.GetBody(sr));

			var selectClause = new SelectClause(projectionExpression.Type, "", projectionExpression);

			builder.RegisterSource(selectClause);

			parseBuildInfo.Sequence.AddClause(sequence);
			parseBuildInfo.Sequence.AddClause(selectClause);

			return parseBuildInfo.Sequence;
		}
	}
}
