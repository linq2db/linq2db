using System;
using System.Collections.Generic;
using System.Data;

namespace LinqToDB.DataProvider.Informix
{
	using Common;
	using Data;
	using Mapping;
	using SqlProvider;

	public class InformixDataProvider : DynamicDataProviderBase<InformixProviderAdapter>
	{
		public InformixDataProvider(string providerName)
						: base(
				  providerName,
				  GetMappingSchema(providerName, InformixProviderAdapter.GetInstance(providerName).MappingSchema),
				  InformixProviderAdapter.GetInstance(providerName))

		{
			SqlProviderFlags.IsParameterOrderDependent         = !Adapter.IsIDSProvider;
			SqlProviderFlags.IsSubQueryTakeSupported           = false;
			SqlProviderFlags.IsInsertOrUpdateSupported         = false;
			SqlProviderFlags.IsGroupByExpressionSupported      = false;
			SqlProviderFlags.IsCrossJoinSupported              = false;
			SqlProviderFlags.IsCommonTableExpressionsSupported = true;
			SqlProviderFlags.IsSubQueryOrderBySupported        = true;
			SqlProviderFlags.IsDistinctOrderBySupported        = false;
			SqlProviderFlags.IsUpdateFromSupported             = false;

			SetCharField("CHAR",  (r,i) => r.GetString(i).TrimEnd(' '));
			SetCharField("NCHAR", (r,i) => r.GetString(i).TrimEnd(' '));
			SetCharFieldToType<char>("CHAR",  (r, i) => DataTools.GetChar(r, i));
			SetCharFieldToType<char>("NCHAR", (r, i) => DataTools.GetChar(r, i));

			SetProviderField<IDataReader,float,  float  >((r,i) => GetFloat  (r, i));
			SetProviderField<IDataReader,double, double >((r,i) => GetDouble (r, i));
			SetProviderField<IDataReader,decimal,decimal>((r,i) => GetDecimal(r, i));

			SetField<IDataReader, float  >((r, i) => GetFloat  (r, i));
			SetField<IDataReader, double >((r, i) => GetDouble (r, i));
			SetField<IDataReader, decimal>((r, i) => GetDecimal(r, i));

			_sqlOptimizer = new InformixSqlOptimizer(SqlProviderFlags);

			if (Adapter.GetBigIntReaderMethod != null)
				SetField(typeof(long), "BIGINT", Adapter.GetBigIntReaderMethod, false, dataReaderType: Adapter.DataReaderType);

			if (Name == ProviderName.Informix && Adapter.DecimalType != null)
											  SetProviderField(Adapter.DecimalType , typeof(decimal) , Adapter.GetDecimalReaderMethod!, dataReaderType: Adapter.DataReaderType);
			if (Adapter.DateTimeType != null) SetProviderField(Adapter.DateTimeType, typeof(DateTime), Adapter.GetDateTimeReaderMethod, dataReaderType: Adapter.DataReaderType);
			if (Adapter.TimeSpanType != null) SetProviderField(Adapter.TimeSpanType, typeof(TimeSpan), Adapter.GetTimeSpanReaderMethod, dataReaderType: Adapter.DataReaderType);
		}

		static float GetFloat(IDataReader dr, int idx)
		{
			using (new InvariantCultureRegion())
				return dr.GetFloat(idx);
		}

		static double GetDouble(IDataReader dr, int idx)
		{
			using (new InvariantCultureRegion())
				return dr.GetDouble(idx);
		}

		static decimal GetDecimal(IDataReader dr, int idx)
		{
			using (new InvariantCultureRegion())
				return dr.GetDecimal(idx);
		}

		public override IDisposable ExecuteScope(DataConnection dataConnection)
		{
			return new InvariantCultureRegion();
		}

		public override ISqlBuilder CreateSqlBuilder(MappingSchema mappingSchema)
		{
			return new InformixSqlBuilder(this, mappingSchema, GetSqlOptimizer(), SqlProviderFlags);
		}

		readonly ISqlOptimizer _sqlOptimizer;

		public override ISqlOptimizer GetSqlOptimizer()
		{
			return _sqlOptimizer;
		}

		public override SchemaProvider.ISchemaProvider GetSchemaProvider()
		{
			return new InformixSchemaProvider(this);
		}

		public override void SetParameter(DataConnection dataConnection, IDbDataParameter parameter, string name, DbDataType dataType, object? value)
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
				value    = value?.ToString();
				dataType = dataType.WithDataType(DataType.Char);
			}
			else if (value is bool b)
			{
				// IDS provider needs short values for bulk copy, but chars still for regular SQL
				if (parameter is BulkCopyReader.Parameter)
				{
					value    = (short)(b == true ? 1 : 0);
					dataType = dataType.WithDataType(DataType.Int16);
				}
				else
				{
					value    = b ? 't' : 'f';
					dataType = dataType.WithDataType(DataType.Char);
				}
			}

			base.SetParameter(dataConnection, parameter, name, dataType, value);
		}

		protected override void SetParameterType(DataConnection dataConnection, IDbDataParameter parameter, DbDataType dataType)
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
				var param = TryGetProviderParameter(parameter, dataConnection.MappingSchema);
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

		static class MappingSchemaInstance
		{
			public static readonly MappingSchema IfxMappingSchema = new InformixMappingSchema.IfxMappingSchema();
			public static readonly MappingSchema DB2MappingSchema = new InformixMappingSchema.DB2MappingSchema();

			public static MappingSchema Get(string providerName, MappingSchema providerSchema)
			{
				switch (providerName)
				{
					default:
					case ProviderName.Informix   : return new MappingSchema(IfxMappingSchema, providerSchema);
					case ProviderName.InformixDB2: return new MappingSchema(DB2MappingSchema, providerSchema);
				}
			}
		}

		private static MappingSchema GetMappingSchema(string name, MappingSchema providerSchema)
		{
			switch (name)
			{
				case ProviderName.Informix   : return new InformixMappingSchema.IfxMappingSchema(providerSchema);
				default                      :
				case ProviderName.InformixDB2: return new InformixMappingSchema.DB2MappingSchema(providerSchema);
			}
		}

		#region BulkCopy

		public override BulkCopyRowsCopied BulkCopy<T>(
			ITable<T> table, BulkCopyOptions options, IEnumerable<T> source)
		{
			return new InformixBulkCopy(this).BulkCopy(
				options.BulkCopyType == BulkCopyType.Default ? InformixTools.DefaultBulkCopyType : options.BulkCopyType,
				table,
				options,
				source);
		}

		#endregion
	}
}
