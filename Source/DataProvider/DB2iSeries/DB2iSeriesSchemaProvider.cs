using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace LinqToDB.DataProvider.DB2iSeries {
  using Common;
  using Data;
  using SchemaProvider;

  public class DB2iSeriesSchemaProvider : SchemaProviderBase {
    protected override List<ColumnInfo> GetColumns(DataConnection dataConnection) {
      var sql = $@"
                Select 
                  Column_text 
                , Data_Type
                , Is_Identity
                , Is_Nullable
                , Length
                , Numeric_Scale
                , Ordinal_Position
                , System_Column_Name
                , System_Table_Name
                , System_Table_Schema
                From QSYS2/SYSCOLUMNS
                Where system_table_schema = 'OSLMTGF3'
                 ";
      Func<IDataReader, ColumnInfo> drf = (IDataReader dr) => {
        var ci = new ColumnInfo {
          DataType = dr["Data_Type"].ToString(),
          Description = dr["Column_Text"].ToString(),
          IsIdentity = dr["Is_Identity"].ToString() == "YES",
          IsNullable = dr["Is_Nullable"].ToString() == "Y",
          Name = dr["System_Column_Name"].ToString(),
          Ordinal = Converter.ChangeTypeTo<int>(dr["Ordinal_Position"]),
          TableID = Convert.ToString(dr["System_Table_Schema"]).Trim(' ') + "." + Convert.ToString(dr["System_Table_Name"]).Trim(' ')
        };
        SetColumnParameters(ci, Convert.ToInt64(dr["Length"]), Convert.ToInt32(dr["Numeric_Scale"]));
        return ci;
      };
      List<ColumnInfo> _list = dataConnection.Query(drf, sql).ToList();
      return _list;
    }

    protected override DataType GetDataType(string _dataType, string columnType, long? length, int? prec, int? scale) {
      switch (_dataType) {
        //case "BIGINT": return DataType.Int64;
        case "BINARY": return DataType.Binary;
        case "BLOB": return DataType.Blob;
        case "CHAR": return DataType.Char;
        //case "CHARACTER": return DataType.Char;
        //case "CHAR() FOR BIT DATA": return DataType.Binary;
        //case "CLOB": return DataType.Text;    
        case "DATE": return DataType.Date;
        //case "DBCLOB": return DataType.Text;
        //case "DECFLOAT16": return DataType.Decimal;
        //case "DECFLOAT34": return DataType.Decimal;
        case "DECIMAL": return DataType.Decimal;
        case "DOUBLE": return DataType.Double;
        //case "GRAPHIC": return DataType.Text;
        //case "INTEGER": return DataType.Int32;
        //case "LONG VARCHAR": return DataType.VarChar;
        //case "LONG VARCHAR FOR BIT DATA": return DataType.VarBinary;
        //case "LONG VARGRAPHIC": return DataType.Text;
        case "NUMERIC": return DataType.Decimal;
        //case "REAL": return DataType.Single;
        //case "ROWID": return DataType.Undefined;
        //case "SMALLINT": return DataType.Int16;
        case "TIME": return DataType.Time;
        case "TIMESTMP": return DataType.Timestamp;
        case "TIMESTAMP": return DataType.Timestamp;
        //case "VARBIN": return DataType.VarBinary;
        case "VARCHAR": return DataType.VarChar;
        //case "VARCHAR() FOR BIT DATA": return DataType.VarBinary;
        //case "VARGRAPHIC": return DataType.Text;
        //case "XML": return DataType.Xml;
        default:
          throw new NotImplementedException($"data type: {_dataType}");
          return DataType.Undefined;
      }
    }

    protected override List<DataTypeInfo> GetDataTypes(DataConnection dataConnection) {
      throw new NotImplementedException("TODO");
      return base.GetDataTypes(dataConnection);
    }

    protected override string GetDbType(string columnType, DataTypeInfo dataType, long? length, int? prec, int? scale) {
      var dt = (
        from x in DataTypes
        where x.TypeName == columnType
        select x).FirstOrDefault();
      if (dt != null) {
        if (dt.CreateParameters == null) {
          scale = 0;
          prec = scale;
          length = prec;
        } else {
          if (dt.CreateParameters == "LENGTH") {
            scale = 0;
            prec = scale;
          } else {
            length = 0;
          }
          if (dt.CreateFormat == null) {
            if (dt.TypeName.IndexOf("()") >= 0) {
              dt.CreateFormat = dt.TypeName.Replace("()", "({0})");
            } else {
              var format = string.Join(",", dt.CreateParameters.Split(',').Select((p, i) => "{" + i + "}").ToArray());
              dt.CreateFormat = dt.TypeName + "(" + format + ")";
            }
          }
        }
      }
      return base.GetDbType(columnType, dataType, length, prec, scale);
    }

    protected override List<ForeingKeyInfo> GetForeignKeys(DataConnection dataConnection) {
      throw new NotImplementedException("TODO");
      var sql = $@"
          Select 
                 cst.constraint_Name 
               , col.Ordinal_position
               , col.system_Column_Name 
               , cst.system_table_SCHEMA
               , cst.system_table_NAME 
          From QSYS2/SYSCST Cst
          Join QSYS2/SYSKEYCST col On(col.constraint_SCHEMA, col.constraint_NAME)=(cst.constraint_SCHEMA, cst.constraint_NAME)
          Where cst.constraint_type = 'FOREIGN KEY' And cst.constraint_SCHEMA In('xOSLMTGF3', 'OSLSLF3', 'OSLD1F3', 'TRAVISB')
          Order By TableID, PrimaryKeyName, Ordinal
          ";
      //And {GetSchemaFilter("col.TBCREATOR")}
      Func<IDataReader, ForeingKeyInfo> drf = (IDataReader dr) => {
        return new ForeingKeyInfo {
          Name = dr["Name"].ToString(),
          Ordinal = Converter.ChangeTypeTo<int>(dr["Ordinal"]),
          OtherColumn = dr["OtherColumn"].ToString(),
          OtherTableID = dr["OtherTableID"].ToString(),
          ThisColumn = dr["ThisColumn"].ToString(),
          ThisTableID = Convert.ToString(dr["System_Table_Schema"]).Trim(' ') + "." + Convert.ToString(dr["System_Table_Name"]).Trim(' ')
        };
      };
      List<ForeingKeyInfo> _list = dataConnection.Query(drf, sql).ToList();
      return _list;
    }

    protected override List<PrimaryKeyInfo> GetPrimaryKeys(DataConnection dataConnection) {
      var sql = $@"
          Select cst.constraint_Name  
               , cst.system_table_SCHEMA
               , cst.system_table_NAME 
               , col.Ordinal_position 
               , col.system_Column_Name   
          From QSYS2/SYSCST    cst
          Join QSYS2/SYSKEYCST col On(col.constraint_SCHEMA, col.constraint_NAME)=(cst.constraint_SCHEMA, cst.constraint_NAME)
          Where cst.constraint_type= 'PRIMARY KEY' 
            And cst.constraint_SCHEMA In('OSLMTGF3')
          Order By TableID, PrimaryKeyName, Ordinal
          ";
      //And {GetSchemaFilter("col.TBCREATOR")}
      Func<IDataReader, PrimaryKeyInfo> drf = (IDataReader dr) => {
        return new PrimaryKeyInfo {
          ColumnName = Convert.ToString(dr["system_Column_Name"]).Trim(' '),
          Ordinal = Converter.ChangeTypeTo<int>(dr["Ordinal_position"]),
          PrimaryKeyName = Convert.ToString(dr["constraint_Name"]).Trim(' '),
          TableID = Convert.ToString(dr["system_table_SCHEMA"]).Trim(' ') + "." + Convert.ToString(dr["system_table_NAME"]).Trim(' ')
        };
      };
      List<PrimaryKeyInfo> _list = dataConnection.Query(drf, sql).ToList();
      return _list;
    }

    protected override List<ProcedureInfo> GetProcedures(DataConnection dataConnection) {
      throw new NotImplementedException("TODO");
      return base.GetProcedures(dataConnection);
    }

    protected override List<ProcedureParameterInfo> GetProcedureParameters(DataConnection dataConnection) {
      throw new NotImplementedException("TODO");
      return base.GetProcedureParameters(dataConnection);
    }

    protected override string GetProviderSpecificTypeNamespace() {
      return DB2iSeriesTools.AssemblyName;
    }

    protected override List<TableInfo> GetTables(DataConnection dataConnection) {
      var sql = $@"
                 ";
      Func<IDataReader, TableInfo> drf = (IDataReader dr) => {
        return new TableInfo {
          CatalogName = dr["CatalogName"].ToString(),
          Description = dr["Description"].ToString(),
          IsDefaultSchema = dr["IsDefaultSchema"].ToString() == "Y",
          IsView = dr["IsView"].ToString() == "Y",
          SchemaName = dr["SchemaName"].ToString(),
          TableID = dr["TableID"].ToString(),
          TableName = dr["TableName"].ToString()
        };
      };
      List<TableInfo> _list = dataConnection.Query(drf, sql).ToList();
      return _list;
    }

    #region Helpers

    public static void SetColumnParameters(ColumnInfo ci, long? size, int? scale) {
      switch (ci.DataType) {
        case "DECIMAL": //, "NUMERIC" ' "DECFLOAT"
          if (((size ?? 0)) > 0) {
            ci.Precision = (int?)size.Value;
          }
          if (((scale ?? 0)) > 0) {
            ci.Scale = scale;
          }
          break;
        case "CHAR":
        case "VARCHAR":
          //"BLOB", "CLOB", "DBCLOB",
          //"LONG VARGRAPHIC", "VARGRAPHIC", "GRAPHIC",
          //"LONG VARCHAR FOR BIT DATA", "VARCHAR () FOR BIT DATA",
          //"VARBIN", "BINARY",
          //"CHAR () FOR BIT DATA", "LONG VARCHAR", "CHARACTER"
          ci.Length = size;
          break;
        default:
          throw new NotImplementedException($"unknown data type: {ci.DataType}");
      }
    }

    #endregion

  }
}