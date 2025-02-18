using System.Globalization;
using System.Text;

using LinqToDB.Common;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;
using LinqToDB.SqlProvider;

namespace LinqToDB.DataProvider.Firebird
{
	public class Firebird4SqlBuilder : Firebird3SqlBuilder
	{
		public Firebird4SqlBuilder(IDataProvider provider, MappingSchema mappingSchema, DataOptions dataOptions, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(provider, mappingSchema, dataOptions, sqlOptimizer, sqlProviderFlags)
		{
		}

		Firebird4SqlBuilder(BasicSqlBuilder parentBuilder) : base(parentBuilder)
		{
		}

		protected override ISqlBuilder CreateSqlBuilder()
		{
			return new Firebird4SqlBuilder(this);
		}

		protected override bool BuildJoinType(SqlJoinedTable join, SqlSearchCondition condition)
		{
			switch (join.JoinType)
			{
				case JoinType.CrossApply: StringBuilder.Append("CROSS JOIN LATERAL "); return false;
				case JoinType.OuterApply: StringBuilder.Append("LEFT JOIN LATERAL " ); return true;
			}

			return base.BuildJoinType(join, condition);
		}

		protected override void BuildDataTypeFromDataType(DbDataType type, bool forCreateTable, bool canBeNull)
		{
			switch (type.DataType)
			{
				case DataType.Guid  : StringBuilder.Append("BINARY(16)"); break;
				case DataType.Binary when type.Length == null || type.Length < 1:
					StringBuilder.Append("BINARY");
					break;
					
				case DataType.Binary:
					StringBuilder.Append(CultureInfo.InvariantCulture, $"BINARY({type.Length})");
					break;
					
				case DataType.VarBinary when type.Length == null || type.Length > 32_765:
					StringBuilder.Append("BLOB");
					break;
					
				case DataType.VarBinary:
					StringBuilder.Append(CultureInfo.InvariantCulture, $"VARBINARY({type.Length})");
					break;
				default:
					base.BuildDataTypeFromDataType(type, forCreateTable, canBeNull);
					break;
			}
			
		}
	}
}
