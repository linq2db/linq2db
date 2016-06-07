using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Net;

namespace LinqToDB.DataProvider.PostgreSQL
{
	using Data;
	using Expressions;
	using Mapping;
	using SqlProvider;

	public class PostgreSQLDataProvider : DynamicDataProviderBase
	{
		public PostgreSQLDataProvider(PostgreSQLVersion version = PostgreSQLVersion.v92)
			: this(
				version == PostgreSQLVersion.v92 ? ProviderName.PostgreSQL92 : ProviderName.PostgreSQL93,
				new PostgreSQLMappingSchema(),
				version)
		{
		}

		public PostgreSQLDataProvider(string providerName, PostgreSQLVersion version)
			: this(providerName, new PostgreSQLMappingSchema(), version)
		{
		}

		protected PostgreSQLDataProvider(string name, MappingSchema mappingSchema, PostgreSQLVersion version = PostgreSQLVersion.v92)
			: base(name, mappingSchema)
		{
			Version = version;

			if (version == PostgreSQLVersion.v93)
				SqlProviderFlags.IsApplyJoinSupported = true;

			SqlProviderFlags.IsInsertOrUpdateSupported      = false;
			SqlProviderFlags.IsUpdateSetTableAliasSupported = false;

			SetCharField("bpchar", (r,i) => r.GetString(i).TrimEnd());

			_sqlOptimizer = new PostgreSQLSqlOptimizer(SqlProviderFlags);
		}

		public PostgreSQLVersion Version { get; private set; }

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
		Type _npgsqlDateTime;

