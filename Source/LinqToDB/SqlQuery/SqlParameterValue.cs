using System.Diagnostics;

namespace LinqToDB.SqlQuery
{
	using Common;

	[DebuggerDisplay("{Value}, {DbDataType}")]
	public class SqlParameterValue
	{
		public SqlParameterValue(object? value, DbDataType dbDataType)
		{
			Value       = value;
			DbDataType = dbDataType;
		}

		public object?    Value      { get; }
		public DbDataType DbDataType { get; }
	}
}
