using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Linq;
using System.Globalization;
using System.IO;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Data;
using LinqToDB.DataProvider.DuckDB;
using LinqToDB.Internal.DataProvider.DuckDB.Translation;
using LinqToDB.Internal.Linq;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Linq.Translation;
using LinqToDB.Mapping;
using LinqToDB.SchemaProvider;

namespace LinqToDB.Internal.DataProvider.DuckDB
{
	public class DuckDBDataProvider : DynamicDataProviderBase<DuckDBProviderAdapter>
	{
		public DuckDBDataProvider()
			: base(ProviderName.DuckDB, DuckDBMappingSchema.Instance, DuckDBProviderAdapter.Instance)
		{
			SqlProviderFlags.IsCommonTableExpressionsSupported = true;
			SqlProviderFlags.IsSubQueryOrderBySupported        = true;
			SqlProviderFlags.IsUnionAllOrderBySupported        = true;
			SqlProviderFlags.IsAllSetOperationsSupported       = true;
			SqlProviderFlags.IsInsertOrUpdateSupported         = true;
			SqlProviderFlags.IsApplyJoinSupported              = true;
			SqlProviderFlags.IsCrossApplyJoinSupportsCondition = true;
			SqlProviderFlags.IsOuterApplyJoinSupportsCondition = true;
			SqlProviderFlags.IsDistinctFromSupported           = true;
			SqlProviderFlags.SupportsPredicatesComparison      = true;

			SqlProviderFlags.DefaultMultiQueryIsolationLevel = System.Data.IsolationLevel.Snapshot;

			SqlProviderFlags.RowConstructorSupport =
				RowFeature.CompareToSelect |
				RowFeature.Update          | RowFeature.UpdateLiteral;

			SetCharFieldToType<char>("VARCHAR", DataTools.GetCharExpression);

			// for supported readers see
			// https://github.com/Giorgi/DuckDB.NET/tree/develop/DuckDB.NET.Data/DataChunk/Reader

			// DuckDB.NET returns BLOB columns as Stream (UnmanagedMemoryStream).
			// Register reader expressions to convert to byte[].
			SetToType<DbDataReader, byte[], Stream>((r, i) => ReadStreamToBytes(r.GetStream(i)));
			ReaderExpressions[new ReaderInfo { FieldType = typeof(Stream) }] =
				(Expression<Func<DbDataReader, int, byte[]>>)((r, i) => ReadStreamToBytes(r.GetStream(i)));

#if SUPPORTS_DATEONLY
			// DuckDB.NET returns TIME columns as TimeOnly; convert to TimeSpan when needed.
			ReaderExpressions[new ReaderInfo { ToType = typeof(TimeSpan), FieldType = typeof(TimeOnly) }] =
				(Expression<Func<DbDataReader, int, TimeSpan>>)((r, i) => TimeOnlyToTimeSpan(r, i));
			ReaderExpressions[new ReaderInfo { ToType = typeof(DateTime), FieldType = typeof(TimeOnly) }] =
				(Expression<Func<DbDataReader, int, DateTime>>)((r, i) => r.GetDateTime(i));
#endif

			// DuckDB.NET returns TIMESTAMPTZ as DateTime(Kind=Utc); convert to DateTimeOffset preserving UTC.
			ReaderExpressions[new ReaderInfo { ToType = typeof(DateTimeOffset), FieldType = typeof(DateTime) }] =
				(Expression<Func<DbDataReader, int, DateTimeOffset>>)((r, i) => r.GetFieldValue<DateTimeOffset>(i));

			// BITSTRING default reader is string
			ReaderExpressions[new ReaderInfo { ToType = typeof(BitArray), FieldType = typeof(string) }] =
				(Expression<Func<DbDataReader, int, BitArray>>)((r, i) => r.GetFieldValue<BitArray>(i));
			SetToType<DbDataReader, byte[], string>("Bit", (r, i) => ParseBitString(r.GetString(i)));

			// TIMETZ
#if SUPPORTS_DATEONLY
			SetToType<DbDataReader, TimeSpan, DateTimeOffset>((r, i) => r.GetFieldValue<DateTimeOffset>(i).UtcDateTime.TimeOfDay);
			SetToType<DbDataReader, TimeOnly, DateTimeOffset>((r, i) => TimeOnly.FromDateTime(r.GetFieldValue<DateTimeOffset>(i).UtcDateTime));
#endif

			SetGetFieldValueReader(Adapter.DuckDBDateOnly,  Adapter.DuckDBDateOnly,  Adapter.DataReaderType);
			SetGetFieldValueReader(Adapter.DuckDBTimeOnly,  Adapter.DuckDBTimeOnly,  Adapter.DataReaderType);
			SetGetFieldValueReader(Adapter.DuckDBTimestamp, Adapter.DuckDBTimestamp, Adapter.DataReaderType);
			SetGetFieldValueReader(Adapter.DuckDBInterval,  Adapter.DuckDBInterval,  Adapter.DataReaderType);

			_sqlOptimizer = new DuckDBSqlOptimizer(SqlProviderFlags);
			_bulkCopy     = new DuckDBBulkCopy(this);
		}

