using System;
using System.Collections.Generic;
using System.Data;

using OdbcType = LinqToDB.DataProvider.OdbcProviderAdapter.OdbcType;

namespace LinqToDB.DataProvider.Access
{
	using Common;
	using Data;
	using Mapping;
	using SchemaProvider;
	using SqlProvider;
	using System.Threading;
	using System.Threading.Tasks;

	public class AccessODBCDataProvider : DynamicDataProviderBase<OdbcProviderAdapter>
	{
		public AccessODBCDataProvider()
			: this(ProviderName.AccessOdbc, MappingSchemaInstance)
		{
		}

		protected AccessODBCDataProvider(string name, MappingSchema mappingSchema)
			: base(name, mappingSchema, OdbcProviderAdapter.GetInstance())
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

			SetToType<IDataReader, sbyte , int>  ("INTEGER" , (r, i) => unchecked((sbyte )r.GetInt32(i)));
			SetToType<IDataReader, uint  , int>  ("INTEGER" , (r, i) => unchecked((uint  )r.GetInt32(i)));
			SetToType<IDataReader, ulong , int>  ("INTEGER" , (r, i) => unchecked((ulong)(uint)r.GetInt32(i)));
			SetToType<IDataReader, ushort, short>("SMALLINT", (r, i) => unchecked((ushort)r.GetInt16(i)));
			SetProviderField<IDataReader, TimeSpan, DateTime>((r, i) => r.GetDateTime(i) - new DateTime(1899, 12, 30));

			_sqlOptimizer = new AccessODBCSqlOptimizer(SqlProviderFlags);
		}

		public override TableOptions SupportedTableOptions => TableOptions.None;

		public override ISqlBuilder CreateSqlBuilder(MappingSchema mappingSchema)
		{
			return new AccessODBCSqlBuilder(this, mappingSchema, GetSqlOptimizer(), SqlProviderFlags);
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

		public override void SetParameter(DataConnection dataConnection, IDbDataParameter parameter, string name, DbDataType dataType, object? value)
		{
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

		protected override void SetParameterType(DataConnection dataConnection, IDbDataParameter parameter, DbDataType dataType)
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
				var param = TryGetProviderParameter(parameter, dataConnection.MappingSchema);
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

		private static readonly MappingSchema MappingSchemaInstance = new AccessMappingSchema.ODBCMappingSchema();

		#region BulkCopy

		public override BulkCopyRowsCopied BulkCopy<T>(
			ITable<T> table, BulkCopyOptions options, IEnumerable<T> source)
		{

			return new AccessBulkCopy().BulkCopy(
				options.BulkCopyType == BulkCopyType.Default ? AccessTools.DefaultBulkCopyType : options.BulkCopyType,
				table,
				options,
				source);
		}

		public override Task<BulkCopyRowsCopied> BulkCopyAsync<T>(
			ITable<T> table, BulkCopyOptions options, IEnumerable<T> source, CancellationToken cancellationToken)
		{

			return new AccessBulkCopy().BulkCopyAsync(
				options.BulkCopyType == BulkCopyType.Default ? AccessTools.DefaultBulkCopyType : options.BulkCopyType,
				table,
				options,
				source,
				cancellationToken);
		}

#if NATIVE_ASYNC
		public override Task<BulkCopyRowsCopied> BulkCopyAsync<T>(
			ITable<T> table, BulkCopyOptions options, IAsyncEnumerable<T> source, CancellationToken cancellationToken)
		{

			return new AccessBulkCopy().BulkCopyAsync(
				options.BulkCopyType == BulkCopyType.Default ? AccessTools.DefaultBulkCopyType : options.BulkCopyType,
				table,
				options,
				source,
				cancellationToken);
		}
#endif

		#endregion
	}
}
