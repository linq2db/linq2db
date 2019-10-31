#nullable disable
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Security;

namespace LinqToDB.DataProvider.Informix
{
	using Common;
	using Data;
	using Mapping;
	using SqlProvider;

	public class InformixDataProvider : DynamicDataProviderBase
	{
		public InformixDataProvider()
			: this(ProviderName.Informix, new InformixMappingSchema())
		{
		}

		protected InformixDataProvider(string name, MappingSchema mappingSchema)
			: base(name, mappingSchema)
		{
			SqlProviderFlags.IsParameterOrderDependent         = true;
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

			if (!Configuration.AvoidSpecificDataProviderAPI)
			{
				SetProviderField<IDataReader,float,  float  >((r,i) => GetFloat  (r, i));
				SetProviderField<IDataReader,double, double >((r,i) => GetDouble (r, i));
				SetProviderField<IDataReader,decimal,decimal>((r,i) => GetDecimal(r, i));

				SetField<IDataReader, float  >((r, i) => GetFloat  (r, i));
				SetField<IDataReader, double >((r, i) => GetDouble (r, i));
				SetField<IDataReader, decimal>((r, i) => GetDecimal(r, i));
			}

			_sqlOptimizer = new InformixSqlOptimizer(SqlProviderFlags);
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

		Type _ifxBlob;
		Type _ifxClob;
		Type _ifxDecimal;
		Type _ifxDateTime;
		Type _ifxTimeSpan;

		public override IDisposable ExecuteScope(DataConnection dataConnection)
		{
			return new InvariantCultureRegion();
		}

		protected override void OnConnectionTypeCreated(Type connectionType)
		{
			_ifxBlob     = connectionType.Assembly.GetType(InformixTools.IsCore ? "IBM.Data.DB2Types.DB2Blob"     : "IBM.Data.Informix.IfxBlob", true);
			_ifxClob     = connectionType.Assembly.GetType(InformixTools.IsCore ? "IBM.Data.DB2Types.DB2Clob"     : "IBM.Data.Informix.IfxClob", true);
			_ifxDecimal  = connectionType.Assembly.GetType(InformixTools.IsCore ? "IBM.Data.DB2Types.DB2Decimal"  : "IBM.Data.Informix.IfxDecimal", true);
			_ifxDateTime = connectionType.Assembly.GetType(InformixTools.IsCore ? "IBM.Data.DB2Types.DB2DateTime" : "IBM.Data.Informix.IfxDateTime", true);
			// type not implemented by core provider (but exists for source compatibility and to punish those who don't use tests)
			_ifxTimeSpan = InformixTools.IsCore ? null : connectionType.Assembly.GetType("IBM.Data.Informix.IfxTimeSpan", true);

			if (!Configuration.AvoidSpecificDataProviderAPI)
			{
				SetField(typeof(Int64), "BIGINT", "GetBigInt", false);

				SetProviderField(_ifxDecimal , typeof(decimal) , InformixTools.IsCore ? "GetDB2Decimal"  : "GetIfxDecimal");
				SetProviderField(_ifxDateTime, typeof(DateTime), InformixTools.IsCore ? "GetDB2DateTime" : "GetIfxDateTime");
				if (_ifxTimeSpan != null)
					SetProviderField(_ifxTimeSpan, typeof(TimeSpan), InformixTools.IsCore ? "GetDB2TimeSpan" : "GetIfxTimeSpan", false);
			}

			var p = Expression.Parameter(typeof(TimeSpan));
			if (_ifxTimeSpan != null)
			{
				_newIfxTimeSpan = Expression.Lambda<Func<TimeSpan, object>>(
					Expression.Convert(
						Expression.New(_ifxTimeSpan.GetConstructor(new[] { typeof(TimeSpan) }), p),
						typeof(object)),
					p).Compile();
			}

			_setText = GetSetParameter(
				connectionType,
				InformixTools.IsCore ? "DB2Parameter" : "IfxParameter",
				InformixTools.IsCore ? "DB2Type"      : "IfxType",
				InformixTools.IsCore ? "DB2Type"      : "IfxType",
				"Clob");

			MappingSchema.AddScalarType(_ifxBlob,     GetNullValue(_ifxBlob),     true, DataType.VarBinary);
			MappingSchema.AddScalarType(_ifxClob,     GetNullValue(_ifxClob),     true, DataType.Text);
			MappingSchema.AddScalarType(_ifxDateTime, GetNullValue(_ifxDateTime), true, DataType.DateTime2);
			MappingSchema.AddScalarType(_ifxDecimal,  GetNullValue(_ifxDecimal),  true, DataType.Decimal);
			if (_ifxTimeSpan != null)
				MappingSchema.AddScalarType(_ifxTimeSpan, GetNullValue(_ifxTimeSpan), true, DataType.Time);
			//AddScalarType(typeof(IfxMonthSpan),   IfxMonthSpan.  Null, DataType.Time);
		}

		static object GetNullValue(Type type)
		{
			try
			{
				var getValue = Expression.Lambda<Func<object>>(Expression.Convert(Expression.Field(null, type, "Null"), typeof(object)));
				return getValue.Compile()();
			}
			catch (SecurityException)
			{
				return null;
			}
		}

		public    override string ConnectionNamespace => InformixTools.IsCore ? "IBM.Data.DB2.Core"                                  : "IBM.Data.Informix";
		protected override string ConnectionTypeName  => InformixTools.IsCore ? "IBM.Data.DB2.Core.DB2Connection, IBM.Data.DB2.Core" : "IBM.Data.Informix.IfxConnection, IBM.Data.Informix";
		protected override string DataReaderTypeName  => InformixTools.IsCore ? "IBM.Data.DB2.Core.DB2DataReader, IBM.Data.DB2.Core" : "IBM.Data.Informix.IfxDataReader, IBM.Data.Informix";

#if !NETSTANDARD2_0 && !NETCOREAPP2_1
		public override string DbFactoryProviderName => "IBM.Data.Informix";
#endif

		public override ISqlBuilder CreateSqlBuilder(MappingSchema mappingSchema)
		{
			return new InformixSqlBuilder(GetSqlOptimizer(), SqlProviderFlags, mappingSchema.ValueToSqlConverter);
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

		Func<TimeSpan,object> _newIfxTimeSpan;

		public override void SetParameter(IDbDataParameter parameter, string name, DbDataType dataType, object value)
		{
			if (value is TimeSpan ts && _newIfxTimeSpan != null)
			{
				if (dataType.DataType != DataType.Int64)
					value = _newIfxTimeSpan(ts);
			}
			else if (value is Guid || value == null && dataType.DataType == DataType.Guid)
			{
				value    = value?.ToString();
				dataType = dataType.WithDataType(DataType.Char);
			}
			else if (value is bool b)
			{
				value = b ? 't' : 'f';
				dataType = dataType.WithDataType(DataType.Char);
			}

			base.SetParameter(parameter, name, dataType, value);
		}

		Action<IDbDataParameter> _setText;

		protected override void SetParameterType(IDbDataParameter parameter, DbDataType dataType)
		{
			switch (dataType.DataType)
			{
				case DataType.UInt16    : dataType = dataType.WithDataType(DataType.Int32);    break;
				case DataType.UInt32    : dataType = dataType.WithDataType(DataType.Int64);    break;
				case DataType.UInt64    : dataType = dataType.WithDataType(DataType.Decimal);  break;
				case DataType.VarNumeric: dataType = dataType.WithDataType(DataType.Decimal);  break;
				case DataType.DateTime2 : dataType = dataType.WithDataType(DataType.DateTime); break;
				case DataType.Text      : _setText(parameter); return;
				case DataType.NText     : _setText(parameter); return;
			}

			base.SetParameterType(parameter, dataType);
		}

		#region BulkCopy

		public override BulkCopyRowsCopied BulkCopy<T>(
			[JetBrains.Annotations.NotNull] ITable<T> table, BulkCopyOptions options, IEnumerable<T> source)
		{
			return new InformixBulkCopy().BulkCopy(
				options.BulkCopyType == BulkCopyType.Default ? InformixTools.DefaultBulkCopyType : options.BulkCopyType,
				table,
				options,
				source);
		}

		#endregion
	}
}
