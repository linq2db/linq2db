using System.Diagnostics;

namespace LinqToDB.SqlQuery
{
	using Common;

	[DebuggerDisplay("{OriginalValue} => DB: {ProviderValue}, {DbDataType}")]
	public class SqlParameterValue
	{
		public SqlParameterValue(object? providerValue, object? originalValue, DbDataType dbDataType)
		{
			ProviderValue = providerValue;
			OriginalValue = originalValue;
			DbDataType    = dbDataType;
		}

		public object?    ProviderValue { get; }
		public object?    OriginalValue { get; }
		public DbDataType DbDataType    { get; }
	}
}
