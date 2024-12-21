namespace LinqToDB.DataProvider
{
	internal static class SqlTypes
	{
		internal const string TypesNamespace = "System.Data.SqlTypes";

		// those reader methods defined for both sql server and sql ce providers
		internal const string GetSqlCharsReaderMethod    = "GetSqlChars";
		internal const string GetSqlBinaryReaderMethod   = "GetSqlBinary";
		internal const string GetSqlBooleanReaderMethod  = "GetSqlBoolean";
		internal const string GetSqlByteReaderMethod     = "GetSqlByte";
		internal const string GetSqlDateTimeReaderMethod = "GetSqlDateTime";
		internal const string GetSqlDecimalReaderMethod  = "GetSqlDecimal";
		internal const string GetSqlDoubleReaderMethod   = "GetSqlDouble";
		internal const string GetSqlGuidReaderMethod     = "GetSqlGuid";
		internal const string GetSqlInt16ReaderMethod    = "GetSqlInt16";
		internal const string GetSqlInt32ReaderMethod    = "GetSqlInt32";
		internal const string GetSqlInt64ReaderMethod    = "GetSqlInt64";
		internal const string GetSqlMoneyReaderMethod    = "GetSqlMoney";
		internal const string GetSqlSingleReaderMethod   = "GetSqlSingle";
		internal const string GetSqlStringReaderMethod   = "GetSqlString";
	}
}
