using System;
using System.Data.SqlTypes;

namespace LinqToDB.Internal.DataProvider.SqlServer
{
	static class SqlServerDecimalUtils
	{
		const int ClrPrecision = 29;

		public static decimal ConvertSqlDecimal(SqlDecimal value)
		{
			// A 29-digit value can exceed decimal.MaxValue (~7.9E28), so precision 29 is not guaranteed to fit and may still be salvageable by scale reduction.
			if (value.Precision >= ClrPrecision)
			{
				for (var scale = Math.Min((int)value.Scale, 28); scale >= 0; scale--)
				{
					try
					{
						return (decimal)SqlDecimal.ConvertToPrecScale(value, ClrPrecision, scale);
					}
					catch (OverflowException)
					{
					}
					catch (SqlTruncateException)
					{
					}
				}
			}

			return value.Value;
		}
	}
}
