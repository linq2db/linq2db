namespace LinqToDB.Internal.Linq.Builder
{
	interface ISequenceBuilder
	{
		BuildSequenceResult BuildSequence(ExpressionBuilder builder, BuildInfo buildInfo);
		bool                IsSequence   (ExpressionBuilder builder, BuildInfo buildInfo);
	}
}
