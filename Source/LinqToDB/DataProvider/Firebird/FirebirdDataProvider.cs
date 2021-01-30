using System;
using System.Collections.Generic;
using System.Data;

namespace LinqToDB.DataProvider.Firebird
{
	using Common;
	using Data;
	using Mapping;
	using SqlProvider;
	using System.Threading;
	using System.Threading.Tasks;

	public class FirebirdDataProvider : DynamicDataProviderBase<FirebirdProviderAdapter>
	{
		public FirebirdDataProvider(FirebirdVersion version = FirebirdVersion.v2_5, FirebirdDialect dialect = FirebirdDialect.Dialect3)
			: this(GetProviderName(version, dialect), version, dialect)
		{
		}

		[Obsolete("To specify custom sql optimizer, subclass provider and override GetSqlOptimizer() method")]
		public FirebirdDataProvider(ISqlOptimizer sqlOptimizer)
			: this(ProviderName.Firebird, FirebirdVersion.v2_5, FirebirdDialect.Dialect3)
		{
			_sqlOptimizer = sqlOptimizer;
		}

		protected FirebirdDataProvider(string name, FirebirdVersion version, FirebirdDialect dialect)
			: base(
				  name,
				  GetMappingSchema(version, dialect, FirebirdProviderAdapter.GetInstance().MappingSchema),
				  FirebirdProviderAdapter.GetInstance())
		{
			Version = version;
			Dialect = dialect;

			SqlProviderFlags.IsIdentityParameterRequired       = true;
			SqlProviderFlags.IsCommonTableExpressionsSupported = true;
			SqlProviderFlags.IsSubQueryOrderBySupported        = true;
			SqlProviderFlags.IsDistinctSetOperationsSupported  = false;
			SqlProviderFlags.IsUpdateFromSupported             = false;

			SetCharField("CHAR", (r,i) => r.GetString(i).TrimEnd(' '));
			SetCharFieldToType<char>("CHAR", DataTools.GetCharExpression);

			SetProviderField<IDataReader,TimeSpan,DateTime>((r,i) => r.GetDateTime(i) - new DateTime(1970, 1, 1));
			SetProviderField<IDataReader,DateTime,DateTime>((r,i) => GetDateTime(r.GetDateTime(i)));

			_sqlOptimizer = new FirebirdSqlOptimizer(SqlProviderFlags);
		}

		public FirebirdVersion Version { get; }
		public FirebirdDialect Dialect { get; }

		static DateTime GetDateTime(DateTime value)
		{
			if (value.Year == 1970 && value.Month == 1 && value.Day == 1)
				return new DateTime(1, 1, 1, value.Hour, value.Minute, value.Second, value.Millisecond);

			return value;
		}

		public override TableOptions SupportedTableOptions =>
			TableOptions.IsTemporary                |
			TableOptions.IsGlobalTemporaryStructure |
			TableOptions.IsLocalTemporaryData       |
			TableOptions.IsTransactionTemporaryData |
			TableOptions.CreateIfNotExists          |
			TableOptions.DropIfExists;

		public override ISqlBuilder CreateSqlBuilder(MappingSchema mappingSchema)
		{
			return Version switch
			{
				FirebirdVersion.v3 => Dialect == FirebirdDialect.Dialect1 ? new Firebird3Dialect1SqlBuilder(this, mappingSchema, GetSqlOptimizer(), SqlProviderFlags)  : new Firebird3SqlBuilder(this, mappingSchema, GetSqlOptimizer(), SqlProviderFlags),
				FirebirdVersion.v4 => Dialect == FirebirdDialect.Dialect1 ? new Firebird4Dialect1SqlBuilder(this, mappingSchema, GetSqlOptimizer(), SqlProviderFlags)  : new Firebird4SqlBuilder(this, mappingSchema, GetSqlOptimizer(), SqlProviderFlags),
				_                  => Dialect == FirebirdDialect.Dialect1 ? new Firebird25Dialect1SqlBuilder(this, mappingSchema, GetSqlOptimizer(), SqlProviderFlags) : new Firebird25SqlBuilder(this, mappingSchema, GetSqlOptimizer(), SqlProviderFlags),
			};
		}

		readonly ISqlOptimizer _sqlOptimizer;

