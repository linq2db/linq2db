using System;

using LinqToDB.DataProvider.SqlServer;
using LinqToDB.Internal.DataProvider.Translation;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.SqlQuery;

namespace LinqToDB.Internal.DataProvider.SqlServer
{
	public class SqlServer2005SqlExpressionConvertVisitor : SqlServerSqlExpressionConvertVisitor
	{
		public SqlServer2005SqlExpressionConvertVisitor(bool allowModify, SqlServerVersion sqlServerVersion) : base(allowModify, sqlServerVersion)
		{
		}

		protected virtual bool ProcessConversion(SqlCastExpression cast, out ISqlExpression result)
		{
			// SQL Server 2005 does not support TIME data type
			if (cast.ToType.DataType == DataType.Time)
			{
				result = cast.Expression;
				return true;
			}

			// SQL Server 2005 has no DATE type: truncate the time part as .Date does
			if (cast.ToType.DataType == DataType.Date && cast.Expression.SystemType?.ToUnderlying() is { } type && (type == typeof(DateTime) || type == typeof(DateTimeOffset)))
			{
				var intDataType = Factory.GetDbDataType(typeof(int));
				var datePart    = Factory.Fragment("dd");
				var dateDiff    = Factory.Function(intDataType, "DateDiff", ParametersNullabilityType.SameAsLastParameter, datePart, Factory.Value(intDataType, 0), cast.Expression);

				result = Factory.Function(cast.Type, "DateAdd", ParametersNullabilityType.SameAsSecondParameter, datePart, dateDiff, Factory.Value(intDataType, 0));
				return true;
			}

			result = cast;
			return false;
		}

		protected override ISqlExpression ConvertConversion(SqlCastExpression cast)
		{
			if (ProcessConversion(cast, out var result))
				return result;

			return base.ConvertConversion(cast);
		}
	}
}
