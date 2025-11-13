using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.DataProvider.DB2;
using LinqToDB.Internal.DataProvider.DB2.Translation;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Linq.Translation;
using LinqToDB.Mapping;
using LinqToDB.SchemaProvider;

namespace LinqToDB.Internal.DataProvider.DB2
{
#pragma warning disable MA0048 // File name must match type name
	sealed class DB2LUWDataProvider : DB2DataProvider { public DB2LUWDataProvider() : base(ProviderName.DB2LUW, DB2Version.LUW) {} }
	sealed class DB2zOSDataProvider : DB2DataProvider { public DB2zOSDataProvider() : base(ProviderName.DB2zOS, DB2Version.zOS) {} }
#pragma warning restore MA0048 // File name must match type name

	public abstract class DB2DataProvider : DynamicDataProviderBase<DB2ProviderAdapter>
	{
		protected DB2DataProvider(string name, DB2Version version)
			: base(name, GetMappingSchema(version), DB2ProviderAdapter.Instance)
		{
			Version = version;

			SqlProviderFlags.AcceptsTakeAsParameter                                = false;
			SqlProviderFlags.AcceptsTakeAsParameterIfSkip                          = true;
			SqlProviderFlags.IsCommonTableExpressionsSupported                     = true;
			SqlProviderFlags.IsUpdateFromSupported                                 = false;
			SqlProviderFlags.IsCrossJoinSupported                                  = false;
			SqlProviderFlags.SupportedCorrelatedSubqueriesLevel                    = 1;
			SqlProviderFlags.CalculateSupportedCorrelatedLevelWithAggregateQueries = true;
			SqlProviderFlags.IsRecursiveCTEJoinWithConditionSupported              = false;
			SqlProviderFlags.IsDistinctFromSupported                               = true;
			SqlProviderFlags.SupportsPredicatesComparison                          = true;

			// Requires:
			// DB2 LUW: 11.1+
			// DB2 zOS: 12+
			SqlProviderFlags.IsUpdateTakeSupported     = version is DB2Version.LUW;
			SqlProviderFlags.IsUpdateSkipTakeSupported = version is DB2Version.LUW;

			SqlProviderFlags.RowConstructorSupport = RowFeature.Equality | RowFeature.Comparisons | RowFeature.Update |
			                                         RowFeature.UpdateLiteral | RowFeature.Overlaps | RowFeature.Between;

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

		private static MappingSchema GetMappingSchema(DB2Version version)
		{
			return version switch
			{
				DB2Version.zOS => new DB2MappingSchema.DB2zOSMappingSchema(),
				_              => new DB2MappingSchema.DB2LUWMappingSchema(),
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

		protected override IMemberTranslator CreateMemberTranslator()
		{
			return new DB2MemberTranslator();
		}

		public override ISqlBuilder CreateSqlBuilder(MappingSchema mappingSchema, DataOptions dataOptions)
		{
			return Version == DB2Version.zOS ?
				new DB2zOSSqlBuilder(this, mappingSchema, dataOptions, GetSqlOptimizer(dataOptions), SqlProviderFlags) :
				new DB2LUWSqlBuilder(this, mappingSchema, dataOptions, GetSqlOptimizer(dataOptions), SqlProviderFlags);
		}

		readonly DB2SqlOptimizer _sqlOptimizer;

		public override ISqlOptimizer GetSqlOptimizer(DataOptions dataOptions)
		{
			return _sqlOptimizer;
		}

		public override void SetParameter(DataConnection dataConnection, DbParameter parameter, string name, DbDataType dataType, object? value)
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
#if SUPPORTS_DATEONLY
			else if (value is DateOnly d)
			{
				value    = d.ToDateTime(TimeOnly.MinValue);
			}
#endif

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

		protected override void SetParameterType(DataConnection dataConnection, DbParameter parameter, DbDataType dataType)
		{
			DB2ProviderAdapter.DB2Type? type = null;
			switch (dataType.DataType)
			{
				case DataType.Blob: type = DB2ProviderAdapter.DB2Type.Blob; break;
			}

			if (type != null)
			{
				var param = TryGetProviderParameter(dataConnection, parameter);
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

		public override BulkCopyRowsCopied BulkCopy<T>(DataOptions options, ITable<T> table, IEnumerable<T> source)
		{
			_bulkCopy ??= new (this);

			return _bulkCopy.BulkCopy(
				options.BulkCopyOptions.BulkCopyType == BulkCopyType.Default ?
					options.FindOrDefault(DB2Options.Default).BulkCopyType :
					options.BulkCopyOptions.BulkCopyType,
				table,
				options,
				source);
		}

		public override Task<BulkCopyRowsCopied> BulkCopyAsync<T>(DataOptions options, ITable<T> table,
			IEnumerable<T> source, CancellationToken cancellationToken)
		{
			_bulkCopy ??= new (this);

			return _bulkCopy.BulkCopyAsync(
				options.BulkCopyOptions.BulkCopyType == BulkCopyType.Default ?
					options.FindOrDefault(DB2Options.Default).BulkCopyType :
					options.BulkCopyOptions.BulkCopyType,
				table,
				options,
				source,
				cancellationToken);
		}

		public override Task<BulkCopyRowsCopied> BulkCopyAsync<T>(DataOptions options, ITable<T> table,
			IAsyncEnumerable<T> source, CancellationToken cancellationToken)
		{
			_bulkCopy ??= new (this);

			return _bulkCopy.BulkCopyAsync(
				options.BulkCopyOptions.BulkCopyType == BulkCopyType.Default ?
					options.FindOrDefault(DB2Options.Default).BulkCopyType :
					options.BulkCopyOptions.BulkCopyType,
				table,
				options,
				source,
				cancellationToken);
		}

#endregion

	}
}
