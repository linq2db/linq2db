using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	interface ISequenceBuilder
	{
		int                 BuildCounter { get; set; }
		bool                CanBuild     (ExpressionBuilder builder, BuildInfo buildInfo);
		BuildSequenceResult BuildSequence(ExpressionBuilder builder, BuildInfo buildInfo);
		bool                IsSequence   (ExpressionBuilder builder, BuildInfo buildInfo);
	}
}
