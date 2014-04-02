using System;
using System.Data;
using System.Linq.Expressions;

namespace LinqToDB.DataProvider.PostgreSQL
{
	using Expressions;
	using Mapping;
	using SqlProvider;

	public class PostgreSQLDataProvider : DynamicDataProviderBase
	{
		public PostgreSQLDataProvider()
			: this(ProviderName.PostgreSQL, new PostgreSQLMappingSchema())
		{
		}

		protected PostgreSQLDataProvider(string name, MappingSchema mappingSchema)
			: base(name, mappingSchema)
		{
			SqlProviderFlags.IsInsertOrUpdateSupported      = false;
			SqlProviderFlags.IsUpdateSetTableAliasSupported = false;

			SetCharField("bpchar", (r,i) => r.GetString(i).TrimEnd());

			_sqlOptimizer = new PostgreSQLSqlOptimizer(SqlProviderFlags);
		}

		internal Type BitStringType;
		internal Type NpgsqlIntervalType;
		internal Type NpgsqlInetType;
		internal Type NpgsqlTimeType;
		internal Type NpgsqlTimeTZType;
		internal Type NpgsqlPointType;
		internal Type NpgsqlLSegType;
		internal Type NpgsqlBoxType;
		internal Type NpgsqlPathType;
		internal Type NpgsqlPolygonType;
		internal Type NpgsqlCircleType;
		internal Type NpgsqlMacAddressType;

		Type _npgsqlTimeStamp;
		Type _npgsqlTimeStampTZ;
		Type _npgsqlDate;

		protected override void OnConnectionTypeCreated(Type connectionType)
		{
			BitStringType      = connectionType.Assembly.GetType("NpgsqlTypes.BitString",         true);
			NpgsqlIntervalType = connectionType.Assembly.GetType("NpgsqlTypes.NpgsqlInterval",    true);
			NpgsqlInetType     = connectionType.Assembly.GetType("NpgsqlTypes.NpgsqlInet",        true);
			NpgsqlTimeType     = connectionType.Assembly.GetType("NpgsqlTypes.NpgsqlTime",        true);
			NpgsqlTimeTZType   = connectionType.Assembly.GetType("NpgsqlTypes.NpgsqlTimeTZ",      true);
			NpgsqlPointType    = connectionType.Assembly.GetType("NpgsqlTypes.NpgsqlPoint",       true);
			NpgsqlLSegType     = connectionType.Assembly.GetType("NpgsqlTypes.NpgsqlLSeg",        true);
			NpgsqlBoxType      = connectionType.Assembly.GetType("NpgsqlTypes.NpgsqlBox",         true);
			NpgsqlPathType     = connectionType.Assembly.GetType("NpgsqlTypes.NpgsqlPath",        true);
			_npgsqlTimeStamp   = connectionType.Assembly.GetType("NpgsqlTypes.NpgsqlTimeStamp",   true);
			_npgsqlTimeStampTZ = connectionType.Assembly.GetType("NpgsqlTypes.NpgsqlTimeStampTZ", true);
			_npgsqlDate        = connectionType.Assembly.GetType("NpgsqlTypes.NpgsqlDate",        true);
			NpgsqlMacAddressType  = connectionType.Assembly.GetType("NpgsqlTypes.NpgsqlMacAddress",  true);
			NpgsqlCircleType      = connectionType.Assembly.GetType("NpgsqlTypes.NpgsqlCircle",      true);
			NpgsqlPolygonType     = connectionType.Assembly.GetType("NpgsqlTypes.NpgsqlPolygon",     true);

			SetProviderField(BitStringType,        BitStringType,        "GetBitString");
			SetProviderField(NpgsqlIntervalType,   NpgsqlIntervalType,   "GetInterval");
			SetProviderField(NpgsqlInetType,       NpgsqlInetType,       "GetProviderSpecificValue");
			SetProviderField(NpgsqlTimeType,       NpgsqlTimeType,       "GetTime");
			SetProviderField(NpgsqlTimeTZType,     NpgsqlTimeTZType,     "GetTimeTZ");
			SetProviderField(_npgsqlTimeStamp,     _npgsqlTimeStamp,     "GetTimeStamp");
			SetProviderField(_npgsqlTimeStampTZ,   _npgsqlTimeStampTZ,   "GetTimeStampTZ");
			SetProviderField(_npgsqlDate,          _npgsqlDate,          "GetDate");
			SetProviderField(NpgsqlMacAddressType, NpgsqlMacAddressType, "GetProviderSpecificValue");

			{
				// SetProviderField2<NpgsqlDataReader,DateTimeOffset,NpgsqlTimeStampTZ>((r,i) => (NpgsqlTimeStampTZ)r.GetProviderSpecificValue(i));

				var dataReaderParameter = Expression.Parameter(DataReaderType, "r");
				var indexParameter      = Expression.Parameter(typeof(int),    "i");

				ReaderExpressions[new ReaderInfo { ToType = typeof(DateTimeOffset), ProviderFieldType = _npgsqlTimeStampTZ }] =
					Expression.Lambda(
						Expression.Convert(
							Expression.Call(dataReaderParameter, "GetProviderSpecificValue", null, indexParameter),
							_npgsqlTimeStampTZ),
						dataReaderParameter,
						indexParameter);
			}

			_setVarBinary = GetSetParameter(connectionType, "NpgsqlParameter", "NpgsqlDbType", "NpgsqlTypes.NpgsqlDbType", "Bytea");
			_setBoolean   = GetSetParameter(connectionType, "NpgsqlParameter", "NpgsqlDbType", "NpgsqlTypes.NpgsqlDbType", "Boolean");
			_setXml       = GetSetParameter(connectionType, "NpgsqlParameter", "NpgsqlDbType", "NpgsqlTypes.NpgsqlDbType", "Xml");
			_setText      = GetSetParameter(connectionType, "NpgsqlParameter", "NpgsqlDbType", "NpgsqlTypes.NpgsqlDbType", "Text");

			MappingSchema.AddScalarType(BitStringType);
			MappingSchema.AddScalarType(NpgsqlInetType);
			MappingSchema.AddScalarType(NpgsqlIntervalType);
			MappingSchema.AddScalarType(NpgsqlPointType);
			MappingSchema.AddScalarType(NpgsqlTimeType);
			MappingSchema.AddScalarType(NpgsqlTimeTZType);
			MappingSchema.AddScalarType(NpgsqlLSegType);
			MappingSchema.AddScalarType(NpgsqlBoxType);
			MappingSchema.AddScalarType(NpgsqlPathType);
			MappingSchema.AddScalarType(NpgsqlCircleType);
			MappingSchema.AddScalarType(_npgsqlDate);
			MappingSchema.AddScalarType(NpgsqlMacAddressType);
			MappingSchema.AddScalarType(NpgsqlPolygonType);
			MappingSchema.AddScalarType(_npgsqlTimeStamp);
			MappingSchema.AddScalarType(_npgsqlTimeStampTZ);

			{
				// SetConvertExpression<NpgsqlTimeStampTZ,DateTimeOffset>(
				//     d => new DateTimeOffset(d.Year, d.Month, d.Day, d.Hours, d.Minutes, d.Seconds, d.Milliseconds,
				//         new TimeSpan(d.TimeZone.Hours, d.TimeZone.Minutes, d.TimeZone.Seconds)));

				var p = Expression.Parameter(_npgsqlTimeStampTZ, "p");

				MappingSchema.SetConvertExpression(_npgsqlTimeStampTZ, typeof(DateTimeOffset),
					Expression.Lambda(
						Expression.New(
							MemberHelper.ConstructorOf(() => new DateTimeOffset(0, 0, 0, 0, 0, 0, 0, new TimeSpan())),
							Expression.PropertyOrField(p, "Year"),
							Expression.PropertyOrField(p, "Month"),
							Expression.PropertyOrField(p, "Day"),
							Expression.PropertyOrField(p, "Hours"),
							Expression.PropertyOrField(p, "Minutes"),
							Expression.PropertyOrField(p, "Seconds"),
							Expression.PropertyOrField(p, "Milliseconds"),
							Expression.New(
								MemberHelper.ConstructorOf(() => new TimeSpan(0, 0, 0)),
								Expression.PropertyOrField(Expression.PropertyOrField(p, "TimeZone"), "Hours"),
								Expression.PropertyOrField(Expression.PropertyOrField(p, "TimeZone"), "Minutes"),
								Expression.PropertyOrField(Expression.PropertyOrField(p, "TimeZone"), "Seconds"))),
						p
					));
			}
		}

