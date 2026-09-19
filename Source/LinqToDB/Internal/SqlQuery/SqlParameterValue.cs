using System.Diagnostics;

namespace LinqToDB.Internal.SqlQuery
{
	[DebuggerDisplay("{ProviderValue}, {DbDataType}")]
	public sealed class SqlParameterValue
	{
		public SqlParameterValue(object? providerValue, object? clientValue, DbDataType dbDataType)
			: this(providerValue, clientValue, dbDataType, false)
		{
		}

		internal SqlParameterValue(object? providerValue, object? clientValue, DbDataType dbDataType, bool isDbDataTypeExplicit)
		{
			ProviderValue          = providerValue;
			ClientValue            = clientValue;
			DbDataType             = dbDataType;
			IsDbDataTypeExplicit   = isDbDataTypeExplicit;
		}

		public object?    ProviderValue { get; }
		public object?    ClientValue   { get; }
		public DbDataType DbDataType    { get; }
		internal bool     IsDbDataTypeExplicit { get; }
	}
}