		protected override void OnConnectionTypeCreated(Type connectionType)
		{
			BitStringType         = connectionType.Assembly.GetType("NpgsqlTypes.BitString",         false);
			NpgsqlIntervalType    = connectionType.Assembly.GetType("NpgsqlTypes.NpgsqlInterval",    false);
			NpgsqlInetType        = connectionType.Assembly.GetType("NpgsqlTypes.NpgsqlInet",        true);
			NpgsqlTimeType        = connectionType.Assembly.GetType("NpgsqlTypes.NpgsqlTime",        false);
			NpgsqlTimeTZType      = connectionType.Assembly.GetType("NpgsqlTypes.NpgsqlTimeTZ",      false);
			NpgsqlPointType       = connectionType.Assembly.GetType("NpgsqlTypes.NpgsqlPoint",       true);
			NpgsqlLSegType        = connectionType.Assembly.GetType("NpgsqlTypes.NpgsqlLSeg",        true);
			NpgsqlBoxType         = connectionType.Assembly.GetType("NpgsqlTypes.NpgsqlBox",         true);
			NpgsqlPathType        = connectionType.Assembly.GetType("NpgsqlTypes.NpgsqlPath",        true);
			_npgsqlTimeStamp      = connectionType.Assembly.GetType("NpgsqlTypes.NpgsqlTimeStamp",   false);
			_npgsqlTimeStampTZ    = connectionType.Assembly.GetType("NpgsqlTypes.NpgsqlTimeStampTZ", false);
			_npgsqlDate           = connectionType.Assembly.GetType("NpgsqlTypes.NpgsqlDate",        true);
			_npgsqlDateTime       = connectionType.Assembly.GetType("NpgsqlTypes.NpgsqlDateTime",    false);
			NpgsqlMacAddressType  = connectionType.Assembly.GetType("NpgsqlTypes.NpgsqlMacAddress",  false);
			NpgsqlCircleType      = connectionType.Assembly.GetType("NpgsqlTypes.NpgsqlCircle",      true);
			NpgsqlPolygonType     = connectionType.Assembly.GetType("NpgsqlTypes.NpgsqlPolygon",     true);

			if (BitStringType        != null) SetProviderField(BitStringType,        BitStringType,        "GetBitString");
			if (NpgsqlIntervalType   != null) SetProviderField(NpgsqlIntervalType,   NpgsqlIntervalType,   "GetInterval");
			if (NpgsqlTimeType       != null) SetProviderField(NpgsqlTimeType,       NpgsqlTimeType,       "GetTime");
			if (NpgsqlTimeTZType     != null) SetProviderField(NpgsqlTimeTZType,     NpgsqlTimeTZType,     "GetTimeTZ");
			if (_npgsqlTimeStamp     != null) SetProviderField(_npgsqlTimeStamp,     _npgsqlTimeStamp,     "GetTimeStamp");
			if (_npgsqlTimeStampTZ   != null) SetProviderField(_npgsqlTimeStampTZ,   _npgsqlTimeStampTZ,   "GetTimeStampTZ");
			if (NpgsqlMacAddressType != null) SetProviderField(NpgsqlMacAddressType, NpgsqlMacAddressType, "GetProviderSpecificValue");
			if (_npgsqlDateTime      != null) SetProviderField(_npgsqlDateTime,      _npgsqlDateTime,      "GetTimeStamp");

			SetProviderField(NpgsqlInetType,       NpgsqlInetType,       "GetProviderSpecificValue");
			SetProviderField(_npgsqlDate,          _npgsqlDate,          "GetDate");

            

			if (_npgsqlTimeStampTZ != null) 
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

			_setMoney     = GetSetParameter(connectionType, "NpgsqlParameter", "NpgsqlDbType", "NpgsqlTypes.NpgsqlDbType", "Money");
			_setVarBinary = GetSetParameter(connectionType, "NpgsqlParameter", "NpgsqlDbType", "NpgsqlTypes.NpgsqlDbType", "Bytea");
			_setBoolean   = GetSetParameter(connectionType, "NpgsqlParameter", "NpgsqlDbType", "NpgsqlTypes.NpgsqlDbType", "Boolean");
			_setXml       = GetSetParameter(connectionType, "NpgsqlParameter", "NpgsqlDbType", "NpgsqlTypes.NpgsqlDbType", "Xml");
			_setText      = GetSetParameter(connectionType, "NpgsqlParameter", "NpgsqlDbType", "NpgsqlTypes.NpgsqlDbType", "Text");
			_setBit       = GetSetParameter(connectionType, "NpgsqlParameter", "NpgsqlDbType", "NpgsqlTypes.NpgsqlDbType", "Bit");
			_setHstore    = GetSetParameter(connectionType, "NpgsqlParameter", "NpgsqlDbType", "NpgsqlTypes.NpgsqlDbType", "Hstore");
		    _setJsonb = GetSetParameter(connectionType, "NpgsqlParameter", "NpgsqlDbType", "NpgsqlTypes.NpgsqlDbType", "Jsonb");


			if (BitStringType        != null) MappingSchema.AddScalarType(BitStringType);
			if (NpgsqlIntervalType   != null) MappingSchema.AddScalarType(NpgsqlIntervalType);
			if (NpgsqlTimeType       != null) MappingSchema.AddScalarType(NpgsqlTimeType);
			if (NpgsqlTimeTZType     != null) MappingSchema.AddScalarType(NpgsqlTimeTZType);
			if (_npgsqlTimeStamp     != null) MappingSchema.AddScalarType(_npgsqlTimeStamp);
			if (_npgsqlTimeStampTZ   != null) MappingSchema.AddScalarType(_npgsqlTimeStampTZ);
			if (NpgsqlMacAddressType != null) MappingSchema.AddScalarType(NpgsqlMacAddressType);
			if (_npgsqlDateTime      != null) MappingSchema.AddScalarType(_npgsqlDateTime);

			MappingSchema.AddScalarType(NpgsqlInetType);
			MappingSchema.AddScalarType(NpgsqlPointType);
			MappingSchema.AddScalarType(NpgsqlLSegType);
			MappingSchema.AddScalarType(NpgsqlBoxType);
			MappingSchema.AddScalarType(NpgsqlPathType);
			MappingSchema.AddScalarType(NpgsqlCircleType);
			MappingSchema.AddScalarType(_npgsqlDate);
			MappingSchema.AddScalarType(NpgsqlPolygonType);

			if (_npgsqlTimeStampTZ != null) 
			{
				// SetConvertExpression<NpgsqlTimeStampTZ,DateTimeOffset>(
				//     d => new DateTimeOffset(d.Year, d.Month, d.Day, d.Hours, d.Minutes, d.Seconds, d.Milliseconds,
				//         new TimeSpan(d.TimeZone.Hours, d.TimeZone.Minutes, d.TimeZone.Seconds)));

				var p = Expression.Parameter(_npgsqlTimeStampTZ, "p");

				MappingSchema.SetConvertExpression(_npgsqlTimeStampTZ, typeof(DateTimeOffset),
					Expression.Lambda(
						Expression.New(
							MemberHelper.ConstructorOf(() => new DateTimeOffset(0L, new TimeSpan())),
							Expression.PropertyOrField(p, "Ticks"),
							Expression.New(
								MemberHelper.ConstructorOf(() => new TimeSpan(0, 0, 0)),
								Expression.PropertyOrField(Expression.PropertyOrField(p, "TimeZone"), "Hours"),
								Expression.PropertyOrField(Expression.PropertyOrField(p, "TimeZone"), "Minutes"),
								Expression.PropertyOrField(Expression.PropertyOrField(p, "TimeZone"), "Seconds"))),
						p
					));
			}

			if (_npgsqlDateTime != null)
			{
				var p = Expression.Parameter(_npgsqlDateTime, "p");

				MappingSchema.SetConvertExpression(_npgsqlDateTime, typeof(DateTimeOffset),
					Expression.Lambda(
						Expression.New(
							MemberHelper.ConstructorOf(() => new DateTimeOffset(new DateTime())),
							Expression.PropertyOrField(p, "DateTime")),p));
			}
		}

