using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Data;
using LinqToDB.DataProvider.Ydb;
using LinqToDB.Internal.DataProvider.Ydb.Translation;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Linq.Translation;
using LinqToDB.Mapping;
using LinqToDB.SchemaProvider;

namespace LinqToDB.Internal.DataProvider.Ydb
{
	/// <summary>
	/// YDB database provider for Linq to DB.
	/// Supports only basic features: automatic table creation, CRUD operations, and simple data types.
	/// Does not support bulk operations, transactions, or complex expressions.
	/// </summary>
	public class YdbDataProvider : DynamicDataProviderBase<YdbProviderAdapter>
	{
		// Provider name (used in MappingSchema configuration).
		private const string NameString   = "YDB";
		public const  string ProviderName = "YDB";

		// Static mapping schema for YDB type definitions.
		private static readonly MappingSchema YdbMappingSchema;

		// Static constructor initializes the mapping schema.
		static YdbDataProvider()
		{
			// Create a named schema specific to YDB.
			YdbMappingSchema = new MappingSchema(NameString);

			// Register basic .NET type to DataType mappings.
			// Numeric types:
			YdbMappingSchema.SetDataType(typeof(sbyte), DataType.SByte);
			YdbMappingSchema.SetDataType(typeof(short), DataType.Int16);
			YdbMappingSchema.SetDataType(typeof(int), DataType.Int32);
			YdbMappingSchema.SetDataType(typeof(long), DataType.Int64);
			YdbMappingSchema.SetDataType(typeof(byte), DataType.Byte);
			YdbMappingSchema.SetDataType(typeof(ushort), DataType.UInt16);
			YdbMappingSchema.SetDataType(typeof(uint), DataType.UInt32);
			YdbMappingSchema.SetDataType(typeof(ulong), DataType.UInt64);
			YdbMappingSchema.SetDataType(typeof(bool), DataType.Boolean);
			YdbMappingSchema.SetDataType(typeof(float), DataType.Single);
			YdbMappingSchema.SetDataType(typeof(double), DataType.Double);
			YdbMappingSchema.SetDataType(typeof(decimal), DataType.Decimal); // YDB supports Decimal(22,9); minimally mapped to .NET decimal.

			// String types:
			YdbMappingSchema.SetDataType(typeof(string), DataType.NVarChar);

			// Date and time:
			YdbMappingSchema.SetDataType(typeof(DateTime), DataType.DateTime);
			// Note: YDB stores DateTime with microsecond precision (Timestamp).
			// We map .NET DateTime to the appropriate YDB Timestamp via DataType.DateTime.
		}

		public YdbDataProvider() : this(ProviderName) { }

		protected YdbDataProvider(string name)
			: base(name, GetMappingSchema(), YdbProviderAdapter.Instance)
		{

			// YDB (YQL) does not support UPDATE … FROM JOIN.
			// It uses UPDATE … ON (subquery) instead → disable the flag.
			SqlProviderFlags.IsUpdateFromSupported = false;

			// YQL supports IS [NOT] DISTINCT FROM operator.
			SqlProviderFlags.IsDistinctFromSupported = true;

			// SQL optimizer specific to this dialect.
			_sqlOptimizer = new YdbSqlOptimizer(SqlProviderFlags);
		}

		public override TableOptions SupportedTableOptions =>
			TableOptions.IsTemporary |
			TableOptions.IsLocalTemporaryStructure |
			TableOptions.IsLocalTemporaryData |
			TableOptions.CreateIfNotExists |
			TableOptions.DropIfExists;

		/// <summary>
		/// Creates an SQL expression builder for this YDB provider.
		/// </summary>
		/// <param name="mappingSchema">Type mapping schema.</param>
		/// <param name="dataOptions">Query execution context options.</param>
		public override ISqlBuilder CreateSqlBuilder(MappingSchema mappingSchema, DataOptions dataOptions)
		{
			// Return the specialized YDB SQL builder.
			return new YdbSqlBuilder(this, mappingSchema, dataOptions, GetSqlOptimizer(dataOptions), SqlProviderFlags);
		}

		/// <summary>
		/// Returns the SQL expression optimizer for YDB.
		/// </summary>
		private readonly ISqlOptimizer _sqlOptimizer;
		public override ISqlOptimizer GetSqlOptimizer(DataOptions dataOptions) => _sqlOptimizer;

		/// <summary>
		/// Performs a bulk copy (insert) for a collection of objects.
		/// Since native BulkCopy is not supported by YDB, it uses row-by-row insert logic.
		/// </summary>
		public override BulkCopyRowsCopied BulkCopy<T>(
			DataOptions options,
			ITable<T> table,
			IEnumerable<T> source)
		{
			var bcType = options.BulkCopyOptions.BulkCopyType;
			return new YdbBulkCopy()
				.BulkCopy(bcType, table, options, source);
		}

		/// <summary>
		/// Asynchronously performs a bulk copy (insert) for an object sequence.
		/// Internally still processes sequentially due to SDK limitations.
		/// </summary>
		public override Task<BulkCopyRowsCopied> BulkCopyAsync<T>(
			DataOptions options,
			ITable<T> table,
			IEnumerable<T> source,
			CancellationToken cancellationToken)
		{
			var bcType = options.BulkCopyOptions.BulkCopyType;
			return new YdbBulkCopy()
				.BulkCopyAsync(bcType, table, options, source, cancellationToken);
		}

		/// <summary>
		/// Asynchronously performs a bulk copy (insert) for an asynchronous object sequence.
		/// </summary>
		public override Task<BulkCopyRowsCopied> BulkCopyAsync<T>(
			DataOptions options,
			ITable<T> table,
			IAsyncEnumerable<T> source,
			CancellationToken cancellationToken)
		{
			var bcType = options.BulkCopyOptions.BulkCopyType;
			return new YdbBulkCopy()
				.BulkCopyAsync(bcType, table, options, source, cancellationToken);
		}

		/// <summary>
		/// The YDB driver does not support DbDataReader.GetSchemaTable().
		/// To prevent Linq to DB from trying to read schema metadata,
		/// we declare that values may be <c>DBNull</c> without schema validation.
		/// </summary>
		public override bool? IsDBNullAllowed(DataOptions options, DbDataReader reader, int idx)
		{
			// Returning true tells Linq to DB to check reader.IsDBNull(idx)
			// and avoid accessing GetSchemaTable().
			return true;
		}

		// Note: All BulkCopy methods here simply delegate to LinqToDB’s row-by-row logic,
		// because YDB SDK lacks optimized bulk insert APIs, or they are not used in this minimalist provider.

		private static MappingSchema GetMappingSchema()
		{
			return new YdbMappingSchema.YdbClientMappingSchema();
		}

		public override ISchemaProvider GetSchemaProvider()
		{
			return new YdbSchemaProvider();
		}

		protected override IMemberTranslator CreateMemberTranslator()
		{
			return new YdbMemberTranslator();
		}
	}
}
