using System.Collections.Generic;
using System.Diagnostics;
using LinqToDB.Common;

namespace LinqToDB.SqlQuery
{
	[DebuggerDisplay("{Value}")]
	public class SqlParameterValue
	{
		public SqlParameterValue(object? value, DbDataType dbDataType)
		{
			Value       = value;
			DbDataType = dbDataType;
		}

		public object?    Value      { get; }
		public DbDataType DbDataType { get; }

		public static IReadOnlyDictionary<SqlParameter, SqlParameterValue> EmptyDictionary = new Dictionary<SqlParameter, SqlParameterValue>();
	}
}
