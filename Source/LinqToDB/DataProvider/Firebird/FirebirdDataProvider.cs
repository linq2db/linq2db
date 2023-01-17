using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.DataProvider.Firebird
{
	using Common;
	using Data;
	using Mapping;
	using SqlProvider;

	public class FirebirdDataProvider : DynamicDataProviderBase<FirebirdProviderAdapter>
	{
		public FirebirdDataProvider() : this(ProviderName.Firebird, null)
		{
		}

		public FirebirdDataProvider(ISqlOptimizer sqlOptimizer)
			: this(ProviderName.Firebird, sqlOptimizer)
		{
		}

		protected FirebirdDataProvider(string name, ISqlOptimizer? sqlOptimizer)
			: base(name, GetMappingSchema(), FirebirdProviderAdapter.Instance)
		{
			SqlProviderFlags.IsIdentityParameterRequired       = true;
			SqlProviderFlags.IsCommonTableExpressionsSupported = true;
			SqlProviderFlags.IsSubQueryOrderBySupported        = true;
			SqlProviderFlags.IsDistinctSetOperationsSupported  = false;
			SqlProviderFlags.IsUpdateFromSupported             = false;
			SqlProviderFlags.OutputUpdateUseSpecialTables      = true;

			SetCharField("CHAR", (r,i) => r.GetString(i).TrimEnd(' '));
			SetCharFieldToType<char>("CHAR", DataTools.GetCharExpression);

			SetProviderField<DbDataReader, TimeSpan,DateTime>((r,i) => r.GetDateTime(i) - new DateTime(1970, 1, 1));
			SetProviderField<DbDataReader, DateTime,DateTime>((r,i) => GetDateTime(r.GetDateTime(i)));

			_sqlOptimizer = sqlOptimizer ?? new FirebirdSqlOptimizer(SqlProviderFlags);
		}

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

		public override ISqlBuilder CreateSqlBuilder(MappingSchema mappingSchema, DataOptions dataOptions)
		{
			return new FirebirdSqlBuilder(this, mappingSchema, dataOptions, GetSqlOptimizer(dataOptions), SqlProviderFlags);
		}

		readonly ISqlOptimizer _sqlOptimizer;

		public override ISqlOptimizer GetSqlOptimizer(DataOptions dataOptions)
		{
			return _sqlOptimizer;
		}

		public override SchemaProvider.ISchemaProvider GetSchemaProvider()
		{
			return new FirebirdSchemaProvider(this);
		}

		public override bool? IsDBNullAllowed(DataOptions options, DbDataReader reader, int idx)
		{
			return true;
		}

		public override void SetParameter(DataConnection dataConnection, DbParameter parameter, string name, DbDataType dataType, object? value)
		{
			if (value is bool boolVal)
			{
				value    = boolVal ? "1" : "0";
				dataType = dataType.WithDataType(DataType.Char);
			}

#if NET6_0_OR_GREATER
			if (!Adapter.IsDateOnlySupported && value is DateOnly d)
			{
				value = d.ToDateTime(TimeOnly.MinValue);
			}
#endif

			base.SetParameter(dataConnection, parameter, name, dataType, value);
		}

		protected override void SetParameterType(DataConnection dataConnection, DbParameter parameter, DbDataType dataType)
		{
			FirebirdProviderAdapter.FbDbType? type = null;
			switch (dataType.DataType)
			{
				case DataType.DateTimeOffset : type = FirebirdProviderAdapter.FbDbType.TimeStampTZ; break;
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
				case DataType.SByte      : dataType = dataType.WithDataType(DataType.Int16);    break;
				case DataType.UInt16     : dataType = dataType.WithDataType(DataType.Int32);    break;
				case DataType.UInt32     : dataType = dataType.WithDataType(DataType.Int64);    break;
				case DataType.UInt64     : dataType = dataType.WithDataType(DataType.Decimal);  break;
				case DataType.VarNumeric : dataType = dataType.WithDataType(DataType.Decimal);  break;
				case DataType.DateTime2  : dataType = dataType.WithDataType(DataType.DateTime); break;
			}

			base.SetParameterType(dataConnection, parameter, dataType);
		}

		static MappingSchema GetMappingSchema()
		{
			return new FirebirdMappingSchema.FirebirdProviderMappingSchema();
		}

		#region BulkCopy

		public override BulkCopyRowsCopied BulkCopy<T>(DataOptions options, ITable<T> table, IEnumerable<T> source)
		{
			return new FirebirdBulkCopy().BulkCopy(
				options.BulkCopyOptions.BulkCopyType == BulkCopyType.Default ?
					options.FindOrDefault(FirebirdOptions.Default).BulkCopyType :
					options.BulkCopyOptions.BulkCopyType,
				table,
				options,
				source);
		}

		public override Task<BulkCopyRowsCopied> BulkCopyAsync<T>(DataOptions options, ITable<T> table,
			IEnumerable<T> source, CancellationToken cancellationToken)
		{
			return new FirebirdBulkCopy().BulkCopyAsync(
				options.BulkCopyOptions.BulkCopyType == BulkCopyType.Default ?
					options.FindOrDefault(FirebirdOptions.Default).BulkCopyType :
					options.BulkCopyOptions.BulkCopyType,
				table,
				options,
				source,
				cancellationToken);
		}

#if NATIVE_ASYNC
		public override Task<BulkCopyRowsCopied> BulkCopyAsync<T>(DataOptions options, ITable<T> table,
			IAsyncEnumerable<T> source, CancellationToken cancellationToken)
		{
			return new FirebirdBulkCopy().BulkCopyAsync(
				options.BulkCopyOptions.BulkCopyType == BulkCopyType.Default ?
					options.FindOrDefault(FirebirdOptions.Default).BulkCopyType :
					options.BulkCopyOptions.BulkCopyType,
				table,
				options,
				source,
				cancellationToken);
		}
#endif

		#endregion
	}
}
