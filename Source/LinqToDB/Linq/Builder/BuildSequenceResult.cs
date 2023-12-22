using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	readonly struct BuildSequenceResult
	{
		public BuildSequenceResult(IBuildContext? buildContext)
		{
			BuildContext    = buildContext;
			ErrorExpression = null;
		}

		public BuildSequenceResult(Expression? errorExpression)
		{
			BuildContext    = null;
			ErrorExpression = errorExpression;
		}

		public BuildSequenceResult()
		{
			BuildContext    = null;
			ErrorExpression = null;
		}

		public static BuildSequenceResult NotSupported()                             => new();
		public static BuildSequenceResult Error(Expression          errorExpression) => new(errorExpression);
		public static BuildSequenceResult FromContext(IBuildContext buildContext)    => new(buildContext);

		public                          IBuildContext? BuildContext    { get; }
		public                          Expression?    ErrorExpression { get; }
	}
}
