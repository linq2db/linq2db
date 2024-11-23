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

		public BuildSequenceResult(Expression? errorExpression, string? additionalDetails)
		{
			BuildContext      = null;
			ErrorExpression   = errorExpression;
			AdditionalDetails = additionalDetails;
		}

		public BuildSequenceResult()
		{
			BuildContext    = null;
			ErrorExpression = null;
		}

		public static BuildSequenceResult NotSupported()
			=> new();

		public static BuildSequenceResult Error(Expression errorExpression, string? additionalDetails = null) 
			=> new(errorExpression, additionalDetails);

		public static BuildSequenceResult FromContext(IBuildContext buildContext) 
			=> new(buildContext);

		public IBuildContext? BuildContext      { get; }
		public Expression?    ErrorExpression   { get; }
		public string?        AdditionalDetails { get; }

		public bool IsSequence => BuildContext != null || ErrorExpression != null || AdditionalDetails != null;
	}
}
