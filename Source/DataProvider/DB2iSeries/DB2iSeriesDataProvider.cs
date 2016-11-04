using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;

namespace LinqToDB.DataProvider.DB2iSeries {
  using Data;
  using Mapping;
  using SchemaProvider;
  using SqlProvider;

  public class DB2iSeriesDataProvider : DynamicDataProviderBase {
    public DB2iSeriesDataProvider() : base(ProviderName.DB2iSeries, null) {
      SqlProviderFlags.AcceptsTakeAsParameter = false;
      SqlProviderFlags.AcceptsTakeAsParameterIfSkip = true;
      SqlProviderFlags.IsDistinctOrderBySupported = true;
      SetCharField("CHAR", (r, i) => r.GetString(i).TrimEnd());
      _sqlOptimizer = new DB2iSeriesSqlOptimizer(SqlProviderFlags);
    }

    protected override void OnConnectionTypeCreated(Type connectionType) {
      DB2iSeriesTypes.ConnectionType = connectionType;

      dynamic ass = connectionType.Assembly;
      DB2iSeriesTypes.BigInt.Type = ass.GetType(DB2iSeriesTools.AssemblyName + ".iDB2BigInt", true);
      DB2iSeriesTypes.Binary.Type = ass.GetType(DB2iSeriesTools.AssemblyName + ".iDB2Binary", true);
      DB2iSeriesTypes.Blob.Type = ass.GetType(DB2iSeriesTools.AssemblyName + ".iDB2Blob", true);
      DB2iSeriesTypes.Char.Type = ass.GetType(DB2iSeriesTools.AssemblyName + ".iDB2Char", true);
      DB2iSeriesTypes.CharBitData.Type = ass.GetType(DB2iSeriesTools.AssemblyName + ".iDB2CharBitData", true);
      DB2iSeriesTypes.Clob.Type = ass.GetType(DB2iSeriesTools.AssemblyName + ".iDB2Clob", true);
      DB2iSeriesTypes.Date.Type = ass.GetType(DB2iSeriesTools.AssemblyName + ".iDB2Date", true);
      DB2iSeriesTypes.DataLink.Type = ass.GetType(DB2iSeriesTools.AssemblyName + ".iDB2DataLink", true);
      DB2iSeriesTypes.DbClob.Type = ass.GetType(DB2iSeriesTools.AssemblyName + ".iDB2DbClob", true);
      DB2iSeriesTypes.DecFloat16.Type = ass.GetType(DB2iSeriesTools.AssemblyName + ".iDB2DecFloat16", true);
      DB2iSeriesTypes.DecFloat34.Type = ass.GetType(DB2iSeriesTools.AssemblyName + ".iDB2DecFloat34", true);
      DB2iSeriesTypes.Decimal.Type = ass.GetType(DB2iSeriesTools.AssemblyName + ".iDB2Decimal", true);
      DB2iSeriesTypes.Double.Type = ass.GetType(DB2iSeriesTools.AssemblyName + ".iDB2Double", true);
      DB2iSeriesTypes.Graphic.Type = ass.GetType(DB2iSeriesTools.AssemblyName + ".iDB2Graphic", true);
      DB2iSeriesTypes.Integer.Type = ass.GetType(DB2iSeriesTools.AssemblyName + ".iDB2Integer", true);
      DB2iSeriesTypes.Numeric.Type = ass.GetType(DB2iSeriesTools.AssemblyName + ".iDB2Numeric", true);
      DB2iSeriesTypes.Real.Type = ass.GetType(DB2iSeriesTools.AssemblyName + ".iDB2Real", true);
      DB2iSeriesTypes.RowId.Type = ass.GetType(DB2iSeriesTools.AssemblyName + ".iDB2Rowid", true);
      DB2iSeriesTypes.SmallInt.Type = ass.GetType(DB2iSeriesTools.AssemblyName + ".iDB2SmallInt", true);
      DB2iSeriesTypes.Time.Type = ass.GetType(DB2iSeriesTools.AssemblyName + ".iDB2Time", true);
      DB2iSeriesTypes.TimeStamp.Type = ass.GetType(DB2iSeriesTools.AssemblyName + ".iDB2TimeStamp", true);
      DB2iSeriesTypes.VarBinary.Type = ass.GetType(DB2iSeriesTools.AssemblyName + ".iDB2VarBinary", true);
      DB2iSeriesTypes.VarChar.Type = ass.GetType(DB2iSeriesTools.AssemblyName + ".iDB2VarChar", true);
      DB2iSeriesTypes.VarCharBitData.Type = ass.GetType(DB2iSeriesTools.AssemblyName + ".iDB2VarCharBitData", true);
      DB2iSeriesTypes.VarGraphic.Type = ass.GetType(DB2iSeriesTools.AssemblyName + ".iDB2VarGraphic", true);
      DB2iSeriesTypes.Xml.Type = ass.GetType(DB2iSeriesTools.AssemblyName + ".iDB2Xml", true);

      SetProviderField(DB2iSeriesTypes.BigInt, typeof(long), "GetiDB2BigInt");
      SetProviderField(DB2iSeriesTypes.Binary, typeof(byte[]), "GetiDB2Binary");
      SetProviderField(DB2iSeriesTypes.Blob, typeof(byte[]), "GetiDB2Blob");
      SetProviderField(DB2iSeriesTypes.Char, typeof(string), "GetiDB2Char");
      SetProviderField(DB2iSeriesTypes.CharBitData, typeof(byte[]), "GetiDB2CharBitData");
      SetProviderField(DB2iSeriesTypes.Clob, typeof(string), "GetiDB2Clob");
      SetProviderField(DB2iSeriesTypes.DataLink, typeof(string), "GetiDB2DataLink");
      SetProviderField(DB2iSeriesTypes.Date, typeof(System.DateTime), "GetiDB2Date");
      SetProviderField(DB2iSeriesTypes.DbClob, typeof(string), "GetiDB2DbClob");
      SetProviderField(DB2iSeriesTypes.DecFloat16, typeof(decimal), "GetiDB2DecFloat16");
      SetProviderField(DB2iSeriesTypes.DecFloat34, typeof(decimal), "GetiDB2DecFloat34");
      SetProviderField(DB2iSeriesTypes.Decimal, typeof(decimal), "GetiDB2Decimal");
      SetProviderField(DB2iSeriesTypes.Double, typeof(double), "GetiDB2Double");
      SetProviderField(DB2iSeriesTypes.Graphic, typeof(string), "GetiDB2Graphic");
      SetProviderField(DB2iSeriesTypes.Integer, typeof(int), "GetiDB2Integer");
      SetProviderField(DB2iSeriesTypes.Numeric, typeof(decimal), "GetiDB2Numeric");
      SetProviderField(DB2iSeriesTypes.Real, typeof(float), "GetiDB2Real");
      SetProviderField(DB2iSeriesTypes.RowId, typeof(byte[]), "GetiDB2RowId");
      SetProviderField(DB2iSeriesTypes.SmallInt, typeof(short), "GetiDB2SmallInt");
      SetProviderField(DB2iSeriesTypes.Time, typeof(System.DateTime), "GetiDB2Time");
      // GetType(TimeSpan), "GetiDB2Time");
      SetProviderField(DB2iSeriesTypes.TimeStamp, typeof(System.DateTime), "GetiDB2TimeStamp");
      SetProviderField(DB2iSeriesTypes.VarBinary, typeof(byte[]), "GetiDB2VarBinary");
      SetProviderField(DB2iSeriesTypes.VarChar, typeof(string), "GetiDB2VarChar");
      SetProviderField(DB2iSeriesTypes.VarCharBitData, typeof(byte[]), "GetiDB2VarCharBitData");
      SetProviderField(DB2iSeriesTypes.VarGraphic, typeof(string), "GetiDB2VarGraphic");
      SetProviderField(DB2iSeriesTypes.Xml, typeof(string), "GetiDB2Xml");

      MappingSchema.AddScalarType(DB2iSeriesTypes.BigInt, GetNullValue(DB2iSeriesTypes.BigInt), true, DataType.Int64);
      MappingSchema.AddScalarType(DB2iSeriesTypes.Binary, GetNullValue(DB2iSeriesTypes.Binary), true, DataType.Binary);
      MappingSchema.AddScalarType(DB2iSeriesTypes.Blob, GetNullValue(DB2iSeriesTypes.Blob), true, DataType.Blob);
      MappingSchema.AddScalarType(DB2iSeriesTypes.Char, GetNullValue(DB2iSeriesTypes.Char), true, DataType.Char);
      MappingSchema.AddScalarType(DB2iSeriesTypes.CharBitData, GetNullValue(DB2iSeriesTypes.CharBitData), true, DataType.Binary);
      MappingSchema.AddScalarType(DB2iSeriesTypes.Clob, GetNullValue(DB2iSeriesTypes.Clob), true, DataType.NText);
      MappingSchema.AddScalarType(DB2iSeriesTypes.DataLink, GetNullValue(DB2iSeriesTypes.DataLink), true, DataType.NText);
      MappingSchema.AddScalarType(DB2iSeriesTypes.Date, GetNullValue(DB2iSeriesTypes.Date), true, DataType.Date);
      MappingSchema.AddScalarType(DB2iSeriesTypes.DbClob, GetNullValue(DB2iSeriesTypes.DbClob), true, DataType.NText);
      MappingSchema.AddScalarType(DB2iSeriesTypes.DecFloat16, GetNullValue(DB2iSeriesTypes.DecFloat16), true, DataType.Decimal);
      MappingSchema.AddScalarType(DB2iSeriesTypes.DecFloat34, GetNullValue(DB2iSeriesTypes.DecFloat34), true, DataType.Decimal);
      MappingSchema.AddScalarType(DB2iSeriesTypes.Decimal, GetNullValue(DB2iSeriesTypes.Decimal), true, DataType.Decimal);
      MappingSchema.AddScalarType(DB2iSeriesTypes.Double, GetNullValue(DB2iSeriesTypes.Double), true, DataType.Double);
      MappingSchema.AddScalarType(DB2iSeriesTypes.Graphic, GetNullValue(DB2iSeriesTypes.Graphic), true, DataType.NText);
      MappingSchema.AddScalarType(DB2iSeriesTypes.Integer, GetNullValue(DB2iSeriesTypes.Integer), true, DataType.Int32);
      MappingSchema.AddScalarType(DB2iSeriesTypes.Numeric, GetNullValue(DB2iSeriesTypes.Numeric), true, DataType.Decimal);
      MappingSchema.AddScalarType(DB2iSeriesTypes.Real, GetNullValue(DB2iSeriesTypes.Real), true, DataType.Single);
      MappingSchema.AddScalarType(DB2iSeriesTypes.RowId, GetNullValue(DB2iSeriesTypes.RowId), true, DataType.VarBinary);
      MappingSchema.AddScalarType(DB2iSeriesTypes.SmallInt, GetNullValue(DB2iSeriesTypes.SmallInt), true, DataType.Int16);
      MappingSchema.AddScalarType(DB2iSeriesTypes.Time, GetNullValue(DB2iSeriesTypes.Time), true, DataType.Time);
      MappingSchema.AddScalarType(DB2iSeriesTypes.TimeStamp, GetNullValue(DB2iSeriesTypes.TimeStamp), true, DataType.DateTime2);
      MappingSchema.AddScalarType(DB2iSeriesTypes.VarBinary, GetNullValue(DB2iSeriesTypes.VarBinary), true, DataType.VarBinary);
      MappingSchema.AddScalarType(DB2iSeriesTypes.VarChar, GetNullValue(DB2iSeriesTypes.VarChar), true, DataType.VarChar);
      MappingSchema.AddScalarType(DB2iSeriesTypes.VarCharBitData, GetNullValue(DB2iSeriesTypes.VarCharBitData), true, DataType.VarBinary);
      MappingSchema.AddScalarType(DB2iSeriesTypes.VarGraphic, GetNullValue(DB2iSeriesTypes.VarGraphic), true, DataType.NText);
      MappingSchema.AddScalarType(DB2iSeriesTypes.Xml, GetNullValue(DB2iSeriesTypes.Xml), true, DataType.Xml);
      _setBlob = GetSetParameter(connectionType, "iDB2Parameter", "iDB2DbType", "iDB2DbType", "iDB2Blob");
      if (DataConnection.TraceSwitch.TraceInfo) {
        DataConnection.WriteTraceLine(DataReaderType.Assembly.FullName, DataConnection.TraceSwitch.DisplayName);
      }
      DB2iSeriesTools.Initialized();
    }

