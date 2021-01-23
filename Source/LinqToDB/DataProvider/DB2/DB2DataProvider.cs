using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.DataProvider.DB2
{
	using Common;
	using Data;
	using Mapping;
	using SchemaProvider;
	using SqlProvider;

	public class DB2DataProvider : DynamicDataProviderBase<DB2ProviderAdapter>
	{
		public DB2DataProvider(string name, DB2Version version)
			: base(
				name,
				GetMappingSchema(version, DB2ProviderAdapter.GetInstance().MappingSchema),
				DB2ProviderAdapter.GetInstance())

		{
			Version = version;

			SqlProviderFlags.AcceptsTakeAsParameter            = false;
			SqlProviderFlags.AcceptsTakeAsParameterIfSkip      = true;
			SqlProviderFlags.IsDistinctOrderBySupported        = false;
			SqlProviderFlags.IsCommonTableExpressionsSupported = true;
			SqlProviderFlags.IsUpdateFromSupported             = false;

			SetCharFieldToType<char>("CHAR", DataTools.GetCharExpression);
			SetCharField            ("CHAR", (r, i) => r.GetString(i).TrimEnd(' '));

			_sqlOptimizer = new DB2SqlOptimizer(SqlProviderFlags);

			SetProviderField(Adapter.DB2Int64Type       , typeof(long)    , Adapter.GetDB2Int64ReaderMethod       , dataReaderType: Adapter.DataReaderType);
			SetProviderField(Adapter.DB2Int32Type       , typeof(int)     , Adapter.GetDB2Int32ReaderMethod       , dataReaderType: Adapter.DataReaderType);
			SetProviderField(Adapter.DB2Int16Type       , typeof(short)   , Adapter.GetDB2Int16ReaderMethod       , dataReaderType: Adapter.DataReaderType);
			SetProviderField(Adapter.DB2DecimalType     , typeof(decimal) , Adapter.GetDB2DecimalReaderMethod     , dataReaderType: Adapter.DataReaderType);
			SetProviderField(Adapter.DB2DecimalFloatType, typeof(decimal) , Adapter.GetDB2DecimalFloatReaderMethod, dataReaderType: Adapter.DataReaderType);
			SetProviderField(Adapter.DB2RealType        , typeof(float)   , Adapter.GetDB2RealReaderMethod        , dataReaderType: Adapter.DataReaderType);
			SetProviderField(Adapter.DB2Real370Type     , typeof(float)   , Adapter.GetDB2Real370ReaderMethod     , dataReaderType: Adapter.DataReaderType);
			SetProviderField(Adapter.DB2DoubleType      , typeof(double)  , Adapter.GetDB2DoubleReaderMethod      , dataReaderType: Adapter.DataReaderType);
			SetProviderField(Adapter.DB2StringType      , typeof(string)  , Adapter.GetDB2StringReaderMethod      , dataReaderType: Adapter.DataReaderType);
			SetProviderField(Adapter.DB2ClobType        , typeof(string)  , Adapter.GetDB2ClobReaderMethod        , dataReaderType: Adapter.DataReaderType);
			SetProviderField(Adapter.DB2BinaryType      , typeof(byte[])  , Adapter.GetDB2BinaryReaderMethod      , dataReaderType: Adapter.DataReaderType);
			SetProviderField(Adapter.DB2BlobType        , typeof(byte[])  , Adapter.GetDB2BlobReaderMethod        , dataReaderType: Adapter.DataReaderType);
			SetProviderField(Adapter.DB2DateType        , typeof(DateTime), Adapter.GetDB2DateReaderMethod        , dataReaderType: Adapter.DataReaderType);
			SetProviderField(Adapter.DB2TimeType        , typeof(TimeSpan), Adapter.GetDB2TimeReaderMethod        , dataReaderType: Adapter.DataReaderType);
			SetProviderField(Adapter.DB2TimeStampType   , typeof(DateTime), Adapter.GetDB2TimeStampReaderMethod   , dataReaderType: Adapter.DataReaderType);
			SetProviderField(Adapter.DB2XmlType         , typeof(string)  , Adapter.GetDB2XmlReaderMethod         , dataReaderType: Adapter.DataReaderType);
			SetProviderField(Adapter.DB2RowIdType       , typeof(byte[])  , Adapter.GetDB2RowIdReaderMethod       , dataReaderType: Adapter.DataReaderType);

			if (Adapter.DB2DateTimeType != null)
				SetProviderField(Adapter.DB2DateTimeType, typeof(DateTime), Adapter.GetDB2DateTimeReaderMethod!   , dataReaderType: Adapter.DataReaderType);
		}

		public DB2Version Version { get; }

		private static MappingSchema GetMappingSchema(DB2Version version, MappingSchema providerSchema)
		{
			return version switch
			{
				DB2Version.zOS => new DB2zOSMappingSchema(providerSchema),
				_              => new DB2LUWMappingSchema(providerSchema),
			};
		}

		public override ISchemaProvider GetSchemaProvider()
		{
			return Version == DB2Version.zOS  ?
				new DB2zOSSchemaProvider(this):
				new DB2LUWSchemaProvider(this);
		}

		public override TableOptions SupportedTableOptions =>
			TableOptions.IsTemporary                |
			TableOptions.IsLocalTemporaryStructure  |
			TableOptions.IsGlobalTemporaryStructure |
			TableOptions.IsLocalTemporaryData       |
			TableOptions.CreateIfNotExists          |
			TableOptions.DropIfExists;

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

			base.SetParameter(dataConnection, parameter, name, dataType, value);
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

		public override Task<BulkCopyRowsCopied> BulkCopyAsync<T>(
			ITable<T> table, BulkCopyOptions options, IEnumerable<T> source, CancellationToken cancellationToken)
		{
			if (_bulkCopy == null)
				_bulkCopy = new DB2BulkCopy(this);

			return _bulkCopy.BulkCopyAsync(
				options.BulkCopyType == BulkCopyType.Default ? DB2Tools.DefaultBulkCopyType : options.BulkCopyType,
				table,
				options,
				source,
				cancellationToken);
		}

#if !NETFRAMEWORK
		public override Task<BulkCopyRowsCopied> BulkCopyAsync<T>(
			ITable<T> table, BulkCopyOptions options, IAsyncEnumerable<T> source, CancellationToken cancellationToken)
		{
			if (_bulkCopy == null)
				_bulkCopy = new DB2BulkCopy(this);

			return _bulkCopy.BulkCopyAsync(
				options.BulkCopyType == BulkCopyType.Default ? DB2Tools.DefaultBulkCopyType : options.BulkCopyType,
				table,
				options,
				source,
				cancellationToken);
		}
#endif

		#endregion

	}
}
