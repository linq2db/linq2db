using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	interface ISequenceBuilder
	{
		IBuildContext?       BuildSequence(ExpressionBuilder builder, BuildInfo buildInfo);
		SequenceConvertInfo? Convert      (ExpressionBuilder builder, BuildInfo buildInfo, ParameterExpression? param);
		bool                 IsSequence   (ExpressionBuilder builder, BuildInfo buildInfo);
	}
}
