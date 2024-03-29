// ---------------------------------------------------------------------------------------------------
// <auto-generated>
// This code was generated by LinqToDB scaffolding tool (https://github.com/linq2db/linq2db).
// Changes to this file may cause incorrect behavior and will be lost if the code is regenerated.
// </auto-generated>
// ---------------------------------------------------------------------------------------------------

using LinqToDB;
using LinqToDB.Mapping;
using Oracle.ManagedDataAccess.Types;

#pragma warning disable 1573, 1591
#nullable enable

namespace Cli.All.Oracle
{
	[Table("STG_TRADE_INFORMATION")]
	public class StgTradeInformation
	{
		[Column("STG_TRADE_ID"         , DataType = DataType.Decimal , DbType = "NUMBER"        , Length = 22  )] public OracleDecimal  StgTradeId          { get; set; } // NUMBER
		[Column("STG_TRADE_VERSION"    , DataType = DataType.Decimal , DbType = "NUMBER"        , Length = 22  )] public OracleDecimal  StgTradeVersion     { get; set; } // NUMBER
		[Column("INFORMATION_TYPE_ID"  , DataType = DataType.Decimal , DbType = "NUMBER"        , Length = 22  )] public OracleDecimal  InformationTypeId   { get; set; } // NUMBER
		[Column("INFORMATION_TYPE_NAME", DataType = DataType.VarChar , DbType = "VARCHAR2(50)"  , Length = 50  )] public string?        InformationTypeName { get; set; } // VARCHAR2(50)
		[Column("VALUE"                , DataType = DataType.VarChar , DbType = "VARCHAR2(4000)", Length = 4000)] public string?        Value               { get; set; } // VARCHAR2(4000)
		[Column("VALUE_AS_INTEGER"     , DataType = DataType.Decimal , DbType = "NUMBER"        , Length = 22  )] public OracleDecimal? ValueAsInteger      { get; set; } // NUMBER
		[Column("VALUE_AS_DATE"        , DataType = DataType.DateTime, DbType = "DATE"          , Length = 7   )] public OracleDate?    ValueAsDate         { get; set; } // DATE
	}
}
