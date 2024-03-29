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
	[Table("DecimalOverflow")]
	public class DecimalOverflow
	{
		[Column("Decimal1", DataType = DataType.Decimal, DbType = "NUMBER (38,20)", Length = 22, Precision = 38, Scale = 20)] public OracleDecimal? Decimal1 { get; set; } // NUMBER (38,20)
		[Column("Decimal2", DataType = DataType.Decimal, DbType = "NUMBER (31,2)" , Length = 22, Precision = 31, Scale = 2 )] public OracleDecimal? Decimal2 { get; set; } // NUMBER (31,2)
		[Column("Decimal3", DataType = DataType.Decimal, DbType = "NUMBER (38,36)", Length = 22, Precision = 38, Scale = 36)] public OracleDecimal? Decimal3 { get; set; } // NUMBER (38,36)
		[Column("Decimal4", DataType = DataType.Decimal, DbType = "NUMBER (29,0)" , Length = 22, Precision = 29, Scale = 0 )] public OracleDecimal? Decimal4 { get; set; } // NUMBER (29,0)
		[Column("Decimal5", DataType = DataType.Decimal, DbType = "NUMBER (38,38)", Length = 22, Precision = 38, Scale = 38)] public OracleDecimal? Decimal5 { get; set; } // NUMBER (38,38)
	}
}
