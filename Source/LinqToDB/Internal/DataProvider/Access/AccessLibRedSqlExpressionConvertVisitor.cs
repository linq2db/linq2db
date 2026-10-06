using LinqToDB.Internal.DataProvider.Translation;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.DataProvider.Access
{
	public class AccessLibRedSqlExpressionConvertVisitor : AccessSqlExpressionConvertVisitor
	{
		public AccessLibRedSqlExpressionConvertVisitor(bool allowModify) : base(allowModify)
		{
		}

		// same bracket escaping EscapeLikePattern applies to a constant pattern, built with REPLACE for a runtime value
		public override ISqlExpression EscapeLikeCharacters(ISqlExpression expression, ref ISqlExpression? escape)
		{
			var type    = QueryHelper.GetDbDataType(expression, MappingSchema);
			var newExpr = Factory.Function(type, "Replace", expression, Factory.Value(type, "["), Factory.Value(type, "[[]"));

			foreach (var s in LikeCharactersToEscape)
				newExpr = Factory.Function(type, "Replace", newExpr, Factory.Value(type, s), Factory.Value(type, "[" + s + "]"));

			return newExpr;
		}

		// LibRed counts milliseconds and has a 64-bit DateDiff_Big, which lifts the two limits the Access overrides exist for
		protected override SqlIntervalUnit? FinestDateUnit             => SqlIntervalUnit.Millisecond;
		public    override SqlIntervalUnit  IntervalResolution         => SqlIntervalUnit.Millisecond;
		public    override bool             CanLowerIntervalDifference  => true;
		public    override bool             CanMeasureDifferenceInTicks => true;
		public    override bool             CanLowerIntervalShift       => true;
		protected override bool             IsTickArithmeticSupported   => true;

		protected override string? DatePartName(SqlIntervalUnit unit)
		{
			return unit == SqlIntervalUnit.Millisecond ? "ms" : base.DatePartName(unit);
		}

		protected override ISqlExpression? CountDateBoundaries(SqlIntervalUnit unit, ISqlExpression start, ISqlExpression end)
		{
			var part = DatePartName(unit);

			return part == null
				? null
				: Factory.Function(Factory.GetDbDataType(typeof(long)), "DateDiff_Big", Factory.Value(part), start, end);
		}

		// Access reads ^ as a power; LibRed has the bitwise operator Jet lacks
		public override IQueryElement ConvertSqlBinaryExpression(SqlBinaryExpression element)
		{
			return element switch
			{
				SqlBinaryExpression(var type, var ex1, "^", var ex2) => new SqlBinaryExpression(type, ex1, "BXOR", ex2, Precedence.Bitwise - 1),
				_ => base.ConvertSqlBinaryExpression(element),
			};
		}
	}
}
