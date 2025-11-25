using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.SqlQuery;

namespace LinqToDB.Linq.Translation
{
	public interface IAggregationContext : ISqlExpressionTranslator
	{
		public Expression[]?                            FilterExpressions { get; }
		public Expression?                              ValueExpression   { get; }
		public ParameterExpression?                     ValueParameter    { get; }
		public Expression[]?                            Items             { get; }
		public ITranslationContext.OrderByInformation[] OrderBy           { get; }
		public bool                                     IsDistinct        { get; }
		public bool                                     IsGroupBy         { get; }
		public bool                                     IsEmptyGroupBy    { get; }
		public SelectQuery?                             SelectQuery       { get; }

		public bool      TranslateLambdaExpression(LambdaExpression lambdaExpression, [NotNullWhen(true)] out ISqlExpression? sql, [NotNullWhen(false)] out SqlErrorExpression? error);
		LambdaExpression SimplifyEntityLambda(LambdaExpression      lambda,           int                                     parameterIndex);
	}
}