    private static object GetNullValue(Type type) {
      dynamic getValue = Expression.Lambda<Func<object>>(Expression.Convert(Expression.Field(null, type, "Null"), typeof(object)));
      return getValue.Compile;
    }

    public override string ConnectionNamespace { get; }
    protected override string ConnectionTypeName { get; }
    protected override string DataReaderTypeName { get; }

    private sealed class MappingSchemaInstance {
      public static readonly DB2iSeriesMappingSchema db2iSeriesMappingSchema = new DB2iSeriesMappingSchema();
    }

    public override MappingSchema MappingSchema {
      get {
        return MappingSchemaInstance.db2iSeriesMappingSchema;
      }
    }

    public override ISchemaProvider GetSchemaProvider() {
      return new DB2iSeriesSchemaProvider();
    }

    public override ISqlBuilder CreateSqlBuilder() {
      //      Return New iSeriesSqlBuilder_DB2(GetSqlOptimizer, SqlProviderFlags, MappingSchema.ValueToSqlConverter)
      return new DB2iSeriesSqlBuilder(GetSqlOptimizer(), SqlProviderFlags, MappingSchema.ValueToSqlConverter);
    }

    readonly DB2iSeriesSqlOptimizer _sqlOptimizer;
    public override ISqlOptimizer GetSqlOptimizer() {
      return _sqlOptimizer;
    }