		public    override string ConnectionNamespace { get { return "Npgsql";                          } }
		protected override string ConnectionTypeName  { get { return "Npgsql.NpgsqlConnection, Npgsql"; } }
		protected override string DataReaderTypeName  { get { return "Npgsql.NpgsqlDataReader, Npgsql"; } }

		public override ISqlBuilder CreateSqlBuilder()
		{
			return new PostgreSQLSqlBuilder(GetSqlOptimizer(), SqlProviderFlags);
		}

		readonly ISqlOptimizer _sqlOptimizer;

		public override ISqlOptimizer GetSqlOptimizer()
		{
			return _sqlOptimizer;
		}

		public override SchemaProvider.ISchemaProvider GetSchemaProvider()
		{
			return new PostgreSQLSchemaProvider();
		}

		static Action<IDbDataParameter> _setVarBinary;
		static Action<IDbDataParameter> _setBoolean;
		static Action<IDbDataParameter> _setXml;
		static Action<IDbDataParameter> _setText;

		protected override void SetParameterType(IDbDataParameter parameter, DataType dataType)
		{
			switch (dataType)
			{
				case DataType.SByte      : parameter.DbType = DbType.Int16;            break;
				case DataType.UInt16     : parameter.DbType = DbType.Int32;            break;
				case DataType.UInt32     : parameter.DbType = DbType.Int64;            break;
				case DataType.UInt64     : parameter.DbType = DbType.Decimal;          break;
				case DataType.DateTime2  : parameter.DbType = DbType.DateTime;         break;
				case DataType.VarNumeric : parameter.DbType = DbType.Decimal;          break;
				case DataType.Decimal    :
				case DataType.Money      : break;
				case DataType.Image      :
				case DataType.Binary     :
				case DataType.VarBinary  : _setVarBinary(parameter);                   break;
				case DataType.Boolean    : _setBoolean  (parameter);                   break;
				case DataType.Xml        : _setXml      (parameter);                   break;
				case DataType.Text       :
				case DataType.NText      : _setText     (parameter);                   break;
				default                  : base.SetParameterType(parameter, dataType); break;
			}
		}
	}
}
