using LinqToDB.Internal.SqlQuery;

namespace LinqToDB.Internal.DataProvider.Firebird
{
	public class Firebird3SqlExpressionConvertVisitor : FirebirdSqlExpressionConvertVisitor
	{
		public Firebird3SqlExpressionConvertVisitor(bool allowModify) : base(allowModify)
		{
		}

		protected override bool? GetCaseSensitiveParameter(SqlPredicate.SearchString predicate) => predicate.CaseSensitive.EvaluateBoolExpression(EvaluationContext);

		/// <summary>
		/// From 3 on <c>DATEDIFF</c> carries the tenth of a millisecond rather than truncating to a whole one, so
		/// the measurement is exact against what a <c>TIMESTAMP</c> stores and no member needs declining. The base
		/// class declares the millisecond for 2.5, the one version that truncates.
		/// </summary>
		public override SqlIntervalUnit IntervalResolution => SqlIntervalUnit.Tick;

		public override IQueryElement ConvertCastToPredicate(SqlCastExpression castExpression)
		{
			var isNull = castExpression.Expression is SqlValue { Value: null };

			if (isNull)
				return castExpression;

			return base.ConvertCastToPredicate(castExpression);
		}
	}
}
