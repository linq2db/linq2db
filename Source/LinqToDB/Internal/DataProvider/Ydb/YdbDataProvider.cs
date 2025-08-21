using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Data;
using LinqToDB.DataProvider.Ydb;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.DataProvider.Ydb.Translation;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Linq.Translation;
using LinqToDB.Mapping;
using LinqToDB.SchemaProvider;

namespace LinqToDB.Internal.DataProvider.Ydb
{
	public class YdbDataProvider : DynamicDataProviderBase<YdbProviderAdapter>
	{
		public YdbDataProvider()
			: this(ProviderName.Ydb, YdbMappingSchema.Instance)
		{
		}

		protected YdbDataProvider(string name, MappingSchema mappingSchema)
			: base(name, mappingSchema, YdbProviderAdapter.Instance)
		{
			SqlProviderFlags.IsSubQueryOrderBySupported        = true;
			SqlProviderFlags.IsDistinctSetOperationsSupported  = false;
			// only Serializable supported
			SqlProviderFlags.DefaultMultiQueryIsolationLevel   = IsolationLevel.Serializable;
			SqlProviderFlags.RowConstructorSupport             = RowFeature.Equality | RowFeature.Comparisons | RowFeature.Between | RowFeature.In | RowFeature.UpdateLiteral;
			SqlProviderFlags.SupportsPredicatesComparison      = true;
			SqlProviderFlags.IsDistinctFromSupported           = true;

			// "emulated" using table expressions
			SqlProviderFlags.IsCommonTableExpressionsSupported = true;

			// https://github.com/ydb-platform/ydb/issues/11258
			// note that we cannot use LIMIT big_num, X workaround, as we do for CH, because server misbehaves
			SqlProviderFlags.IsSkipSupported                  = false;
			SqlProviderFlags.IsSkipSupportedIfTake            = true;

			SqlProviderFlags.IsComplexJoinConditionSupported  = false;
			SqlProviderFlags.IsNestedJoinsSupported           = false;

			SqlProviderFlags.IsSupportedSimpleCorrelatedSubqueries = true;
			SqlProviderFlags.SupportedCorrelatedSubqueriesLevel    = 0;

			SetProviderField<byte[]>  (YdbProviderAdapter.GetBytes,        Adapter.DataReaderType);
			SetProviderField<sbyte>   (YdbProviderAdapter.GetSByte,        Adapter.DataReaderType);
			SetProviderField<ushort>  (YdbProviderAdapter.GetUInt16,       Adapter.DataReaderType);
			SetProviderField<uint>    (YdbProviderAdapter.GetUInt32,       Adapter.DataReaderType);
			SetProviderField<ulong>   (YdbProviderAdapter.GetUInt64,       Adapter.DataReaderType);
			SetProviderField<TimeSpan>(YdbProviderAdapter.GetInterval,     Adapter.DataReaderType);
			SetProviderField<string>  (YdbProviderAdapter.GetJson,         Adapter.DataReaderType, dataTypeName: "Json");
			SetProviderField<string>  (YdbProviderAdapter.GetJsonDocument, Adapter.DataReaderType, dataTypeName: "JsonDocument");

			SetGetFieldValueReader<TextReader>(dataReaderType: Adapter.DataReaderType);
			SetGetFieldValueReader<Stream>(dataReaderType: Adapter.DataReaderType);

			// datetime/datetimeoffset handling is a mess in provider
			SetProviderField<DbDataReader, DateTimeOffset, DateTime>((r, i) => new DateTimeOffset(r.GetDateTime(i), default), dataTypeName: "Timestamp");
			// another bug in provider. For Yson it returns byte[] but reports String as a value type
			SetProviderField<DbDataReader, byte[], string>((r, i) => (byte[])r.GetValue(i), dataTypeName: "Yson");
			SetProviderField<DbDataReader, string, string>((r, i) => Encoding.UTF8.GetString((byte[])r.GetValue(i)), dataTypeName: "Yson");

			SetProviderField<DbDataReader, string, byte[]>((r, i) => Encoding.UTF8.GetString((byte[])r.GetValue(i)), dataTypeName: "String");
			SetProviderField<DbDataReader, string, byte[]>((r, i) => Encoding.UTF8.GetString((byte[])r.GetValue(i)), dataTypeName: "String", dataReaderType: Adapter.DataReaderType);

			SetProviderField<DbDataReader, char, byte[]>((r, i) => Encoding.UTF8.GetString((byte[])r.GetValue(i))[0], dataTypeName: "String");
			SetProviderField<DbDataReader, char, byte[]>((r, i) => Encoding.UTF8.GetString((byte[])r.GetValue(i))[0], dataTypeName: "String", dataReaderType: Adapter.DataReaderType);

			_sqlOptimizer = new YdbSqlOptimizer(SqlProviderFlags);
		}

