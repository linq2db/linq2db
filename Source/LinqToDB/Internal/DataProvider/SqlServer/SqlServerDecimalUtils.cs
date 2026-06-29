using System;
using System.Data.SqlTypes;

namespace LinqToDB.Internal.DataProvider.SqlServer
{
	static class SqlServerDecimalUtils
	{
		const int ClrPrecision = 29;

		public static decimal ConvertSqlDecimal(SqlDecimal value)
		{
			if (value.Precision > ClrPrecision)
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
