using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

using OdbcType = LinqToDB.DataProvider.OdbcProviderAdapter.OdbcType;

namespace LinqToDB.DataProvider.Access
{
	using Common;
	using Data;
	using Mapping;
	using SchemaProvider;
	using SqlProvider;

	public class AccessODBCDataProvider : DynamicDataProviderBase<OdbcProviderAdapter>
	{
		public AccessODBCDataProvider() : base(ProviderName.AccessOdbc, _mappingSchemaInstance, OdbcProviderAdapter.GetInstance())
		{
			SqlProviderFlags.AcceptsTakeAsParameter           = false;
			SqlProviderFlags.IsSkipSupported                  = false;
			SqlProviderFlags.IsCountSubQuerySupported         = false;
			SqlProviderFlags.IsInsertOrUpdateSupported        = false;
			SqlProviderFlags.TakeHintsSupported               = TakeHints.Percent;
			SqlProviderFlags.IsCrossJoinSupported             = false;
			SqlProviderFlags.IsInnerJoinAsCrossSupported      = false;
			SqlProviderFlags.IsDistinctOrderBySupported       = false;
			SqlProviderFlags.IsDistinctSetOperationsSupported = false;
			SqlProviderFlags.IsParameterOrderDependent        = true;
			SqlProviderFlags.IsUpdateFromSupported            = false;
			SqlProviderFlags.DefaultMultiQueryIsolationLevel  = IsolationLevel.Unspecified;

			SetCharField            ("CHAR", (r, i) => r.GetString(i).TrimEnd(' '));
			SetCharFieldToType<char>("CHAR", DataTools.GetCharExpression);

			SetToType<DbDataReader, sbyte , int>  ("INTEGER" , (r, i) => unchecked((sbyte )r.GetInt32(i)));
			SetToType<DbDataReader, uint  , int>  ("INTEGER" , (r, i) => unchecked((uint  )r.GetInt32(i)));
			SetToType<DbDataReader, ulong , int>  ("INTEGER" , (r, i) => unchecked((ulong)(uint)r.GetInt32(i)));
			SetToType<DbDataReader, ushort, short>("SMALLINT", (r, i) => unchecked((ushort)r.GetInt16(i)));
			SetProviderField<DbDataReader, TimeSpan, DateTime>((r, i) => r.GetDateTime(i) - new DateTime(1899, 12, 30));

			_sqlOptimizer = new AccessODBCSqlOptimizer(SqlProviderFlags);
		}

		public override TableOptions SupportedTableOptions => TableOptions.None;

		public override ISqlBuilder CreateSqlBuilder(MappingSchema mappingSchema, DataOptions dataOptions)
		{
			return new AccessODBCSqlBuilder(this, mappingSchema, dataOptions, GetSqlOptimizer(), SqlProviderFlags);
		}

		readonly ISqlOptimizer _sqlOptimizer;

		public override ISqlOptimizer GetSqlOptimizer()
		{
			return _sqlOptimizer;
		}

		public override ISchemaProvider GetSchemaProvider()
		{
			return new AccessODBCSchemaProvider();
		}

		public override void SetParameter(DataConnection dataConnection, DbParameter parameter, string name, DbDataType dataType, object? value)
		{
#if NET6_0_OR_GREATER
			if (value is DateOnly d)
				value = d.ToDateTime(TimeOnly.MinValue);
#endif

			switch (dataType.DataType)
			{
				case DataType.SByte:
					if (value is sbyte sbyteVal)
						value = unchecked((byte)sbyteVal);
					break;
				case DataType.UInt16:
					if (value is ushort ushortVal)
						value = unchecked((short)ushortVal);
					break;
				case DataType.UInt32:
					if (value is uint uintVal)
						value = unchecked((int)uintVal);
					break;
				case DataType.Int64:
					if (value is long longValue)
						value = checked((int)longValue);
					break;
				case DataType.UInt64:
					if (value is ulong ulongValue)
						value = unchecked((int)checked((uint)ulongValue));
					break;
			}

			base.SetParameter(dataConnection, parameter, name, dataType, value);
		}

		protected override void SetParameterType(DataConnection dataConnection, DbParameter parameter, DbDataType dataType)
		{
			// https://docs.microsoft.com/en-us/sql/odbc/microsoft/microsoft-access-data-types?view=sql-server-ver15
			// https://docs.microsoft.com/en-us/sql/odbc/microsoft/data-type-limitations?view=sql-server-ver15
			OdbcType? type = null;
			switch (dataType.DataType)
			{
				case DataType.Variant: type = OdbcType.Binary ; break;
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

			switch (dataType.DataType)
			{
				case DataType.SByte     : parameter.DbType = DbType.Byte      ; return;
				case DataType.UInt16    : parameter.DbType = DbType.Int16     ; return;
				case DataType.UInt32    :
				case DataType.UInt64    :
				case DataType.Int64     : parameter.DbType = DbType.Int32     ; return;
				case DataType.Money     :
				case DataType.SmallMoney:
				case DataType.VarNumeric:
				case DataType.Decimal   : parameter.DbType = DbType.AnsiString; return;
				// fallback
				case DataType.Variant   : parameter.DbType = DbType.Binary    ; return;
			}

			base.SetParameterType(dataConnection, parameter, dataType);
		}

		static readonly MappingSchema _mappingSchemaInstance = new AccessMappingSchema.OdbcMappingSchema();

		#region BulkCopy

		public override BulkCopyRowsCopied BulkCopy<T>(DataOptions options, ITable<T> table, IEnumerable<T> source)
		{

			return new AccessBulkCopy().BulkCopy(
				options.BulkCopyOptions.BulkCopyType == BulkCopyType.Default ?
					AccessTools.DefaultBulkCopyType :
					options.BulkCopyOptions.BulkCopyType,
				table,
				options.BulkCopyOptions,
				source);
		}

		public override Task<BulkCopyRowsCopied> BulkCopyAsync<T>(DataOptions options, ITable<T> table,
			IEnumerable<T>    source,
			CancellationToken cancellationToken)
		{

			return new AccessBulkCopy().BulkCopyAsync(
				options.BulkCopyOptions.BulkCopyType == BulkCopyType.Default ?
					AccessTools.DefaultBulkCopyType :
					options.BulkCopyOptions.BulkCopyType,
				table,
				options.BulkCopyOptions,
				source,
				cancellationToken);
		}

#if NATIVE_ASYNC
		public override Task<BulkCopyRowsCopied> BulkCopyAsync<T>(DataOptions options, ITable<T> table,
			IAsyncEnumerable<T> source, CancellationToken cancellationToken)
		{

			return new AccessBulkCopy().BulkCopyAsync(
				options.BulkCopyOptions.BulkCopyType == BulkCopyType.Default ?
					AccessTools.DefaultBulkCopyType :
					options.BulkCopyOptions.BulkCopyType,
				table,
				options.BulkCopyOptions,
				source,
				cancellationToken);
		}
#endif

		#endregion
	}
}
