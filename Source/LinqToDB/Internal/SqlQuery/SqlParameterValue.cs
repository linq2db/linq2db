using System.Diagnostics;

namespace LinqToDB.Internal.SqlQuery
{
	using LinqToDB.Common;

	[DebuggerDisplay("{ProviderValue}, {DbDataType}")]
	public class SqlParameterValue
	{
		public SqlParameterValue(object? providerValue, DbDataType dbDataType)
		{
			ProviderValue = providerValue;
			DbDataType    = dbDataType;
		}

		public object?    ProviderValue { get; }
		public DbDataType DbDataType    { get; }
	}
}
