using System;

namespace LinqToDB.DataProvider.Firebird
{
	using Extensions;
	using SqlProvider;
	using SqlQuery;

	public class Firebird3SqlExpressionConvertVisitor : FirebirdSqlExpressionConvertVisitor
	{
		public Firebird3SqlExpressionConvertVisitor(bool allowModify) : base(allowModify)
		{
		}

		protected override bool? GetCaseSensitiveParameter(SqlPredicate.SearchString predicate) => predicate.CaseSensitive.EvaluateBoolExpression(EvaluationContext);

		protected override ISqlExpression ConvertConversion(SqlCastExpression cast)
		{
			var isNull = cast.Expression is SqlValue { Value: null } sqlValue;

			if (!isNull
				&& Type.GetTypeCode(cast.SystemType.ToUnderlying()) == TypeCode.Boolean
				&& ReferenceEquals(cast, IsForPredicate))
			{
				return ConvertToBooleanSearchCondition(cast.Expression);
			}

			return base.ConvertConversion(cast);
		}
	}
}
