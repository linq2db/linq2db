using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LinqToDB.DataProvider.DB2iSeries {
  using Mapping;
  using SqlQuery;

  public class DB2iSeriesMappingSchema : MappingSchema {
    public DB2iSeriesMappingSchema() : this(ProviderName.DB2iSeries) {
    }

    static internal readonly DB2iSeriesMappingSchema Instance = new DB2iSeriesMappingSchema();
    protected DB2iSeriesMappingSchema(string configuration) : base(configuration) {
      SetValueToSqlConverter(typeof(Guid), (sb, dt, v) => ConvertGuidToSql(sb, (Guid)v));
      SetDataType(typeof(string), new SqlDataType(DataType.NVarChar, typeof(string), 255));
      SetValueToSqlConverter(typeof(string), (sb, dt, v) => ConvertStringToSql(sb, v.ToString()));
      SetValueToSqlConverter(typeof(char), (sb, dt, v) => ConvertCharToSql(sb, (char)v));
    }

    private static void AppendConversion(StringBuilder stringBuilder, int value) {
      stringBuilder.Append("varchar(").Append(value).Append(")");
    }

    private static void ConvertStringToSql(StringBuilder stringBuilder, string value) {
      DataTools.ConvertStringToSql(stringBuilder, "||", "'", AppendConversion, value);
    }

    private static void ConvertCharToSql(StringBuilder stringBuilder, char value) {
      DataTools.ConvertCharToSql(stringBuilder, "'", AppendConversion, value);
    }

    private static void ConvertGuidToSql(StringBuilder stringBuilder, Guid value) {
      dynamic s = value.ToString("N");
      stringBuilder
        .Append("Cast(x'")
        .Append(s.Substring(6, 2))
        .Append(s.Substring(4, 2))
        .Append(s.Substring(2, 2))
        .Append(s.Substring(0, 2))
        .Append(s.Substring(10, 2))
        .Append(s.Substring(8, 2))
        .Append(s.Substring(14, 2))
        .Append(s.Substring(12, 2))
        .Append(s.Substring(16, 16))
        .Append("' as char(16) for bit data)");
    }

  }
}