		public override TableOptions SupportedTableOptions =>
			TableOptions.IsTemporary |
			TableOptions.IsLocalTemporaryStructure |
			TableOptions.IsLocalTemporaryData |
			TableOptions.CreateIfNotExists |
			TableOptions.DropIfExists;

		protected override IMemberTranslator CreateMemberTranslator() => new YdbMemberTranslator();

		public override ISqlBuilder CreateSqlBuilder(MappingSchema mappingSchema, DataOptions dataOptions)
		{
			return new YdbSqlBuilder(this, mappingSchema, dataOptions, GetSqlOptimizer(dataOptions), SqlProviderFlags);
		}

		private readonly ISqlOptimizer _sqlOptimizer;
		public override ISqlOptimizer GetSqlOptimizer(DataOptions dataOptions) => _sqlOptimizer;

		public override ISchemaProvider GetSchemaProvider() => new YdbSchemaProvider();

		// GetSchemaTable not implemented by provider
		public override bool? IsDBNullAllowed(DataOptions options, DbDataReader reader, int idx) => true;

		public override void SetParameter(DataConnection dataConnection, DbParameter parameter, string name, DbDataType dataType, object? value)
		{
			// handle various provider parameter support features/issues

			// provider doesn't support char type
			if (value is char chr)
				value = chr.ToString();

			switch (dataType.DataType)
			{
				case DataType.Binary:
				case DataType.VarBinary:
				{
					if (value is string str)
						value = Encoding.UTF8.GetBytes(str);
				}

				break;

				case DataType.SByte:
				{
					if (value is byte b)
						value = checked((sbyte)b);
					else if (value is short s)
						value = checked((sbyte)s);
					else if (value is ushort us)
						value = checked((sbyte)us);
					else if (value is int i)
						value = checked((sbyte)i);
					else if (value is uint u)
						value = checked((sbyte)u);
					else if (value is ulong ul)
						value = checked((sbyte)ul);
					else if (value is long l)
						value = checked((sbyte)l);
					else if (value is float f)
						value = checked((sbyte)f);
					else if (value is double dbl)
						value = checked((sbyte)dbl);
					else if (value is decimal d)
						value = checked((sbyte)d);
				}

				break;

				case DataType.Byte:
				{
					if (value is bool b)
						value = b ? (byte)1 : (byte)0;
					else if (value is sbyte sb)
						value = checked((byte)sb);
					else if (value is ushort us)
						value = checked((byte)us);
					else if (value is short s)
						value = checked((byte)s);
					else if (value is uint u)
						value = checked((byte)u);
					else if (value is int i)
						value = checked((byte)i);
					else if (value is ulong ul)
						value = checked((byte)ul);
					else if (value is long l)
						value = checked((byte)l);
					else if (value is decimal d)
						value = checked((byte)d);
					else if (value is float f)
						value = checked((byte)f);
					else if (value is double dbl)
						value = checked((byte)dbl);
				}

				break;

				case DataType.Int16:
				{
					if (value is ushort us)
						value = checked((short)us);
					else if (value is uint u)
						value = checked((short)u);
					else if (value is int i)
						value = checked((short)i);
					else if (value is long l)
						value = checked((short)l);
					else if (value is ulong ul)
						value = checked((short)ul);
					else if (value is decimal d)
						value = checked((short)d);
					else if (value is float f)
						value = checked((short)f);
					else if (value is double dbl)
						value = checked((short)dbl);
				}

				break;

				case DataType.UInt16:
				{
					if (value is sbyte sb)
						value = checked((ushort)sb);
					else if (value is short s)
						value = checked((ushort)s);
					else if (value is uint u)
						value = checked((ushort)u);
					else if (value is int i)
						value = checked((ushort)i);
					else if (value is ulong ul)
						value = checked((ushort)ul);
					else if (value is long l)
						value = checked((ushort)l);
					else if (value is decimal d)
						value = checked((ushort)d);
					else if (value is float f)
						value = checked((ushort)f);
					else if (value is double dbl)
						value = checked((ushort)dbl);
				}

				break;

				case DataType.Int32:
				{
					if (value is uint u)
						value = checked((int)u);
					else if (value is ulong ul)
						value = checked((int)ul);
					else if (value is long l)
						value = checked((int)l);
					else if (value is decimal d)
						value = checked((int)d);
					else if (value is float f)
						value = checked((int)f);
					else if (value is double dbl)
						value = checked((int)dbl);
				}

				break;

				case DataType.UInt32:
				{
					if (value is ulong ul)
						value = checked((uint)ul);
					else if (value is sbyte sb)
						value = checked((uint)sb);
					else if (value is short s)
						value = checked((uint)s);
					else if (value is int i)
						value = checked((uint)i);
					else if (value is long l)
						value = checked((uint)l);
					else if (value is decimal d)
						value = checked((uint)d);
					else if (value is float f)
						value = checked((uint)f);
					else if (value is double dbl)
						value = checked((uint)dbl);
				}

				break;

				case DataType.UInt64:
				{
					if (value is sbyte sb)
						value = checked((ulong)sb);
					else if (value is short s)
						value = checked((ulong)s);
					else if (value is int i)
						value = checked((ulong)i);
					else if (value is long l)
						value = checked((ulong)l);
					else if (value is decimal d)
						value = checked((ulong)d);
					else if (value is float f)
						value = checked((ulong)f);
					else if (value is double dbl)
						value = checked((ulong)dbl);
				}

				break;

				case DataType.Int64:
				{
					if (value is ulong ul)
						value = checked((long)ul);
					else if (value is decimal d)
						value = checked((long)d);
					else if (value is float f)
						value = checked((long)f);
					else if (value is double dbl)
						value = checked((long)dbl);
				}

				break;

				case DataType.DecFloat:
				{
					if (value is byte b)
						value = (decimal)b;
				}

				break;

				case DataType.Single:
				{
					if (value is byte b)
						value = checked((float)b);
					else if (value is sbyte sb)
						value = checked((float)sb);
					else if (value is ushort us)
						value = checked((float)us);
					else if (value is short s)
						value = checked((float)s);
					else if (value is uint u)
						value = checked((float)u);
					else if (value is int i)
						value = checked((float)i);
					else if (value is ulong ul)
						value = checked((float)ul);
					else if (value is long l)
						value = checked((float)l);
					else if (value is decimal d)
						value = checked((float)d);
					else if (value is double dbl)
						value = checked((float)dbl);
				}

				break;

				case DataType.Double:
				{
					if (value is byte b)
						value = checked((double)b);
					else if (value is sbyte sb)
						value = checked((double)sb);
					else if (value is ushort us)
						value = checked((double)us);
					else if (value is short s)
						value = checked((double)s);
					else if (value is uint u)
						value = checked((double)u);
					else if (value is int i)
						value = checked((double)i);
					else if (value is ulong ul)
						value = checked((double)ul);
					else if (value is long l)
						value = checked((double)l);
					else if (value is decimal d)
						value = checked((double)d);
				}

				break;

				case DataType.Date:
				{
					if (value is DateTimeOffset dto)
						value = dto.LocalDateTime.Date;
				}

				break;

				case DataType.DateTime:
				{
					if (value is DateTimeOffset dto)
						value = dto.LocalDateTime;
				}

				break;

				case DataType.DateTime2:
				{
					if (value is DateTimeOffset dto)
						dataType = dataType.WithDataType(DataType.DateTimeOffset);
				}

				break;

				case DataType.Yson:
				{
					if (value is null)
						value = Adapter.YsonNull;
					else
					{
						if (value is string str)
							value = Encoding.UTF8.GetBytes(str);

						if (value is byte[] val)
							value = Adapter.MakeYson(val);
					}
				}

				break;

				case DataType.Decimal:
				{
					if      (value is byte b    ) value = (decimal)b;
					else if (value is sbyte sb  ) value = (decimal)sb;
					else if (value is ushort us ) value = (decimal)us;
					else if (value is short s   ) value = (decimal)s;
					else if (value is uint u    ) value = (decimal)u;
					else if (value is int i     ) value = (decimal)i;
					else if (value is ulong ul  ) value = (decimal)ul;
					else if (value is long l    ) value = (decimal)l;
					else if (value is float f   ) value = checked((decimal)f);
					else if (value is double dbl) value = checked((decimal)dbl);
					else if (value is string str)
						value = Adapter.MakeDecimalFromString(str, dataType.Precision ?? YdbMappingSchema.DEFAULT_DECIMAL_PRECISION, dataType.Scale ?? YdbMappingSchema.DEFAULT_DECIMAL_SCALE);
				}

				break;
			}

			base.SetParameter(dataConnection, parameter, name, dataType, value);
		}

