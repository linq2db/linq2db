// Generated verbatim from the CI log of build 23187 - the query that breaks the connection.
// Do not reformat: this is exactly what linq2db emitted.

internal static class FailingQuery
{
	public const string Sql = @"SELECT
	g_1.Id,
	COUNT(CASE
		WHEN g_1.Boolean THEN 1
		ELSE NULL
	END),
	COUNT(CASE
		WHEN g_1.Boolean = true THEN 1
		ELSE NULL
	END),
	COUNT(CASE
		WHEN g_1.BooleanN = true THEN 1
		ELSE NULL
	END),
	COUNT(CASE
		WHEN g_1.Boolean = false THEN 1
		ELSE NULL
	END),
	COUNT(CASE
		WHEN g_1.BooleanN = false THEN 1
		ELSE NULL
	END),
	COUNT(CASE
		WHEN g_1.Boolean = false THEN 1
		ELSE NULL
	END),
	COUNT(CASE
		WHEN g_1.BooleanN = false OR g_1.BooleanN IS NULL THEN 1
		ELSE NULL
	END),
	COUNT(CASE
		WHEN g_1.Boolean = true THEN 1
		ELSE NULL
	END),
	COUNT(CASE
		WHEN g_1.BooleanN = true OR g_1.BooleanN IS NULL THEN 1
		ELSE NULL
	END),
	COUNT(CASE
		WHEN g_1.Int32 = 0 THEN 1
		ELSE NULL
	END),
	COUNT(CASE
		WHEN g_1.Int32N = 0 THEN 1
		ELSE NULL
	END),
	COUNT(CASE
		WHEN g_1.Decimal = toDecimal128('0', 10) THEN 1
		ELSE NULL
	END),
	COUNT(CASE
		WHEN g_1.DecimalN = toDecimal128('0', 10) THEN 1
		ELSE NULL
	END),
	COUNT(CASE
		WHEN g_1.Double = toFloat64(0) THEN 1
		ELSE NULL
	END),
	COUNT(CASE
		WHEN g_1.DoubleN = toFloat64(0) THEN 1
		ELSE NULL
	END),
	COUNT(CASE
		WHEN g_1.Int32 <> 0 THEN 1
		ELSE NULL
	END),
	COUNT(CASE
		WHEN g_1.Int32N <> 0 OR g_1.Int32N IS NULL THEN 1
		ELSE NULL
	END),
	COUNT(CASE
		WHEN g_1.Decimal <> toDecimal128('0', 10) THEN 1
		ELSE NULL
	END),
	COUNT(CASE
		WHEN g_1.DecimalN <> toDecimal128('0', 10) OR g_1.DecimalN IS NULL
			THEN 1
		ELSE NULL
	END),
	COUNT(CASE
		WHEN g_1.Double <> toFloat64(0) THEN 1
		ELSE NULL
	END),
	COUNT(CASE
		WHEN g_1.DoubleN <> toFloat64(0) OR g_1.DoubleN IS NULL THEN 1
		ELSE NULL
	END),
	COUNT(CASE
		WHEN g_1.Int32 > 0 THEN 1
		ELSE NULL
	END),
	COUNT(CASE
		WHEN g_1.Int32N > 0 THEN 1
		ELSE NULL
	END),
	COUNT(CASE
		WHEN g_1.Decimal > toDecimal128('0', 10) THEN 1
		ELSE NULL
	END),
	COUNT(CASE
		WHEN g_1.DecimalN > toDecimal128('0', 10) THEN 1
		ELSE NULL
	END),
	COUNT(CASE
		WHEN g_1.Double > toFloat64(0) THEN 1
		ELSE NULL
	END),
	COUNT(CASE
		WHEN g_1.DoubleN > toFloat64(0) THEN 1
		ELSE NULL
	END),
	COUNT(CASE
		WHEN g_1.Int32 < 0 THEN 1
		ELSE NULL
	END),
	COUNT(CASE
		WHEN g_1.Int32N < 0 THEN 1
		ELSE NULL
	END),
	COUNT(CASE
		WHEN g_1.Decimal < toDecimal128('0', 10) THEN 1
		ELSE NULL
	END),
	COUNT(CASE
		WHEN g_1.DecimalN < toDecimal128('0', 10) THEN 1
		ELSE NULL
	END),
	COUNT(CASE
		WHEN g_1.Double < toFloat64(0) THEN 1
		ELSE NULL
	END),
	COUNT(CASE
		WHEN g_1.DoubleN < toFloat64(0) THEN 1
		ELSE NULL
	END),
	COUNT(CASE
		WHEN g_1.Int32 >= 0 THEN 1
		ELSE NULL
	END),
	COUNT(CASE
		WHEN g_1.Int32N >= 0 THEN 1
		ELSE NULL
	END),
	COUNT(CASE
		WHEN g_1.Decimal >= toDecimal128('0', 10) THEN 1
		ELSE NULL
	END),
	COUNT(CASE
		WHEN g_1.DecimalN >= toDecimal128('0', 10) THEN 1
		ELSE NULL
	END),
	COUNT(CASE
		WHEN g_1.Double >= toFloat64(0) THEN 1
		ELSE NULL
	END),
	COUNT(CASE
		WHEN g_1.DoubleN >= toFloat64(0) THEN 1
		ELSE NULL
	END),
	COUNT(CASE
		WHEN g_1.Int32 <= 0 THEN 1
		ELSE NULL
	END),
	COUNT(CASE
		WHEN g_1.Int32N <= 0 THEN 1
		ELSE NULL
	END),
	COUNT(CASE
		WHEN g_1.Decimal <= toDecimal128('0', 10) THEN 1
		ELSE NULL
	END),
	COUNT(CASE
		WHEN g_1.DecimalN <= toDecimal128('0', 10) THEN 1
		ELSE NULL
	END),
	COUNT(CASE
		WHEN g_1.Double <= toFloat64(0) THEN 1
		ELSE NULL
	END),
	COUNT(CASE
		WHEN g_1.DoubleN <= toFloat64(0) THEN 1
		ELSE NULL
	END)
FROM
	BooleanTable g_1
GROUP BY
	g_1.Id";
}

