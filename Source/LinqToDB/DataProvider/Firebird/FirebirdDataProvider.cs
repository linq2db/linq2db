using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Linq;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.DataProvider.Firebird.Translation;
using LinqToDB.Internal.Linq.Translation;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Mapping;

namespace LinqToDB.DataProvider.Firebird
{
	sealed class FirebirdDataProvider25 : FirebirdDataProvider { public FirebirdDataProvider25() : base(ProviderName.Firebird25, FirebirdVersion.v25) { } }
	sealed class FirebirdDataProvider3  : FirebirdDataProvider { public FirebirdDataProvider3()  : base(ProviderName.Firebird3,  FirebirdVersion.v3 ) { } }
	sealed class FirebirdDataProvider4  : FirebirdDataProvider { public FirebirdDataProvider4()  : base(ProviderName.Firebird4,  FirebirdVersion.v4 ) { } }
	sealed class FirebirdDataProvider5  : FirebirdDataProvider { public FirebirdDataProvider5()  : base(ProviderName.Firebird5,  FirebirdVersion.v5 ) { } }

	public abstract class FirebirdDataProvider : DynamicDataProviderBase<FirebirdProviderAdapter>
	{
		protected FirebirdDataProvider(string name, FirebirdVersion version)
			: base(name, GetMappingSchema(version), FirebirdProviderAdapter.Instance)
		{
			Version = version;

			SqlProviderFlags.IsIdentityParameterRequired       = true;
			SqlProviderFlags.IsCommonTableExpressionsSupported = true;
			SqlProviderFlags.IsSubQueryOrderBySupported        = true;
			SqlProviderFlags.IsDistinctSetOperationsSupported  = false;
			SqlProviderFlags.IsUpdateFromSupported             = false;
			SqlProviderFlags.OutputUpdateUseSpecialTables      = true;
			SqlProviderFlags.OutputMergeUseSpecialTables       = true;
			SqlProviderFlags.IsExistsPreferableForContains     = true;
			SqlProviderFlags.IsWindowFunctionsSupported        = Version >= FirebirdVersion.v3;
			SqlProviderFlags.IsApplyJoinSupported              = Version >= FirebirdVersion.v4;
			// CROSS - doesn't support, OUTER - supports conditions
			SqlProviderFlags.IsOuterApplyJoinSupportsCondition = Version >= FirebirdVersion.v4;
			SqlProviderFlags.SupportsPredicatesComparison      = Version >= FirebirdVersion.v3;
			SqlProviderFlags.SupportsBooleanType               = Version >= FirebirdVersion.v3;

			SqlProviderFlags.MaxInListValuesCount = Version >= FirebirdVersion.v5 ? 65535 : 1500;

			SqlProviderFlags.IsUpdateTakeSupported     = true;
			SqlProviderFlags.IsUpdateSkipTakeSupported = true;
			SqlProviderFlags.IsDistinctFromSupported   = true;

			SqlProviderFlags.SupportedCorrelatedSubqueriesLevel = 1;

			SetCharField("CHAR", (r,i) => r.GetString(i).TrimEnd(' '));
			SetCharFieldToType<char>("CHAR", DataTools.GetCharExpression);

			SetProviderField<DbDataReader, TimeSpan, DateTime>((r,i) => r.GetDateTime(i) - new DateTime(1970, 1, 1));
			SetProviderField<DbDataReader, DateTime, DateTime>((r,i) => GetDateTime(r.GetDateTime(i)));

			SetToType<DbDataReader, byte[], string>("VARCHAR", (r, i) => r.GetFieldValue<byte[]>(i));
			SetToType<DbDataReader, Binary, string>("VARCHAR", (r, i) => new Binary(r.GetFieldValue<byte[]>(i)));

			_sqlOptimizer = Version >= FirebirdVersion.v3
				? new Firebird3SqlOptimizer(SqlProviderFlags)
				: new FirebirdSqlOptimizer(SqlProviderFlags);
		}

		static DateTime GetDateTime(DateTime value)
		{
			if (value.Year == 1970 && value.Month == 1 && value.Day == 1)
				return new DateTime(1, 1, 1, value.Hour, value.Minute, value.Second, value.Millisecond);

			return value;
		}

		public FirebirdVersion Version { get; }

		public override TableOptions SupportedTableOptions =>
			TableOptions.IsTemporary                |
			TableOptions.IsGlobalTemporaryStructure |
			TableOptions.IsLocalTemporaryData       |
			TableOptions.IsTransactionTemporaryData |
			TableOptions.CreateIfNotExists          |
			TableOptions.DropIfExists;

		protected override IMemberTranslator CreateMemberTranslator()
		{
			return Version == FirebirdVersion.v5 ? new Firebird5MemberTranslator() : new FirebirdMemberTranslator();
		}

		protected override IIdentifierService CreateIdentifierService()
		{
			return new IdentifierServiceSimple(Version <= FirebirdVersion.v3 ? 31 : 63);
		}

		public override ISqlBuilder CreateSqlBuilder(MappingSchema mappingSchema, DataOptions dataOptions)
		{
			if (Version == FirebirdVersion.v3)
				return new Firebird3SqlBuilder(this, mappingSchema, dataOptions, GetSqlOptimizer(dataOptions), SqlProviderFlags);

			if (Version >= FirebirdVersion.v4)
				return new Firebird4SqlBuilder(this, mappingSchema, dataOptions, GetSqlOptimizer(dataOptions), SqlProviderFlags);

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

		#region BulkCopy

		public override BulkCopyRowsCopied BulkCopy<T>(DataOptions options, ITable<T> table, IEnumerable<T> source)
		{
			return new FirebirdBulkCopy(this).BulkCopy(
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
			return new FirebirdBulkCopy(this).BulkCopyAsync(
				options.BulkCopyOptions.BulkCopyType == BulkCopyType.Default ?
					options.FindOrDefault(FirebirdOptions.Default).BulkCopyType :
					options.BulkCopyOptions.BulkCopyType,
				table,
				options,
				source,
				cancellationToken);
		}

		public override Task<BulkCopyRowsCopied> BulkCopyAsync<T>(DataOptions options, ITable<T> table,
			IAsyncEnumerable<T> source, CancellationToken cancellationToken)
		{
			return new FirebirdBulkCopy(this).BulkCopyAsync(
				options.BulkCopyOptions.BulkCopyType == BulkCopyType.Default ?
					options.FindOrDefault(FirebirdOptions.Default).BulkCopyType :
					options.BulkCopyOptions.BulkCopyType,
				table,
				options,
				source,
				cancellationToken);
		}

		#endregion

		static MappingSchema GetMappingSchema(FirebirdVersion version)
		{
			return version switch
			{
				FirebirdVersion.v25 => new FirebirdMappingSchema.Firebird25MappingSchema(),
				FirebirdVersion.v3  => new FirebirdMappingSchema.Firebird3MappingSchema (),
				FirebirdVersion.v4  => new FirebirdMappingSchema.Firebird4MappingSchema (),
				FirebirdVersion.v5  => new FirebirdMappingSchema.Firebird5MappingSchema (),
				_                   => new FirebirdMappingSchema.Firebird25MappingSchema(),
			};
		}
	}
}
