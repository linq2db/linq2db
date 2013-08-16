using System;
using System.Data;
using System.Globalization;
using System.Linq.Expressions;
using System.Threading;

namespace LinqToDB.DataProvider.Informix
{
	using Common;
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
			SqlProviderFlags.IsParameterOrderDependent = true;
			SqlProviderFlags.IsSubQueryTakeSupported   = false;
			SqlProviderFlags.IsInsertOrUpdateSupported = false;

			SetCharField("CHAR",  (r,i) => r.GetString(i).TrimEnd());
			SetCharField("NCHAR", (r,i) => r.GetString(i).TrimEnd());

			if (!Configuration.AvoidSpecificDataProviderAPI)
			{
				SetProviderField<IDataReader,float,  float  >((r,i) => GetFloat  (r, i));
				SetProviderField<IDataReader,double, double >((r,i) => GetDouble (r, i));
				SetProviderField<IDataReader,decimal,decimal>((r,i) => GetDecimal(r, i));
			}
		}

		static float GetFloat(IDataReader dr, int idx)
		{
			var current = Thread.CurrentThread.CurrentCulture;

			if (Thread.CurrentThread.CurrentCulture != CultureInfo.InvariantCulture)
				Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

			var value = dr.GetFloat(idx);

			if (current != CultureInfo.InvariantCulture)
				Thread.CurrentThread.CurrentCulture = current;

			return value;
		}

		static double GetDouble(IDataReader dr, int idx)
		{
			var current = Thread.CurrentThread.CurrentCulture;

			if (Thread.CurrentThread.CurrentCulture != CultureInfo.InvariantCulture)
				Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

			var value = dr.GetDouble(idx);

			if (current != CultureInfo.InvariantCulture)
				Thread.CurrentThread.CurrentCulture = current;

			return value;
		}

		static decimal GetDecimal(IDataReader dr, int idx)
		{
			var current = Thread.CurrentThread.CurrentCulture;

			if (Thread.CurrentThread.CurrentCulture != CultureInfo.InvariantCulture)
				Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

			var value = dr.GetDecimal(idx);

			if (current != CultureInfo.InvariantCulture)
				Thread.CurrentThread.CurrentCulture = current;

			return value;
		}

		Type _ifxBlob;
		Type _ifxClob;
		Type _ifxDecimal;
		Type _ifxDateTime;
		Type _ifxTimeSpan;

		protected override void OnConnectionTypeCreated(Type connectionType)
		{
			_ifxBlob     = connectionType.Assembly.GetType("IBM.Data.Informix.IfxBlob",     true);
			_ifxClob     = connectionType.Assembly.GetType("IBM.Data.Informix.IfxClob",     true);
			_ifxDecimal  = connectionType.Assembly.GetType("IBM.Data.Informix.IfxDecimal",  true);
			_ifxDateTime = connectionType.Assembly.GetType("IBM.Data.Informix.IfxDateTime", true);
			_ifxTimeSpan = connectionType.Assembly.GetType("IBM.Data.Informix.IfxTimeSpan", true);

			if (!Configuration.AvoidSpecificDataProviderAPI)
			{
				SetField(typeof(Int64), "BIGINT", "GetBigInt");

				SetProviderField(_ifxDecimal,  typeof(decimal),  "GetIfxDecimal");
				SetProviderField(_ifxDateTime, typeof(DateTime), "GetIfxDateTime");
				SetProviderField(_ifxTimeSpan, typeof(TimeSpan), "GetIfxTimeSpan");
			}

			var p = Expression.Parameter(typeof(TimeSpan));

			_newIfxTimeSpan = Expression.Lambda<Func<TimeSpan,object>>(
				Expression.Convert(
					Expression.New(_ifxTimeSpan.GetConstructor(new[] { typeof(TimeSpan) }), p),
					typeof(object)),
				p).Compile();

			_setText = GetSetParameter(connectionType, "IfxParameter", "IfxType", "IfxType", "Clob");

			MappingSchema.AddScalarType(_ifxBlob,     GetNullValue(_ifxBlob),     true, DataType.VarBinary);
			MappingSchema.AddScalarType(_ifxClob,     GetNullValue(_ifxClob),     true, DataType.Text);
			MappingSchema.AddScalarType(_ifxDateTime, GetNullValue(_ifxDateTime), true, DataType.DateTime2);
			MappingSchema.AddScalarType(_ifxDecimal,  GetNullValue(_ifxDecimal),  true, DataType.Decimal);
			MappingSchema.AddScalarType(_ifxTimeSpan, GetNullValue(_ifxTimeSpan), true, DataType.Time);
			//AddScalarType(typeof(IfxMonthSpan),   IfxMonthSpan.  Null, DataType.Time);
		}

		static object GetNullValue(Type type)
		{
			var getValue = Expression.Lambda<Func<object>>(Expression.Convert(Expression.Field(null, type, "Null"), typeof(object)));
			return getValue.Compile()();
		}

		public    override string ConnectionNamespace { get { return "IBM.Data.Informix"; } }
		protected override string ConnectionTypeName  { get { return "IBM.Data.Informix.IfxConnection, IBM.Data.Informix"; } }
		protected override string DataReaderTypeName  { get { return "IBM.Data.Informix.IfxDataReader, IBM.Data.Informix"; } }
		
		public override ISqlBuilder CreateSqlBuilder()
		{
			return new InformixSqlBuilder(GetSqlOptimizer(), SqlProviderFlags);
		}

		readonly ISqlOptimizer _sqlOptimizer = new InformixSqlOptimizer();

		public override ISqlOptimizer GetSqlOptimizer()
		{
			return _sqlOptimizer;
		}

		public override SchemaProvider.ISchemaProvider GetSchemaProvider()
		{
			return new InformixSchemaProvider();
		}

		Func<TimeSpan,object> _newIfxTimeSpan;

		public override void SetParameter(IDbDataParameter parameter, string name, DataType dataType, object value)
		{
			if (value is TimeSpan)
				value = _newIfxTimeSpan((TimeSpan)value);
			else if (value is Guid)
			{
				value    = value.ToString();
				dataType = DataType.Char;
			}
			else if (value is bool)
			{
				value = (bool)value ? 't' : 'f';
				dataType = DataType.Char;
			}

			base.SetParameter(parameter, name, dataType, value);
		}

		static Action<IDbDataParameter> _setText;

		protected override void SetParameterType(IDbDataParameter parameter, DataType dataType)
		{
			switch (dataType)
			{
				case DataType.UInt16    : dataType = DataType.Int32;    break;
				case DataType.UInt32    : dataType = DataType.Int64;    break;
				case DataType.UInt64    : dataType = DataType.Decimal;  break;
				case DataType.VarNumeric: dataType = DataType.Decimal;  break;
				case DataType.DateTime2 : dataType = DataType.DateTime; break;
				case DataType.Text      : _setText(parameter); return;
				case DataType.NText     : _setText(parameter); return;
			}

			base.SetParameterType(parameter, dataType);
		}
	}
}