		public    override string ConnectionNamespace { get { return "Npgsql";                          } }
		protected override string ConnectionTypeName  { get { return "Npgsql.NpgsqlConnection, Npgsql"; } }
		protected override string DataReaderTypeName  { get { return "Npgsql.NpgsqlDataReader, Npgsql"; } }

		public override ISqlBuilder CreateSqlBuilder()
		{
			return new PostgreSQLSqlBuilder(GetSqlOptimizer(), SqlProviderFlags, MappingSchema.ValueToSqlConverter);
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

		static Action<IDbDataParameter> _setMoney;
		static Action<IDbDataParameter> _setVarBinary;
		static Action<IDbDataParameter> _setBoolean;
		static Action<IDbDataParameter> _setXml;
		static Action<IDbDataParameter> _setText;
		static Action<IDbDataParameter> _setBit;
		static Action<IDbDataParameter> _setHstore;
	    private static Action<IDbDataParameter> _setJsonb;

		public override void SetParameter(IDbDataParameter parameter, string name, DataType dataType, object value)
		{
			if (value is IDictionary && dataType == DataType.Undefined)
			{
				dataType = DataType.Dictionary;
			}

			base.SetParameter(parameter, name, dataType, value);
		}

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
				case DataType.Decimal    : parameter.DbType = DbType.Decimal;          break;
				case DataType.Money      : _setMoney(parameter);                       break;
				case DataType.Image      :
				case DataType.Binary     :
				case DataType.VarBinary  : _setVarBinary(parameter);                   break;
				case DataType.Boolean    : _setBoolean  (parameter);                   break;
				case DataType.Xml        : _setXml      (parameter);                   break;
				case DataType.Text       :
				case DataType.NText      : _setText     (parameter);                   break;
				case DataType.BitArray   : _setBit      (parameter);                   break;
				case DataType.Dictionary : _setHstore(parameter);                      break;
                case DataType.Jsonb:
			        _setJsonb(parameter); break;
				default                  : base.SetParameterType(parameter, dataType); break;
			}
		}

		#region BulkCopy

		public override BulkCopyRowsCopied BulkCopy<T>(
			[JetBrains.Annotations.NotNull] DataConnection dataConnection, BulkCopyOptions options, IEnumerable<T> source)
		{
			return new PostgreSQLBulkCopy().BulkCopy(
				options.BulkCopyType == BulkCopyType.Default ? PostgreSQLTools.DefaultBulkCopyType : options.BulkCopyType,
				dataConnection,
				options,
				source);
		}

		#endregion
	}
}
