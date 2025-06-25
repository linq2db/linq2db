using LinqToDB.SqlQuery;

namespace LinqToDB.DataProvider.Firebird
{
	public class Firebird3SqlExpressionConvertVisitor : FirebirdSqlExpressionConvertVisitor
	{
		public Firebird3SqlExpressionConvertVisitor(bool allowModify) : base(allowModify)
		{
		}

		protected override bool? GetCaseSensitiveParameter(SqlPredicate.SearchString predicate) => predicate.CaseSensitive.EvaluateBoolExpression(EvaluationContext);

		public override IQueryElement ConvertCastToPredicate(SqlCastExpression castExpression)
		{
			var isNull = castExpression.Expression is SqlValue { Value: null };

			if (isNull)
				return castExpression;

			return base.ConvertCastToPredicate(castExpression);
		}
	}
}