		private byte[] ParseBitString(string bits)
		{
			var bytes = new byte[(bits.Length + 7) / 8];

			for (var i = 0; i < bits.Length; i++)
			{
				var c = bits[i];
				if (c == '1')
				{
					bytes[i / 8] |= (byte)(1 << (i % 8));
				}
				else if (c != '0')
				{
					throw new FormatException(string.Create(CultureInfo.InvariantCulture, $"Invalid bitstring character '{c}' at index {i}."));
				}
			}

			return bytes;
		}

		public override TableOptions SupportedTableOptions =>
			TableOptions.IsTemporary               |
			TableOptions.IsLocalTemporaryStructure |
			TableOptions.IsLocalTemporaryData      |
			TableOptions.CreateIfNotExists         |
			TableOptions.DropIfExists;

		protected override IMemberTranslator CreateMemberTranslator() => new DuckDBMemberTranslator();

		public override ISqlBuilder CreateSqlBuilder(MappingSchema mappingSchema, DataOptions dataOptions)
		{
			return new DuckDBSqlBuilder(this, mappingSchema, dataOptions, GetSqlOptimizer(dataOptions), SqlProviderFlags);
		}

		private readonly ISqlOptimizer _sqlOptimizer;
		public override ISqlOptimizer GetSqlOptimizer(DataOptions dataOptions) => _sqlOptimizer;

		public override ISchemaProvider GetSchemaProvider() => new DuckDBSchemaProvider();

		public override void SetParameter(DataConnection dataConnection, DbParameter parameter, string name, DbDataType dataType, object? value)
		{
			// DuckDB.NET expects parameter names without $ prefix.
			// BulkCopy may pass names with $ prefix — strip it.
			if (name.Length > 0 && name[0] == '$')
				name = name.Substring(1);

			if (value is DateTimeOffset dto)
			{
				if (dataType.DataType == DataType.DateTime)
					value = dto.DateTime;
			}
			else if (value is TimeSpan ts)
			{
				if (dataType.DataType == DataType.TimeTZ)
					value = DateTimeOffset.MinValue + ts;
				else if (dataType.DataType == DataType.Int64)
					value = ts.Ticks;
#if NET8_0_OR_GREATER
				else if (dataType.DataType == DataType.Time)
					value = TimeOnly.FromTimeSpan(ts);
#endif
			}

			if (value is Binary b)
				value = b.ToArray();

			// don't call base implementation: it sets parameter value after type which result in type being reset to string type
			parameter.ParameterName = name;
			parameter.Value         = value;
			// must be called after value set!
			SetParameterType(dataConnection, parameter, dataType);

			if (dataType.DataType == DataType.Decimal)
			{
				parameter.Precision = (byte)(dataType.Precision ?? 18);
				parameter.Scale     = (byte)(dataType.Scale     ?? 3);
			}
		}

		protected override void SetParameterType(DataConnection dataConnection, DbParameter parameter, DbDataType dataType)
		{
			switch (dataType.DataType)
			{
				case DataType.VarNumeric: parameter.DbType = System.Data.DbType.Decimal ; return;
			}

			base.SetParameterType(dataConnection, parameter, dataType);
		}

		static byte[] ReadStreamToBytes(Stream stream)
		{
			using (stream)
			{
				if (stream is MemoryStream ms)
					return ms.ToArray();

				using var result = new MemoryStream();
				stream.CopyTo(result);
				return result.ToArray();
			}
		}

#if SUPPORTS_DATEONLY
		[ColumnReader(1)]
		static TimeSpan TimeOnlyToTimeSpan(DbDataReader reader, int index)
		{
			var value = reader.GetFieldValue<TimeOnly>(index);
			return value.ToTimeSpan();
		}
#endif

		#region BulkCopy

		private readonly DuckDBBulkCopy _bulkCopy;

		public override BulkCopyRowsCopied BulkCopy<T>(DataOptions options, ITable<T> table, IEnumerable<T> source)
		{
			return _bulkCopy.BulkCopy(
				options.BulkCopyOptions.BulkCopyType == BulkCopyType.Default ?
					options.FindOrDefault(DuckDBOptions.Default).BulkCopyType :
					options.BulkCopyOptions.BulkCopyType,
				table,
				options,
				source);
		}

		public override Task<BulkCopyRowsCopied> BulkCopyAsync<T>(DataOptions options, ITable<T> table,
			IEnumerable<T> source, CancellationToken cancellationToken)
		{
			return _bulkCopy.BulkCopyAsync(
				options.BulkCopyOptions.BulkCopyType == BulkCopyType.Default ?
					options.FindOrDefault(DuckDBOptions.Default).BulkCopyType :
					options.BulkCopyOptions.BulkCopyType,
				table,
				options,
				source,
				cancellationToken);
		}

		public override Task<BulkCopyRowsCopied> BulkCopyAsync<T>(DataOptions options, ITable<T> table,
			IAsyncEnumerable<T> source, CancellationToken cancellationToken)
		{
			return _bulkCopy.BulkCopyAsync(
				options.BulkCopyOptions.BulkCopyType == BulkCopyType.Default ?
					options.FindOrDefault(DuckDBOptions.Default).BulkCopyType :
					options.BulkCopyOptions.BulkCopyType,
				table,
				options,
				source,
				cancellationToken);
		}

		#endregion
	}
}
