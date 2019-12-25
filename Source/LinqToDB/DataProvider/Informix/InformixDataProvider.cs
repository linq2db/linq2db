using System;
using System.Collections.Generic;
using System.Data;

namespace LinqToDB.DataProvider.Informix
{
	using Common;
	using Data;
	using Mapping;
	using SqlProvider;

	public class InformixDataProvider : DynamicDataProviderBase
	{
		public InformixDataProvider(string providerName)
			: base(providerName, null!)
		{
			Wrapper = new Lazy<InformixWrappers.IInformixWrapper>(() => Initialize(), true);

			// TODO: is informix IDS provider also order-dependent?
			SqlProviderFlags.IsParameterOrderDependent         = !Wrapper.Value.IsIDSProvider;
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
		}

		internal readonly Lazy<InformixWrappers.IInformixWrapper> Wrapper;

		private InformixWrappers.IInformixWrapper Initialize()
		{
			var wrapper = InformixWrappers.Initialize(this);

			// present only in SQLI provider
			SetField(typeof(long), "BIGINT", "GetBigInt", false, dataReaderType: wrapper.DataReaderType);

			if (Name == ProviderName.Informix)
											  SetProviderField(wrapper.DecimalType!,  typeof(decimal),                                 "GetIfxDecimal",                     dataReaderType: wrapper.DataReaderType);
			if (wrapper.DateTimeType != null) SetProviderField(wrapper.DateTimeType, typeof(DateTime), Name == ProviderName.Informix ? "GetIfxDateTime" : "GetDB2DateTime", dataReaderType: wrapper.DataReaderType);
			if (wrapper.TimeSpanType != null) SetProviderField(wrapper.TimeSpanType, typeof(TimeSpan), Name == ProviderName.Informix ? "GetIfxTimeSpan" : "GetDB2TimeSpan", dataReaderType: wrapper.DataReaderType);

			return wrapper;
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

		protected override void OnConnectionTypeCreated(Type connectionType)
		{
		}

		public    override string ConnectionNamespace => Name == ProviderName.Informix
			? "IBM.Data.Informix"
#if NET45 || NET46
			: "IBM.Data.DB2";
#else
			: "IBM.Data.DB2.Core";
#endif
		protected override string ConnectionTypeName => Name == ProviderName.Informix
			? $"{ConnectionNamespace}.IfxConnection, {ConnectionNamespace}"
			: $"{ConnectionNamespace}.DB2Connection, {ConnectionNamespace}";
		protected override string DataReaderTypeName => Name == ProviderName.Informix
			? $"{ConnectionNamespace}.IfxDataReader, {ConnectionNamespace}"
			: $"{ConnectionNamespace}.DB2DataReader, {ConnectionNamespace}";

#if !NETSTANDARD2_0 && !NETCOREAPP2_1
		public override string DbFactoryProviderName => Name == ProviderName.Informix
			? "IBM.Data.Informix" : "IBM.Data.DB2";
#endif

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
			return new InformixSchemaProvider();
		}

		public override void SetParameter(DataConnection dataConnection, IDbDataParameter parameter, string name, DbDataType dataType, object? value)
		{
			if (value is TimeSpan ts)
			{
				// TODO: we should reverse Int64 check somehow, as now it pollutes multiple places
				// and will not work with other not-interval mappings
				if (Wrapper.Value.TimeSpanFactory != null && dataType.DataType != DataType.Int64)
					value = Wrapper.Value.TimeSpanFactory(ts);
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

			InformixWrappers.IfxType? idsType = null;
			DB2.DB2Wrappers.DB2Type?  db2Type = null;

			switch (dataType.DataType)
			{
				case DataType.Text      :
				case DataType.NText     :
					idsType = InformixWrappers.IfxType.Clob;
					db2Type = DB2.DB2Wrappers .DB2Type.Clob;
					break;
			}

			if (idsType != null && db2Type != null)
			{
				var param = TryConvertParameter(Wrapper.Value.ParameterType, parameter, dataConnection.MappingSchema);
				if (param != null)
				{
					if (Wrapper.Value.IfxTypeSetter != null)
						Wrapper.Value.IfxTypeSetter(param, idsType.Value);
					else
						Wrapper.Value.DB2TypeSetter!(param, db2Type.Value);
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
			public static readonly InformixMappingSchema.IDSMappingSchema IDSMappingSchema = new InformixMappingSchema.IDSMappingSchema();
			public static readonly InformixMappingSchema.DB2MappingSchema DB2MappingSchema = new InformixMappingSchema.DB2MappingSchema();
		}

		public override MappingSchema MappingSchema => Name == ProviderName.Informix
			? MappingSchemaInstance.IDSMappingSchema as MappingSchema
			: MappingSchemaInstance.DB2MappingSchema;


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
