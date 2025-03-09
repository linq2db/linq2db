using System.Linq.Expressions;

using LinqToDB.Internal.Expressions;

namespace LinqToDB.Internal.Linq.Builder
{
	interface IBuildProxy
	{
		public IBuildContext Owner           { get; }
		public Expression    InnerExpression { get; }
		public Expression    HandleTranslated(Expression? path, SqlPlaceholderExpression placeholder);
	}
}
