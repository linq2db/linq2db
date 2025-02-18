using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.DataProvider.Informix.Translation;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Linq.Internal;
using LinqToDB.Linq.Translation;
using LinqToDB.Mapping;

namespace LinqToDB.DataProvider.Informix
{
	sealed class InformixDataProviderInformix : InformixDataProvider { public InformixDataProviderInformix() : base(ProviderName.Informix,    InformixProvider.Informix) {} }
	sealed class InformixDataProviderDB2      : InformixDataProvider { public InformixDataProviderDB2()      : base(ProviderName.InformixDB2, InformixProvider.DB2     ) {} }

	public abstract class InformixDataProvider : DynamicDataProviderBase<InformixProviderAdapter>
	{
		protected InformixDataProvider(string name, InformixProvider provider)
			: base(name, GetMappingSchema(provider), InformixProviderAdapter.GetInstance(provider))
		{
			SqlProviderFlags.IsParameterOrderDependent                 = !Adapter.IsIDSProvider;
			SqlProviderFlags.IsSubQueryTakeSupported                   = false;
			SqlProviderFlags.IsInsertOrUpdateSupported                 = false;
			SqlProviderFlags.IsCommonTableExpressionsSupported         = true;
			SqlProviderFlags.IsUpdateFromSupported                     = false;
			SqlProviderFlags.RowConstructorSupport                     = RowFeature.Equality | RowFeature.In;
			SqlProviderFlags.IsExistsPreferableForContains             = true;
			SqlProviderFlags.IsCorrelatedSubQueryTakeSupported         = false;

			SetCharField("CHAR",  (r,i) => r.GetString(i).TrimEnd(' '));
			SetCharField("NCHAR", (r,i) => r.GetString(i).TrimEnd(' '));
			SetCharFieldToType<char>("CHAR",  DataTools.GetCharExpression);
			SetCharFieldToType<char>("NCHAR", DataTools.GetCharExpression);

			SetProviderField<DbDataReader, float,  float  >((r,i) => GetFloat  (r, i));
			SetProviderField<DbDataReader, double, double >((r,i) => GetDouble (r, i));
			SetProviderField<DbDataReader, decimal,decimal>((r,i) => GetDecimal(r, i));

			SetField<DbDataReader, float  >((r, i) => GetFloat  (r, i));
			SetField<DbDataReader, double >((r, i) => GetDouble (r, i));
			SetField<DbDataReader, decimal>((r, i) => GetDecimal(r, i));

			_sqlOptimizer = new InformixSqlOptimizer(SqlProviderFlags);

			if (Adapter.GetBigIntReaderMethod != null)
				SetField(typeof(long), "BIGINT", Adapter.GetBigIntReaderMethod, false, dataReaderType: Adapter.DataReaderType);

			if (Name == ProviderName.Informix && Adapter.DecimalType != null)
											  SetProviderField(Adapter.DecimalType , typeof(decimal) , Adapter.GetDecimalReaderMethod!, dataReaderType: Adapter.DataReaderType);
			if (Adapter.DateTimeType != null) SetProviderField(Adapter.DateTimeType, typeof(DateTime), Adapter.GetDateTimeReaderMethod, dataReaderType: Adapter.DataReaderType);
			if (Adapter.TimeSpanType != null) SetProviderField(Adapter.TimeSpanType, typeof(TimeSpan), Adapter.GetTimeSpanReaderMethod, dataReaderType: Adapter.DataReaderType);
		}

		protected override IMemberTranslator CreateMemberTranslator()
		{
			return new InformixMemberTranslator();
		}

		[ColumnReader(1)]
		static float GetFloat(DbDataReader dr, int idx)
		{
			using (new InvariantCultureRegion(null))
				return dr.GetFloat(idx);
		}

		[ColumnReader(1)]
		static double GetDouble(DbDataReader dr, int idx)
		{
			using (new InvariantCultureRegion(null))
				return dr.GetDouble(idx);
		}

		[ColumnReader(1)]
		static decimal GetDecimal(DbDataReader dr, int idx)
		{
			using (new InvariantCultureRegion(null))
				return dr.GetDecimal(idx);
		}

		public override IExecutionScope ExecuteScope(DataConnection dataConnection) => new InvariantCultureRegion(null);

		public override TableOptions SupportedTableOptions =>
			TableOptions.IsTemporary               |
			TableOptions.IsLocalTemporaryStructure |
			TableOptions.IsLocalTemporaryData      |
			TableOptions.CreateIfNotExists         |
			TableOptions.DropIfExists;

		public override ISqlBuilder CreateSqlBuilder(MappingSchema mappingSchema, DataOptions dataOptions)
		{
			return new InformixSqlBuilder(this, mappingSchema, dataOptions, GetSqlOptimizer(dataOptions), SqlProviderFlags);
		}

		readonly ISqlOptimizer _sqlOptimizer;

		public override ISqlOptimizer GetSqlOptimizer(DataOptions dataOptions)
		{
			return _sqlOptimizer;
		}

