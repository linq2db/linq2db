using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.DataProvider.MySql
{
	using Common;
	using Data;
	using Mapping;
	using SqlProvider;

	public class MySqlDataProvider : DynamicDataProviderBase<MySqlProviderAdapter>
	{
		public MySqlDataProvider(string name)
			: this(name, null)
		{
		}

		protected MySqlDataProvider(string name, MappingSchema? mappingSchema)
			: base(
				  name,
				  mappingSchema != null
					? new MappingSchema(mappingSchema, MySqlProviderAdapter.GetInstance().MappingSchema)
					: GetMappingSchema(name, MySqlProviderAdapter.GetInstance().MappingSchema),
				  MySqlProviderAdapter.GetInstance())
		{
			SqlProviderFlags.IsDistinctOrderBySupported        = false;
			SqlProviderFlags.IsSubQueryOrderBySupported        = true;
			SqlProviderFlags.IsCommonTableExpressionsSupported = true;
			SqlProviderFlags.IsDistinctSetOperationsSupported  = false;
			SqlProviderFlags.IsUpdateFromSupported             = false;

			_sqlOptimizer = new MySqlSqlOptimizer(SqlProviderFlags);

			// configure provider-specific data readers
			SetProviderField(typeof(DateTimeOffset), Adapter.GetDateTimeOffsetMethodName, Adapter.DataReaderType);
			SetToTypeField  (typeof(DateTimeOffset), Adapter.GetDateTimeOffsetMethodName, Adapter.DataReaderType);

			SetProviderField(Adapter.MySqlDateTimeType, Adapter.GetMySqlDateTimeMethodName, Adapter.DataReaderType);
			SetToTypeField  (Adapter.MySqlDateTimeType, Adapter.GetMySqlDateTimeMethodName, Adapter.DataReaderType);
		}

		public override SchemaProvider.ISchemaProvider GetSchemaProvider()
		{
			return new MySqlSchemaProvider(this);
		}

		public override TableOptions SupportedTableOptions =>
			TableOptions.IsTemporary               |
			TableOptions.IsLocalTemporaryStructure |
			TableOptions.IsLocalTemporaryData      |
			TableOptions.CreateIfNotExists         |
			TableOptions.DropIfExists;

		public override ISqlBuilder CreateSqlBuilder(MappingSchema mappingSchema)
		{
			return new MySqlSqlBuilder(this, mappingSchema, GetSqlOptimizer(), SqlProviderFlags);
		}

		private static MappingSchema GetMappingSchema(string name, MappingSchema providerSchema) => new MySqlMappingSchema.MySqlConnectorMappingSchema(providerSchema);

		readonly ISqlOptimizer _sqlOptimizer;

		public override ISqlOptimizer GetSqlOptimizer()
		{
			return _sqlOptimizer;
		}

#if !NETFRAMEWORK
		public override bool? IsDBNullAllowed(DbDataReader reader, int idx)
		{
			return true;
		}
#endif

		protected override void SetParameterType(DataConnection dataConnection, DbParameter parameter, DbDataType dataType)
		{
			switch (dataType.DataType)
			{
				// TODO: MySql.Data mappings, remove if MySqlConnector don't need them
				//case DataType.VarNumeric: parameter.DbType = DbType.Decimal;  return;
				//case DataType.Date:
				//case DataType.DateTime2 : parameter.DbType = DbType.DateTime; return;
				case DataType.BitArray  : parameter.DbType = DbType.UInt64;   return;
			}

			base.SetParameterType(dataConnection, parameter, dataType);
		}

		#region BulkCopy

		public override BulkCopyRowsCopied BulkCopy<T>(
			ITable<T> table, BulkCopyOptions options, IEnumerable<T> source)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));

			return new MySqlBulkCopy(this).BulkCopy(
				options.BulkCopyType == BulkCopyType.Default ? MySqlTools.DefaultBulkCopyType : options.BulkCopyType,
				table,
				options,
				source);
		}

		public override Task<BulkCopyRowsCopied> BulkCopyAsync<T>(
			ITable<T> table, BulkCopyOptions options, IEnumerable<T> source, CancellationToken cancellationToken)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));

			return new MySqlBulkCopy(this).BulkCopyAsync(
				options.BulkCopyType == BulkCopyType.Default ? MySqlTools.DefaultBulkCopyType : options.BulkCopyType,
				table,
				options,
				source,
				cancellationToken);
		}

#if NATIVE_ASYNC
		public override Task<BulkCopyRowsCopied> BulkCopyAsync<T>(
			ITable<T> table, BulkCopyOptions options, IAsyncEnumerable<T> source, CancellationToken cancellationToken)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));

			return new MySqlBulkCopy(this).BulkCopyAsync(
				options.BulkCopyType == BulkCopyType.Default ? MySqlTools.DefaultBulkCopyType : options.BulkCopyType,
				table,
				options,
				source,
				cancellationToken);
		}
#endif

		#endregion
	}
}