		protected override void SetParameterType(DataConnection dataConnection, DbParameter parameter, DbDataType dataType)
		{
			YdbProviderAdapter.YdbDbType? type = null;

			switch (dataType.DataType)
			{
				case DataType.Json      : type = YdbProviderAdapter.YdbDbType.Json;         break;
				case DataType.BinaryJson: type = YdbProviderAdapter.YdbDbType.JsonDocument; break;
				case DataType.Interval  : type = YdbProviderAdapter.YdbDbType.Interval;     break;

				case DataType.Decimal:
				{
					if (dataType.Precision != null)
						parameter.Precision = (byte)dataType.Precision.Value;

					if (dataType.Scale != null)
						parameter.Scale = (byte)dataType.Scale.Value;

					break;
				}
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

		public override BulkCopyRowsCopied BulkCopy<T>(DataOptions options, ITable<T> table, IEnumerable<T> source)
		{
			return new YdbBulkCopy(this).BulkCopy(
				options.BulkCopyOptions.BulkCopyType == BulkCopyType.Default ?
					options.FindOrDefault(YdbOptions.Default).BulkCopyType :
					options.BulkCopyOptions.BulkCopyType,
				table,
				options,
				source);
		}

		public override Task<BulkCopyRowsCopied> BulkCopyAsync<T>(DataOptions options, ITable<T> table,
			IEnumerable<T> source, CancellationToken cancellationToken)
		{
			return new YdbBulkCopy(this).BulkCopyAsync(
				options.BulkCopyOptions.BulkCopyType == BulkCopyType.Default ?
					options.FindOrDefault(YdbOptions.Default).BulkCopyType :
					options.BulkCopyOptions.BulkCopyType,
				table,
				options,
				source,
				cancellationToken);
		}

		public override Task<BulkCopyRowsCopied> BulkCopyAsync<T>(DataOptions options, ITable<T> table,
			IAsyncEnumerable<T> source, CancellationToken cancellationToken)
		{
			return new YdbBulkCopy(this).BulkCopyAsync(
				options.BulkCopyOptions.BulkCopyType == BulkCopyType.Default ?
					options.FindOrDefault(YdbOptions.Default).BulkCopyType :
					options.BulkCopyOptions.BulkCopyType,
				table,
				options,
				source,
				cancellationToken);
		}

		#endregion
	}
}