    public override void InitCommand(DataConnection dataConnection, CommandType commandType, string commandText, DataParameter[] parameters) {
      dataConnection.DisposeCommand();
      base.InitCommand(dataConnection, commandType, commandText, parameters);
    }


    static Action<IDbDataParameter> _setBlob;
    public override void SetParameter(IDbDataParameter parameter, string name, DataType dataType__1, object value) {
      if (value is sbyte) {
        value = Convert.ToInt16(Convert.ToSByte(value));
        dataType__1 = DataType.Int16;
      } else if (value is byte) {
        value = Convert.ToInt16(Convert.ToByte(value));
        dataType__1 = DataType.Int16;
      }
      switch (dataType__1) {
        case DataType.UInt16:
          dataType__1 = DataType.Int32;
          break;
        case DataType.UInt32:
          dataType__1 = DataType.Int64;
          break;
        case DataType.UInt64:
          dataType__1 = DataType.Decimal;
          break;
        case DataType.VarNumeric:
          dataType__1 = DataType.Decimal;
          break;
        case DataType.DateTime2:
          dataType__1 = DataType.Date;
          break;
        case DataType.Char:
        case DataType.VarChar:
        case DataType.NChar:
        case DataType.NVarChar:
          if (value is Guid) {
            value = ((Guid)value).ToString();
          } else if (value is bool) {
            value = Common.ConvertTo<char>.From(value);
          }
          break;
        case DataType.Boolean:
        case DataType.Int16:
          if (value is bool) {
            value = (bool)value ? 1 : 0;
            dataType__1 = DataType.Int16;
          }
          break;
        case DataType.Guid:
          if (value is Guid) {
            value = ((Guid)value).ToByteArray();
            dataType__1 = DataType.VarBinary;
          }
          break;
        case DataType.Binary:
        case DataType.VarBinary:
          if (value is Guid) {
            value = ((Guid)value).ToByteArray();
          }
          break;
        case DataType.Blob:
          base.SetParameter(parameter, Convert.ToString("@") + name, dataType__1, value);
          _setBlob(parameter);
          return;
      }
      base.SetParameter(parameter, Convert.ToString("@") + name, dataType__1, value);
    }

    #region "BulkCopy"

    DB2iSeriesBulkCopy _bulkCopy;

    public override BulkCopyRowsCopied BulkCopy<T>(DataConnection dataConnection, BulkCopyOptions options, IEnumerable<T> source) {
      if (_bulkCopy == null)
        _bulkCopy = new DB2iSeriesBulkCopy(GetConnectionType());

      return _bulkCopy.BulkCopy(
        options.BulkCopyType == BulkCopyType.Default ? DB2iSeriesTools.DefaultBulkCopyType : options.BulkCopyType,
        dataConnection,
        options,
        source);
    }

    #endregion

    #region "Merge"

    public override int Merge<T>(DataConnection dataConnection, Expression<Func<T, bool>> deletePredicate, bool delete, IEnumerable<T> source,
       string tableName, string databaseName, string schemaName) {
      if (delete)
        throw new LinqToDBException("DB2 iSeries MERGE statement does not support DELETE by source.");

      return new DB2iSeriesMerge().Merge(dataConnection, deletePredicate, delete, source, tableName, databaseName, schemaName);
    }

    #endregion
  }
}