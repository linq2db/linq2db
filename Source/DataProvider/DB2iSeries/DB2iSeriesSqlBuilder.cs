using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LinqToDB.DataProvider.DB2iSeries {
  using SqlProvider;
  using SqlQuery;
  using System.Data;

  public class DB2iSeriesSqlBuilder : BasicSqlBuilder {

    public static DB2iSeriesIdentifierQuoteMode IdentifierQuoteMode = DB2iSeriesIdentifierQuoteMode.None;

    public DB2iSeriesSqlBuilder(ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags, ValueToSqlConverter valueToSqlConverter) : base(sqlOptimizer, sqlProviderFlags, valueToSqlConverter) {
    }

    protected override string LimitFormat {
      get {
        return ((SelectQuery.Select.SkipValue == null) ? " FETCH FIRST {0} ROWS ONLY" : null);
      }
    }

    protected override void BuildColumnExpression(ISqlExpression expr, string alias, ref bool addAlias) {
      var wrap = false;
      if (expr.SystemType == typeof(bool)) {
        if (expr is SelectQuery.SearchCondition) {
          wrap = true;
        } else {
          var ex = expr as SqlExpression;
          wrap = ex != null && ex.Expr == "{0}" && ex.Parameters.Length == 1 && ex.Parameters[0] is SelectQuery.SearchCondition;
        }
      }
      if (wrap) {
        StringBuilder.Append("CASE WHEN ");
      }
      base.BuildColumnExpression(expr, alias, ref addAlias);
      if (wrap) {
        StringBuilder.Append(" THEN 1 ELSE 0 END");
      }
    }

    protected override void BuildCommand(int commandNumber) {
      StringBuilder.AppendLine($"SELECT {DB2iSeriesTools.IdentityColumnSql} FROM {DB2iSeriesTools.iSeriesDummyTableName()}");
    }

    protected override void BuildCreateTableIdentityAttribute1(SqlField field) {
      StringBuilder.Append("GENERATED ALWAYS AS IDENTITY");
    }

    protected override void BuildDataType(SqlDataType type, bool createDbType = false) {
      switch (type.DataType) {
        case DataType.DateTime: StringBuilder.Append("timestamp"); break;
        case DataType.DateTime2: StringBuilder.Append("timestamp"); break;
        default: base.BuildDataType(type); break;
      }
    }

    protected override void BuildEmptyInsert() {
      StringBuilder.Append("VALUES");
      foreach (var col in SelectQuery.Insert.Into.Fields) {
        StringBuilder.Append("(DEFAULT)");
      }
      StringBuilder.AppendLine();
    }

    protected override void BuildFunction(SqlFunction func) {
      func = ConvertFunctionParameters(func);
      //      switch (func.Name) {
      //        case "Coalesce":
      //          if (func.Parameters.Length > 2) {
      //            var parms = new ISqlExpression[func.Parameters.Length - 1];
      //            Array.Copy(func.Parameters, 1, parms, 0, parms.Length);
      //            BuildFunction(new SqlFunction(func.SystemType, func.Name, func.Parameters[0],
      //                          new SqlFunction(func.SystemType, func.Name, parms)));
      //            return;
      //          }
      //          var sc = new SelectQuery.SearchCondition();
      //          sc.Conditions.Add(new SelectQuery.Condition(false, new SelectQuery.Predicate.IsNull(func.Parameters[0], false)));
      //          func = new SqlFunction(func.SystemType, "xIif", sc, func.Parameters[1], func.Parameters[0]);
      //          break;
      //      }
      base.BuildFunction(func);
    }

    protected override void BuildFromClause() {
      if (!SelectQuery.IsUpdate) {
        base.BuildFromClause();
      }
    }

    protected override void BuildInsertOrUpdateQuery() {
      BuildInsertOrUpdateQueryAsMerge($"FROM {DB2iSeriesTools.iSeriesDummyTableName()} FETCH FIRST 1 ROW ONLY");
    }

    protected override void BuildSelectClause() {
      if (SelectQuery.From.Tables.Count == 0) {
        AppendIndent().AppendLine("SELECT");
        BuildColumns();
        AppendIndent().AppendLine($"FROM {DB2iSeriesTools.iSeriesDummyTableName()} FETCH FIRST 1 ROW ONLY");
      } else {
        base.BuildSelectClause();
      }
    }

    protected override void BuildSql() {
      AlternativeBuildSql(false, base.BuildSql);
    }

    public override int CommandCount(SelectQuery selectQuery) {
      return ((selectQuery.IsInsert && selectQuery.Insert.WithIdentity) ? 2 : 1);
    }

    public override object Convert(object value, ConvertType _convertType) {
      switch (_convertType) {
        case ConvertType.NameToQueryParameter:
          return "@" + value.ToString();
        case ConvertType.NameToCommandParameter:
        case ConvertType.NameToSprocParameter:
          return ":" + value;
        case ConvertType.SprocParameterToName:
          if (value != null) {
            var str = value.ToString();
            return ((str.Length > 0 && str[0] == ':') ? str.Substring(1) : str);
          }
          break;
        case ConvertType.NameToQueryField:
        case ConvertType.NameToQueryFieldAlias:
        case ConvertType.NameToQueryTable:
        case ConvertType.NameToQueryTableAlias:
          if (value != null && IdentifierQuoteMode != DB2iSeriesIdentifierQuoteMode.None) {
            var name = value.ToString();
            if (name.Length > 0 && name[0] == '"') {
              return name;
            }
            if (IdentifierQuoteMode == DB2iSeriesIdentifierQuoteMode.Quote || name.StartsWith("_") || name.ToCharArray().Any((c) => char.IsWhiteSpace(c))) {
              //If IdentifierQuoteMode = iSeriesIdentifierQuoteMode.Quote OrElse name.StartsWith("_") OrElse name.ToCharArray().Any(Function(c) Char.IsLower(c) OrElse Char.IsWhiteSpace(c)) Then
              return '"' + name + '"';
            }
          }
          break;
      }
      return value;
    }

    protected override ISqlBuilder CreateSqlBuilder() {
      return new DB2iSeriesSqlBuilder(SqlOptimizer, SqlProviderFlags, ValueToSqlConverter);
    }

    protected override string GetProviderTypeName(IDbDataParameter parameter) {
      //     If parameter.DbType = DbType.Decimal AndAlso TypeOf parameter.Value Is Decimal Then
      //     Dim d = New SqlDecimal(parameter.Value)
      //     Return "(" & d.Precision & "," & d.Scale & ")"
      //     End If
      dynamic p = parameter;
      return p.iDB2DbType.ToString();
      //     Return CType(parameter, IBM.Data.DB2.iSeries.iDB2Parameter).iDB2DbType.ToString
    }

  }
}