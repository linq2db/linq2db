using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;

namespace LinqToDB.DataProvider.DB2
{
	using Data;
	using Common;
	using Mapping;
	using SchemaProvider;
	using SqlProvider;

	public class DB2DataProvider : DynamicDataProviderBase<DB2ProviderAdapter>
	{
		public DB2DataProvider(string name, DB2Version version)
						: base(
				  name,
				  MappingSchemaInstance.Get(version, DB2ProviderAdapter.GetInstance().MappingSchema),
				  DB2ProviderAdapter.GetInstance())

		{
			Version = version;

			SqlProviderFlags.AcceptsTakeAsParameter            = false;
			SqlProviderFlags.AcceptsTakeAsParameterIfSkip      = true;
			SqlProviderFlags.IsDistinctOrderBySupported        = false;
			SqlProviderFlags.IsCommonTableExpressionsSupported = true;
			SqlProviderFlags.IsUpdateFromSupported             = false;

			SetCharFieldToType<char>("CHAR", (r, i) => DataTools.GetChar(r, i));
			SetCharField            ("CHAR", (r, i) => r.GetString(i).TrimEnd(' '));

			_sqlOptimizer = new DB2SqlOptimizer(SqlProviderFlags);

			SetProviderField(Adapter.DB2Int64Type       , typeof(long)    , "GetDB2Int64"       , dataReaderType: Adapter.DataReaderType);
			SetProviderField(Adapter.DB2Int32Type       , typeof(int)     , "GetDB2Int32"       , dataReaderType: Adapter.DataReaderType);
			SetProviderField(Adapter.DB2Int16Type       , typeof(short)   , "GetDB2Int16"       , dataReaderType: Adapter.DataReaderType);
			SetProviderField(Adapter.DB2DecimalType     , typeof(decimal) , "GetDB2Decimal"     , dataReaderType: Adapter.DataReaderType);
			SetProviderField(Adapter.DB2DecimalFloatType, typeof(decimal) , "GetDB2DecimalFloat", dataReaderType: Adapter.DataReaderType);
			SetProviderField(Adapter.DB2RealType        , typeof(float)   , "GetDB2Real"        , dataReaderType: Adapter.DataReaderType);
			SetProviderField(Adapter.DB2Real370Type     , typeof(float)   , "GetDB2Real370"     , dataReaderType: Adapter.DataReaderType);
			SetProviderField(Adapter.DB2DoubleType      , typeof(double)  , "GetDB2Double"      , dataReaderType: Adapter.DataReaderType);
			SetProviderField(Adapter.DB2StringType      , typeof(string)  , "GetDB2String"      , dataReaderType: Adapter.DataReaderType);
			SetProviderField(Adapter.DB2ClobType        , typeof(string)  , "GetDB2Clob"        , dataReaderType: Adapter.DataReaderType);
			SetProviderField(Adapter.DB2BinaryType      , typeof(byte[])  , "GetDB2Binary"      , dataReaderType: Adapter.DataReaderType);
			SetProviderField(Adapter.DB2BlobType        , typeof(byte[])  , "GetDB2Blob"        , dataReaderType: Adapter.DataReaderType);
			SetProviderField(Adapter.DB2DateType        , typeof(DateTime), "GetDB2Date"        , dataReaderType: Adapter.DataReaderType);
			SetProviderField(Adapter.DB2TimeType        , typeof(TimeSpan), "GetDB2Time"        , dataReaderType: Adapter.DataReaderType);
			SetProviderField(Adapter.DB2TimeStampType   , typeof(DateTime), "GetDB2TimeStamp"   , dataReaderType: Adapter.DataReaderType);
			SetProviderField(Adapter.DB2XmlType         , typeof(string)  , "GetDB2Xml"         , dataReaderType: Adapter.DataReaderType);
			SetProviderField(Adapter.DB2RowIdType       , typeof(byte[])  , "GetDB2RowId"       , dataReaderType: Adapter.DataReaderType);

			if (Adapter.DB2DateTimeType != null)
				SetProviderField(Adapter.DB2DateTimeType, typeof(DateTime), "GetDB2DateTime"    , dataReaderType: Adapter.DataReaderType);
		}

		public DB2Version Version { get; }

		static class MappingSchemaInstance
		{
			public static readonly MappingSchema DB2LUWMappingSchema = new DB2LUWMappingSchema();
			public static readonly MappingSchema DB2zOSMappingSchema = new DB2zOSMappingSchema();

			public static MappingSchema Get(DB2Version version, MappingSchema providerSchema)
			{
				switch (version)
				{
					default:
					case DB2Version.LUW: return new MappingSchema(DB2LUWMappingSchema, providerSchema);
					case DB2Version.zOS: return new MappingSchema(DB2zOSMappingSchema, providerSchema);
				}
			}
		}

