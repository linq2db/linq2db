using System.Collections.Generic;

using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.DataProvider.Oracle
{
	// Extensions methods/helpers specific to Oracle DataProvider
	internal static class OracleExtensions
	{
		// Check if all expressions have a consistent charset (CHAR vs NCHAR, or VARCHAR vs NVARCHAR).
		public static bool HasInconsistentCharset(this MappingSchema mappingSchema, IEnumerable<ISqlExpression> expressions)
		{
			var hasChar = false;
			var hasNChar = false;

			foreach (var expr in expressions)
			{
				var type = QueryHelper.GetDbDataType(expr, mappingSchema);

				hasChar  |= type.DataType is DataType.Char or DataType.VarChar;
				hasNChar |= type.DataType is DataType.NChar or DataType.NVarChar;

				if (hasChar & hasNChar)
					return true;
			}

			return false;
		}

		// If expr is of type CHAR (resp. VARCHAR), cast it to NCHAR (resp. NVARCHAR).
		// This is used in conjunction with `HasInconsistentCharset` to unify charset of incompatible expressions.
		public static ISqlExpression FixCharset(this MappingSchema mappingSchema, ISqlExpression expr)
		{
			var type = QueryHelper.GetDbDataType(expr, mappingSchema);
			return type.DataType switch
			{
				// Note that TO_NCHAR(x) works on CHAR type too, but it always returns NVARCHAR2, not NCHAR.
				DataType.VarChar => new SqlFunction(type.WithDataType(DataType.NVarChar), "To_NChar", expr),
				DataType.Char => new SqlCastExpression(expr, type.WithDataType(DataType.NChar), null, isMandatory: true),
				_ => expr,
			};
		}

	}
}
