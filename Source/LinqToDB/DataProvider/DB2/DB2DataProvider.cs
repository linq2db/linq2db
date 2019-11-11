#nullable disable
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
	using System.Diagnostics;

	public class DB2DataProvider : DynamicDataProviderBase
	{
		public DB2DataProvider(string name, DB2Version version)
			: base(name, null)
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
		}

		private bool _customReadersConfigured = false;

		public override Expression GetReaderExpression(MappingSchema mappingSchema, IDataReader reader, int idx, Expression readerExpression, Type toType)
		{
			if (!_customReadersConfigured)
			{
				DB2Wrappers.Initialize(MappingSchema);

				SetProviderField(DB2Wrappers.DB2Int64Type       , typeof(long)    , "GetDB2Int64"       , dataReaderType: DB2Wrappers.DataReaderType);
				SetProviderField(DB2Wrappers.DB2Int32Type       , typeof(int)     , "GetDB2Int32"       , dataReaderType: DB2Wrappers.DataReaderType);
				SetProviderField(DB2Wrappers.DB2Int16Type       , typeof(short)   , "GetDB2Int16"       , dataReaderType: DB2Wrappers.DataReaderType);
				SetProviderField(DB2Wrappers.DB2DecimalType     , typeof(decimal) , "GetDB2Decimal"     , dataReaderType: DB2Wrappers.DataReaderType);
				SetProviderField(DB2Wrappers.DB2DecimalFloatType, typeof(decimal) , "GetDB2DecimalFloat", dataReaderType: DB2Wrappers.DataReaderType);
				SetProviderField(DB2Wrappers.DB2RealType        , typeof(float)   , "GetDB2Real"        , dataReaderType: DB2Wrappers.DataReaderType);
				SetProviderField(DB2Wrappers.DB2Real370Type     , typeof(float)   , "GetDB2Real370"     , dataReaderType: DB2Wrappers.DataReaderType);
				SetProviderField(DB2Wrappers.DB2DoubleType      , typeof(double)  , "GetDB2Double"      , dataReaderType: DB2Wrappers.DataReaderType);
				SetProviderField(DB2Wrappers.DB2StringType      , typeof(string)  , "GetDB2String"      , dataReaderType: DB2Wrappers.DataReaderType);
				SetProviderField(DB2Wrappers.DB2ClobType        , typeof(string)  , "GetDB2Clob"        , dataReaderType: DB2Wrappers.DataReaderType);
				SetProviderField(DB2Wrappers.DB2BinaryType      , typeof(byte[])  , "GetDB2Binary"      , dataReaderType: DB2Wrappers.DataReaderType);
				SetProviderField(DB2Wrappers.DB2BlobType        , typeof(byte[])  , "GetDB2Blob"        , dataReaderType: DB2Wrappers.DataReaderType);
				SetProviderField(DB2Wrappers.DB2DateType        , typeof(DateTime), "GetDB2Date"        , dataReaderType: DB2Wrappers.DataReaderType);
				SetProviderField(DB2Wrappers.DB2TimeType        , typeof(TimeSpan), "GetDB2Time"        , dataReaderType: DB2Wrappers.DataReaderType);
				SetProviderField(DB2Wrappers.DB2TimeStampType   , typeof(DateTime), "GetDB2TimeStamp"   , dataReaderType: DB2Wrappers.DataReaderType);
				SetProviderField(DB2Wrappers.DB2XmlType         , typeof(string)  , "GetDB2Xml"         , dataReaderType: DB2Wrappers.DataReaderType);
				SetProviderField(DB2Wrappers.DB2RowIdType       , typeof(byte[])  , "GetDB2RowId"       , dataReaderType: DB2Wrappers.DataReaderType);

				if (DB2Wrappers.DB2DateTimeType != null)
					SetProviderField(DB2Wrappers.DB2DateTimeType, typeof(DateTime), "GetDB2DateTime"    , dataReaderType: DB2Wrappers.DataReaderType);

				_customReadersConfigured = true;
			}

			return base.GetReaderExpression(mappingSchema, reader, idx, readerExpression, toType);
		}

		protected override void OnConnectionTypeCreated(Type connectionType)
		{
		}

#if NET45 || NET46
		public string AssemblyName => "IBM.Data.DB2";
#else
		public string AssemblyName => "IBM.Data.DB2.Core";
#endif

		public    override string ConnectionNamespace => AssemblyName;
		protected override string ConnectionTypeName  => AssemblyName + ".DB2Connection, " + AssemblyName;
		protected override string DataReaderTypeName  => AssemblyName + ".DB2DataReader, " + AssemblyName;

#if !NETSTANDARD2_0 && !NETCOREAPP2_1
		public override string DbFactoryProviderName => "IBM.Data.DB2";
#endif

		public DB2Version Version { get; }

		static class MappingSchemaInstance
		{
			public static readonly DB2LUWMappingSchema DB2LUWMappingSchema = new DB2LUWMappingSchema();
			public static readonly DB2zOSMappingSchema DB2zOSMappingSchema = new DB2zOSMappingSchema();
		}

		public override MappingSchema MappingSchema
		{
			get
			{
				switch (Version)
				{
					case DB2Version.LUW : return MappingSchemaInstance.DB2LUWMappingSchema;
					case DB2Version.zOS : return MappingSchemaInstance.DB2zOSMappingSchema;
				}

				return base.MappingSchema;
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

		public override void InitCommand(DataConnection dataConnection, CommandType commandType, string commandText, DataParameter[] parameters, bool withParameters)
		{
			dataConnection.DisposeCommand();
			base.InitCommand(dataConnection, commandType, commandText, parameters, withParameters);
		}

		public override void SetParameter(DataConnection dataConnection, IDbDataParameter parameter, string name, DbDataType dataType, object value)
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
						else if (parameter.Size == 0 && value != null && value.GetType().Name == "DB2Binary")
						{
							dynamic v = value;
							if (v.IsNull)
								value = DBNull.Value;
						}
						break;
					}
				case DataType.Blob       :
					base.SetParameter(dataConnection, parameter, "@" + name, dataType, value);
					DB2Wrappers.Initialize(MappingSchema);
					DB2Wrappers.TypeSetter(parameter, DB2Wrappers.DB2Type.Blob);
					return;
			}

			// TODO: why we add @ explicitly for DB2, SQLite and Sybase providers???
			base.SetParameter(dataConnection, parameter, "@" + name, dataType, value);
		}

		#region BulkCopy

		DB2BulkCopy _bulkCopy;

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