		public override ISchemaProvider GetSchemaProvider()
		{
			return Version == DB2Version.zOS ?
				new DB2zOSSchemaProvider() :
				new DB2LUWSchemaProvider();
		}

		public override ISqlBuilder CreateSqlBuilder(MappingSchema mappingSchema)
		{
			return Version == DB2Version.zOS ?
				new DB2zOSSqlBuilder(this, mappingSchema, GetSqlOptimizer(), SqlProviderFlags) as ISqlBuilder:
				new DB2LUWSqlBuilder(this, mappingSchema, GetSqlOptimizer(), SqlProviderFlags);
		}

		readonly DB2SqlOptimizer _sqlOptimizer;

		public override ISqlOptimizer GetSqlOptimizer()
		{
			return _sqlOptimizer;
		}

		public override void InitCommand(DataConnection dataConnection, CommandType commandType, string commandText, DataParameter[]? parameters, bool withParameters)
		{
			dataConnection.DisposeCommand();
			base.InitCommand(dataConnection, commandType, commandText, parameters, withParameters);
		}

		public override void SetParameter(DataConnection dataConnection, IDbDataParameter parameter, string name, DbDataType dataType, object? value)
		{
			if (value is sbyte sb)
			{
				value    = (short)sb;
				dataType = dataType.WithDataType(DataType.Int16);
			}
			else if (value is byte b)
			{
				value    = (short)b;
				dataType = dataType.WithDataType(DataType.Int16);
			}

			switch (dataType.DataType)
			{
				case DataType.UInt16     : dataType = dataType.WithDataType(DataType.Int32);    break;
				case DataType.UInt32     : dataType = dataType.WithDataType(DataType.Int64);    break;
				case DataType.UInt64     : dataType = dataType.WithDataType(DataType.Decimal);  break;
				case DataType.VarNumeric : dataType = dataType.WithDataType(DataType.Decimal);  break;
				case DataType.DateTime2  : dataType = dataType.WithDataType(DataType.DateTime); break;
				case DataType.Char       :
				case DataType.VarChar    :
				case DataType.NChar      :
				case DataType.NVarChar   :
					{
							 if (value is Guid g) value = g.ToString();
						else if (value is bool b) value = ConvertTo<char>.From(b);
						break;
					}
				case DataType.Boolean    :
				case DataType.Int16      :
					{
						if (value is bool b)
						{
							value    = b ? 1 : 0;
							dataType = dataType.WithDataType(DataType.Int16);
					}
					break;
					}
				case DataType.Guid       :
					{
						if (value is Guid g)
						{
							value    = g.ToByteArray();
							dataType = dataType.WithDataType(DataType.VarBinary);
						}
						if (value == null)
							dataType = dataType.WithDataType(DataType.VarBinary);
						break;
					}
				case DataType.Binary     :
				case DataType.VarBinary  :
					{
						if (value is Guid g) value = g.ToByteArray();

						else if (parameter.Size == 0 && value != null
							&& value.GetType() == Adapter.DB2BinaryType
							&& Adapter.IsDB2BinaryNull(value))
								value = DBNull.Value;
						break;
					}
			}

			// TODO: why we add @ explicitly for DB2, SQLite and Sybase providers???
			base.SetParameter(dataConnection, parameter, "@" + name, dataType, value);
		}

		protected override void SetParameterType(DataConnection dataConnection, IDbDataParameter parameter, DbDataType dataType)
		{
			DB2ProviderAdapter.DB2Type? type = null;
			switch (dataType.DataType)
			{
				case DataType.Blob: type = DB2ProviderAdapter.DB2Type.Blob; break;
			}

			if (type != null)
			{
				var param = TryGetProviderParameter(parameter, dataConnection.MappingSchema);
				if (param != null)
				{
					Adapter.SetDbType(param, type.Value);
					return;
				}
			}

			base.SetParameterType(dataConnection, parameter, dataType);
		}

		#region BulkCopy

		DB2BulkCopy? _bulkCopy;

		public override BulkCopyRowsCopied BulkCopy<T>(ITable<T> table, BulkCopyOptions options, IEnumerable<T> source)
		{
			if (_bulkCopy == null)
				_bulkCopy = new DB2BulkCopy(this);

				return _bulkCopy.BulkCopy(
				options.BulkCopyType == BulkCopyType.Default ? DB2Tools.DefaultBulkCopyType : options.BulkCopyType,
				table,
				options,
				source);
		}

#endregion

	}
}