		public override SchemaProvider.ISchemaProvider GetSchemaProvider()
		{
			return new InformixSchemaProvider(this);
		}

		public override void SetParameter(DataConnection dataConnection, DbParameter parameter, string name, DbDataType dataType, object? value)
		{
			if (value is TimeSpan ts)
			{
				// TODO: we should reverse Int64 check somehow, as now it pollutes multiple places
				// and will not work with other not-interval mappings
				if (Adapter.TimeSpanFactory != null && dataType.DataType != DataType.Int64)
					value = Adapter.TimeSpanFactory(ts);
			}
			else if (value is Guid || value == null && dataType.DataType == DataType.Guid)
			{
				value    = value == null ? null : string.Format(CultureInfo.InvariantCulture, "{0}", value);
				dataType = dataType.WithDataType(DataType.Char);
			}
			else if (value is byte byteValue && dataType.DataType == DataType.Int16)
			{
				value = (short)byteValue;
			}
			else if (value is bool b)
			{
				// IDS provider needs short values for bulk copy, but chars still for regular SQL
				if (parameter is BulkCopyReader.Parameter)
				{
					value    = (short)(b ? 1 : 0);
					dataType = dataType.WithDataType(DataType.Int16);
				}
				else
				{
					value    = b ? 't' : 'f';
					dataType = dataType.WithDataType(DataType.Char);
				}
			}
#if NET6_0_OR_GREATER
			else if (value is DateOnly d)
			{
				value = d.ToDateTime(TimeOnly.MinValue);
			}
#endif

			base.SetParameter(dataConnection, parameter, name, dataType, value);
		}

		protected override void SetParameterType(DataConnection dataConnection, DbParameter parameter, DbDataType dataType)
		{
			if (parameter is BulkCopyReader.Parameter)
				return;

			InformixProviderAdapter.IfxType? idsType = null;
			DB2.DB2ProviderAdapter.DB2Type?  db2Type = null;

			switch (dataType.DataType)
			{
				case DataType.Text      :
				case DataType.NText     :
					idsType = InformixProviderAdapter.IfxType.Clob;
					db2Type = DB2.DB2ProviderAdapter .DB2Type.Clob;
					break;
			}

			if (idsType != null && db2Type != null)
			{
				var param = TryGetProviderParameter(dataConnection, parameter);
				if (param != null)
				{
					if (Adapter.SetIfxType != null)
						Adapter.SetIfxType(param, idsType.Value);
					else
						Adapter.SetDB2Type!(param, db2Type.Value);
					return;
				}
			}

			switch (dataType.DataType)
			{
				case DataType.UInt16    : dataType = dataType.WithDataType(DataType.Int32);    break;
				case DataType.UInt32    : dataType = dataType.WithDataType(DataType.Int64);    break;
				case DataType.UInt64    :
				case DataType.VarNumeric: dataType = dataType.WithDataType(DataType.Decimal);  break;
				case DataType.DateTime2 : dataType = dataType.WithDataType(DataType.DateTime); break;
				case DataType.Text      :
				case DataType.NText     : dataType = dataType.WithDataType(DataType.NVarChar); break;
			}

			base.SetParameterType(dataConnection, parameter, dataType);
		}

		static MappingSchema GetMappingSchema(InformixProvider provider)
		{
			return provider switch
			{
				InformixProvider.Informix => new InformixMappingSchema.IfxMappingSchema(),
				_                         => new InformixMappingSchema.DB2MappingSchema(),
			};
		}

		#region BulkCopy

		public override BulkCopyRowsCopied BulkCopy<T>(DataOptions options, ITable<T> table, IEnumerable<T> source)
		{
			return new InformixBulkCopy(this).BulkCopy(
				options.BulkCopyOptions.BulkCopyType == BulkCopyType.Default ?
					options.FindOrDefault(InformixOptions.Default).BulkCopyType :
					options.BulkCopyOptions.BulkCopyType,
				table,
				options,
				source);
		}

		public override Task<BulkCopyRowsCopied> BulkCopyAsync<T>(DataOptions options, ITable<T> table,
			IEnumerable<T> source, CancellationToken cancellationToken)
		{
			return new InformixBulkCopy(this).BulkCopyAsync(
				options.BulkCopyOptions.BulkCopyType == BulkCopyType.Default ?
					options.FindOrDefault(InformixOptions.Default).BulkCopyType :
					options.BulkCopyOptions.BulkCopyType,
				table,
				options,
				source,
				cancellationToken);
		}

		public override Task<BulkCopyRowsCopied> BulkCopyAsync<T>(DataOptions options, ITable<T> table,
			IAsyncEnumerable<T> source, CancellationToken cancellationToken)
		{
			return new InformixBulkCopy(this).BulkCopyAsync(
				options.BulkCopyOptions.BulkCopyType == BulkCopyType.Default ?
					options.FindOrDefault(InformixOptions.Default).BulkCopyType :
					options.BulkCopyOptions.BulkCopyType,
				table,
				options,
				source,
				cancellationToken);
		}

		#endregion
	}
}