		public override ISqlOptimizer GetSqlOptimizer() => _sqlOptimizer;

		public override SchemaProvider.ISchemaProvider GetSchemaProvider() => new FirebirdSchemaProvider();

		public override void SetParameter(DataConnection dataConnection, IDbDataParameter parameter, string name, DbDataType dataType, object? value)
		{
			if (value is bool)
			{
				value = (bool)value ? "1" : "0";
				dataType = dataType.WithDataType(DataType.Char);
			}

			base.SetParameter(dataConnection, parameter, name, dataType, value);
		}

		public override bool? IsDBNullAllowed(IDataReader reader, int idx) => true;

		protected override void SetParameterType(DataConnection dataConnection, IDbDataParameter parameter, DbDataType dataType)
		{
			switch (dataType.DataType)
			{
				case DataType.SByte      : dataType = dataType.WithDataType(DataType.Int16);    break;
				case DataType.UInt16     : dataType = dataType.WithDataType(DataType.Int32);    break;
				case DataType.UInt32     : dataType = dataType.WithDataType(DataType.Int64);    break;
				case DataType.UInt64     : dataType = dataType.WithDataType(DataType.Decimal);  break;
				case DataType.VarNumeric : dataType = dataType.WithDataType(DataType.Decimal);  break;
				case DataType.DateTime2  : dataType = dataType.WithDataType(DataType.DateTime); break;
			}

			base.SetParameterType(dataConnection, parameter, dataType);
		}

		private static string GetProviderName(FirebirdVersion version, FirebirdDialect dialect)
		{
			return version switch
			{
				FirebirdVersion.v3 => dialect == FirebirdDialect.Dialect1 ? ProviderName.Firebird3Dialect1  : ProviderName.Firebird3,
				FirebirdVersion.v4 => dialect == FirebirdDialect.Dialect1 ? ProviderName.Firebird4Dialect1  : ProviderName.Firebird4,
				_                  => dialect == FirebirdDialect.Dialect1 ? ProviderName.Firebird25Dialect1 : ProviderName.Firebird25,
			};
		}

		private static MappingSchema GetMappingSchema(FirebirdVersion version, FirebirdDialect dialect, MappingSchema providerSchema)
		{
			return version switch
			{
				FirebirdVersion.v3 => dialect == FirebirdDialect.Dialect1 ? new Firebird3Dialect1MappingSchema(providerSchema)  : new Firebird3MappingSchema(providerSchema),
				FirebirdVersion.v4 => dialect == FirebirdDialect.Dialect1 ? new Firebird4Dialect1MappingSchema(providerSchema)  : new Firebird4MappingSchema(providerSchema),
				_                  => dialect == FirebirdDialect.Dialect1 ? new Firebird25Dialect1MappingSchema(providerSchema) : new Firebird25MappingSchema(providerSchema),
			};
		}

		#region BulkCopy

		public override BulkCopyRowsCopied BulkCopy<T>(
			ITable<T> table, BulkCopyOptions options, IEnumerable<T> source)
		{
			return new FirebirdBulkCopy().BulkCopy(
				options.BulkCopyType == BulkCopyType.Default ? FirebirdTools.DefaultBulkCopyType : options.BulkCopyType,
				table,
				options,
				source);
		}

		public override Task<BulkCopyRowsCopied> BulkCopyAsync<T>(
			ITable<T> table, BulkCopyOptions options, IEnumerable<T> source, CancellationToken cancellationToken)
		{
			return new FirebirdBulkCopy().BulkCopyAsync(
				options.BulkCopyType == BulkCopyType.Default ? FirebirdTools.DefaultBulkCopyType : options.BulkCopyType,
				table,
				options,
				source,
				cancellationToken);
		}

#if !NETFRAMEWORK
		public override Task<BulkCopyRowsCopied> BulkCopyAsync<T>(
			ITable<T> table, BulkCopyOptions options, IAsyncEnumerable<T> source, CancellationToken cancellationToken)
		{
			return new FirebirdBulkCopy().BulkCopyAsync(
				options.BulkCopyType == BulkCopyType.Default ? FirebirdTools.DefaultBulkCopyType : options.BulkCopyType,
				table,
				options,
				source,
				cancellationToken);
		}
#endif

		#endregion
	}
}
