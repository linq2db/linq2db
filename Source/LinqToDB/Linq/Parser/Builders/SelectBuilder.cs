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
			{ ParsingMethods.SelectMethod };

		public override MethodInfo[] SupportedMethods()
		{
			return _supported;
		}

		public override Sequence BuildSequence(ModelParser builder, ParseBuildInfo parseBuildInfo, MethodCallExpression methodCallExpression)
		{
			var sequence = builder.BuildSequence(parseBuildInfo, methodCallExpression.Arguments[0]);

			var sr = builder.GetSourceReference(sequence);

			var lambda = (LambdaExpression)methodCallExpression.Arguments[1].Unwrap();
			var projectionExpression = lambda.GetBody(sr);
			
			parseBuildInfo.Sequence.AddClause(new ProjectionClause(projectionExpression.Type.GetGenericArguments()[0], lambda.Parameters[0].Name, projectionExpression));
			return sequence;
		}
	}
}
