using System.Diagnostics;

namespace LinqToDB.SqlQuery
{
	using Common;

	[DebuggerDisplay("{ProviderValue}, {DbDataType}")]
	public class SqlParameterValue
	{
		public SqlParameterValue(object? providerValue, object? clientValue, DbDataType dbDataType)
		{
			ProviderValue = providerValue;
			ClientValue   = clientValue;
			DbDataType    = dbDataType;
		}

		public object?    ProviderValue { get; }
		public object?    ClientValue { get; }
		public DbDataType DbDataType    { get; }
	}
}
