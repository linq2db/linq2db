using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

using LinqToDB.Extensions;

namespace LinqToDB.DataProvider.Oracle
{
	using Common;
	using Data;
	using Expressions;
	using Mapping;
	using SqlProvider;

	public class OracleDataProvider : DynamicDataProviderBase
	{
		public OracleDataProvider()
			: this(ProviderName.Oracle, new OracleMappingSchema())
		{
		}

		protected OracleDataProvider(string name, MappingSchema mappingSchema)
			: base(name, mappingSchema)
		{
			SqlProviderFlags.IsCountSubQuerySupported    = false;
			SqlProviderFlags.IsIdentityParameterRequired = true;

			SqlProviderFlags.MaxInListValuesCount = 1000;

			SetCharField("Char",  (r,i) => r.GetString(i).TrimEnd());
			SetCharField("NChar", (r,i) => r.GetString(i).TrimEnd());

//			ReaderExpressions[new ReaderInfo { FieldType = typeof(decimal), ToType = typeof(TimeSpan) }] =
//				(Expression<Func<IDataReader,int,TimeSpan>>)((rd,n) => new TimeSpan((long)rd.GetDecimal(n)));

			_sqlOptimizer = new OracleSqlOptimizer(SqlProviderFlags);
		}

		Type _oracleBFile;
		Type _oracleBinary;
		Type _oracleBlob;
		Type _oracleClob;
		Type _oracleDate;
		Type _oracleDecimal;
		Type _oracleIntervalDS;
		Type _oracleIntervalYM;
		Type _oracleRef;
		Type _oracleRefCursor;
		Type _oracleString;
		Type _oracleTimeStamp;
		Type _oracleTimeStampLTZ;
		Type _oracleTimeStampTZ;
		Type _oracleXmlType;
		Type _oracleXmlStream;

		protected override void OnConnectionTypeCreated(Type connectionType)
		{
			var typesNamespace  = OracleTools.AssemblyName + ".Types.";

			_oracleBFile        = connectionType.Assembly.GetType(typesNamespace + "OracleBFile",        true);
			_oracleBinary       = connectionType.Assembly.GetType(typesNamespace + "OracleBinary",       true);
			_oracleBlob         = connectionType.Assembly.GetType(typesNamespace + "OracleBlob",         true);
			_oracleClob         = connectionType.Assembly.GetType(typesNamespace + "OracleClob",         true);
			_oracleDate         = connectionType.Assembly.GetType(typesNamespace + "OracleDate",         true);
			_oracleDecimal      = connectionType.Assembly.GetType(typesNamespace + "OracleDecimal",      true);
			_oracleIntervalDS   = connectionType.Assembly.GetType(typesNamespace + "OracleIntervalDS",   true);
			_oracleIntervalYM   = connectionType.Assembly.GetType(typesNamespace + "OracleIntervalYM",   true);
			_oracleRefCursor    = connectionType.Assembly.GetType(typesNamespace + "OracleRefCursor",    true);
			_oracleString       = connectionType.Assembly.GetType(typesNamespace + "OracleString",       true);
			_oracleTimeStamp    = connectionType.Assembly.GetType(typesNamespace + "OracleTimeStamp",    true);
			_oracleTimeStampLTZ = connectionType.Assembly.GetType(typesNamespace + "OracleTimeStampLTZ", true);
			_oracleTimeStampTZ  = connectionType.Assembly.GetType(typesNamespace + "OracleTimeStampTZ",  true);
			_oracleRef          = connectionType.Assembly.GetType(typesNamespace + "OracleRef",          false);
			_oracleXmlType      = connectionType.Assembly.GetType(typesNamespace + "OracleXmlType",      false);
			_oracleXmlStream    = connectionType.Assembly.GetType(typesNamespace + "OracleXmlStream",    false);

			SetProviderField(_oracleBFile,           _oracleBFile,        "GetOracleBFile");
			SetProviderField(_oracleBinary,          _oracleBinary,       "GetOracleBinary");
			SetProviderField(_oracleBlob,            _oracleBlob,         "GetOracleBlob");
			SetProviderField(_oracleClob,            _oracleClob,         "GetOracleClob");
			SetProviderField(_oracleDate,            _oracleDate,         "GetOracleDate");
			SetProviderField(_oracleDecimal,         _oracleDecimal,      "GetOracleDecimal");
			SetProviderField(_oracleIntervalDS,      _oracleIntervalDS,   "GetOracleIntervalDS");
			SetProviderField(_oracleIntervalYM,      _oracleIntervalYM,   "GetOracleIntervalYM");
			SetProviderField(_oracleString,          _oracleString,       "GetOracleString");
			SetProviderField(_oracleTimeStamp,       _oracleTimeStamp,    "GetOracleTimeStamp");
			SetProviderField(_oracleTimeStampLTZ,    _oracleTimeStampLTZ, "GetOracleTimeStampLTZ");
			SetProviderField(_oracleTimeStampTZ,     _oracleTimeStampTZ,  "GetOracleTimeStampTZ");

			if (_oracleRef != null)
				SetProviderField(_oracleRef, _oracleRef, "GetOracleRef");

			if (_oracleXmlType != null)
				SetProviderField(_oracleXmlType, _oracleXmlType, "GetOracleXmlType");

			var dataReaderParameter = Expression.Parameter(DataReaderType, "r");
			var indexParameter      = Expression.Parameter(typeof(int),    "i");

			{
				// static DateTimeOffset GetOracleTimeStampTZ(OracleDataReader rd, int idx)
				// {
				//     var tstz = rd.GetOracleTimeStampTZ(idx);
				//     return new DateTimeOffset(
				//         tstz.Year, tstz.Month,  tstz.Day,
				//         tstz.Hour, tstz.Minute, tstz.Second, (int)tstz.Millisecond,
				//         TimeSpan.Parse(tstz.TimeZone.TrimStart('+')));
				// }

				var tstz = Expression.Parameter(_oracleTimeStampTZ, "tstz");

				ReaderExpressions[new ReaderInfo { ToType = typeof(DateTimeOffset), ProviderFieldType = _oracleTimeStampTZ }] =
					Expression.Lambda(
						Expression.Block(
							new[] { tstz },
							new Expression[]
							{
								Expression.Assign(tstz, Expression.Call(dataReaderParameter, "GetOracleTimeStampTZ", null, indexParameter)),
								Expression.New(
									MemberHelper.ConstructorOf(() => new DateTimeOffset(0,0,0,0,0,0,0,new TimeSpan())),
									Expression.PropertyOrField(tstz, "Year"),
									Expression.PropertyOrField(tstz, "Month"),
									Expression.PropertyOrField(tstz, "Day"),
									Expression.PropertyOrField(tstz, "Hour"),
									Expression.PropertyOrField(tstz, "Minute"),
									Expression.PropertyOrField(tstz, "Second"),
									Expression.Convert(Expression.PropertyOrField(tstz, "Millisecond"), typeof(int)),
									Expression.Call(
										MemberHelper.MethodOf(() => TimeSpan.Parse("")),
										Expression.Call(
											Expression.PropertyOrField(tstz, "TimeZone"),
											MemberHelper.MethodOf(() => "".TrimStart(' ')),
											Expression.NewArrayInit(typeof(char), Expression.Constant('+'))))
								)
							}),
						dataReaderParameter,
						indexParameter);
			}

			{
				// static DateTimeOffset GetOracleTimeStampLTZ(OracleDataReader rd, int idx)
				// {
				//     var tstz = rd.GetOracleTimeStampLTZ(idx).ToOracleTimeStampTZ();
				//     return new DateTimeOffset(
				//         tstz.Year, tstz.Month,  tstz.Day,
				//         tstz.Hour, tstz.Minute, tstz.Second, (int)tstz.Millisecond,
				//         TimeSpan.Parse(tstz.TimeZone.TrimStart('+')));
				// }

				var tstz = Expression.Parameter(_oracleTimeStampTZ, "tstz");

				ReaderExpressions[new ReaderInfo { ToType = typeof(DateTimeOffset), ProviderFieldType = _oracleTimeStampLTZ }] =
					Expression.Lambda(
						Expression.Block(
							new[] { tstz },
							new Expression[]
							{
								Expression.Assign(
									tstz,
									Expression.Call(
										Expression.Call(dataReaderParameter, "GetOracleTimeStampLTZ", null, indexParameter),
										"ToOracleTimeStampTZ",
										null,
										null)),
								Expression.New(
									MemberHelper.ConstructorOf(() => new DateTimeOffset(0,0,0,0,0,0,0,new TimeSpan())),
									Expression.PropertyOrField(tstz, "Year"),
									Expression.PropertyOrField(tstz, "Month"),
									Expression.PropertyOrField(tstz, "Day"),
									Expression.PropertyOrField(tstz, "Hour"),
									Expression.PropertyOrField(tstz, "Minute"),
									Expression.PropertyOrField(tstz, "Second"),
									Expression.Convert(Expression.PropertyOrField(tstz, "Millisecond"), typeof(int)),
									Expression.Call(
										MemberHelper.MethodOf(() => TimeSpan.Parse("")),
										Expression.Call(
											Expression.PropertyOrField(tstz, "TimeZone"),
											MemberHelper.MethodOf(() => "".TrimStart(' ')),
											Expression.NewArrayInit(typeof(char), Expression.Constant('+'))))
								)
							}),
						dataReaderParameter,
						indexParameter);
			}

			{
				// ((OracleCommand)dataConnection.Command).BindByName = true;

				var p = Expression.Parameter(typeof(DataConnection), "dataConnection");

				_setBindByName =
					Expression.Lambda<Action<DataConnection>>(
						Expression.Assign(
							Expression.PropertyOrField(
								Expression.Convert(
									Expression.PropertyOrField(p, "Command"),
									connectionType.Assembly.GetType(OracleTools.AssemblyName + ".Client.OracleCommand", true)),
								"BindByName"),
							Expression.Constant(true)),
							p
					).Compile();
			}

			{
				// value = new OracleTimeStampTZ(dto.Year, dto.Month, dto.Day, dto.Hour, dto.Minute, dto.Second, dto.Millisecond, zone);

				var dto  = Expression.Parameter(typeof(DateTimeOffset), "dto");
				var zone = Expression.Parameter(typeof(string),         "zone");

				_createOracleTimeStampTZ =
					Expression.Lambda<Func<DateTimeOffset,string,object>>(
						Expression.Convert(
							Expression.New(
								_oracleTimeStampTZ.GetConstructor(new[]
								{
									typeof(int), typeof(int), typeof(int), typeof(int), typeof(int), typeof(int), typeof(int), typeof(string)
								}),
								Expression.PropertyOrField(dto, "Year"),
								Expression.PropertyOrField(dto, "Month"),
								Expression.PropertyOrField(dto, "Day"),
								Expression.PropertyOrField(dto, "Hour"),
								Expression.PropertyOrField(dto, "Minute"),
								Expression.PropertyOrField(dto, "Second"),
								Expression.PropertyOrField(dto, "Millisecond"),
								zone),
							typeof(object)),
						dto,
						zone
					).Compile();
			}

			_setSingle         = GetSetParameter(connectionType, "OracleParameter", "OracleDbType", "OracleDbType", "BinaryFloat");
			_setDouble         = GetSetParameter(connectionType, "OracleParameter", "OracleDbType", "OracleDbType", "BinaryDouble");
			_setText           = GetSetParameter(connectionType, "OracleParameter", "OracleDbType", "OracleDbType", "Clob");
			_setNText          = GetSetParameter(connectionType, "OracleParameter", "OracleDbType", "OracleDbType", "NClob");
			_setImage          = GetSetParameter(connectionType, "OracleParameter", "OracleDbType", "OracleDbType", "Blob");
			_setBinary         = GetSetParameter(connectionType, "OracleParameter", "OracleDbType", "OracleDbType", "Blob");
			_setVarBinary      = GetSetParameter(connectionType, "OracleParameter", "OracleDbType", "OracleDbType", "Blob");
			_setDate           = GetSetParameter(connectionType, "OracleParameter", "OracleDbType", "OracleDbType", "Date");
			_setSmallDateTime  = GetSetParameter(connectionType, "OracleParameter", "OracleDbType", "OracleDbType", "Date");
			_setDateTime2      = GetSetParameter(connectionType, "OracleParameter", "OracleDbType", "OracleDbType", "TimeStamp");
			_setDateTimeOffset = GetSetParameter(connectionType, "OracleParameter", "OracleDbType", "OracleDbType", "TimeStampTZ");
			_setGuid           = GetSetParameter(connectionType, "OracleParameter", "OracleDbType", "OracleDbType", "Raw");

			MappingSchema.AddScalarType(_oracleBFile,        GetNullValue(_oracleBFile),        true, DataType.VarChar);    // ?
			MappingSchema.AddScalarType(_oracleBinary,       GetNullValue(_oracleBinary),       true, DataType.VarBinary);
			MappingSchema.AddScalarType(_oracleBlob,         GetNullValue(_oracleBlob),         true, DataType.Blob);       // ?
			MappingSchema.AddScalarType(_oracleClob,         GetNullValue(_oracleClob),         true, DataType.NText);
			MappingSchema.AddScalarType(_oracleDate,         GetNullValue(_oracleDate),         true, DataType.DateTime);
			MappingSchema.AddScalarType(_oracleDecimal,      GetNullValue(_oracleDecimal),      true, DataType.Decimal);
			MappingSchema.AddScalarType(_oracleIntervalDS,   GetNullValue(_oracleIntervalDS),   true, DataType.Time);       // ?
			MappingSchema.AddScalarType(_oracleIntervalYM,   GetNullValue(_oracleIntervalYM),   true, DataType.Date);       // ?
			MappingSchema.AddScalarType(_oracleRefCursor,    GetNullValue(_oracleRefCursor),    true, DataType.Binary);     // ?
			MappingSchema.AddScalarType(_oracleString,       GetNullValue(_oracleString),       true, DataType.NVarChar);
			MappingSchema.AddScalarType(_oracleTimeStamp,    GetNullValue(_oracleTimeStamp),    true, DataType.DateTime2);
			MappingSchema.AddScalarType(_oracleTimeStampLTZ, GetNullValue(_oracleTimeStampLTZ), true, DataType.DateTimeOffset);
			MappingSchema.AddScalarType(_oracleTimeStampTZ,  GetNullValue(_oracleTimeStampTZ),  true, DataType.DateTimeOffset);

			if (_oracleRef != null)
				MappingSchema.AddScalarType(_oracleRef, GetNullValue(_oracleRef), true, DataType.Binary); // ?

			if (_oracleXmlType != null)
				MappingSchema.AddScalarType(_oracleXmlType, GetNullValue(_oracleXmlType), true, DataType.Xml);

			if (_oracleXmlStream != null)
				MappingSchema.AddScalarType(_oracleXmlStream, GetNullValue(_oracleXmlStream), true, DataType.Xml); // ?
		}

		static object GetNullValue(Type type)
		{
			var getValue = Expression.Lambda<Func<object>>(Expression.Convert(Expression.Field(null, type, "Null"), typeof(object)));
			try
			{
				return getValue.Compile()();
			}
			catch (Exception)
			{
				return getValue.Compile()();
			}
		}

		public    override string ConnectionNamespace { get { return OracleTools.AssemblyName + ".Client"; } }
		protected override string ConnectionTypeName  { get { return "{0}.{1}, {0}".Args(OracleTools.AssemblyName, "Client.OracleConnection"); } }
		protected override string DataReaderTypeName  { get { return "{0}.{1}, {0}".Args(OracleTools.AssemblyName, "Client.OracleDataReader"); } }

		public bool IsXmlTypeSupported
		{
			get { return _oracleXmlType != null; }
		}

		public override ISqlBuilder CreateSqlBuilder()
		{
			return new OracleSqlBuilder(GetSqlOptimizer(), SqlProviderFlags);
		}

		readonly ISqlOptimizer _sqlOptimizer;

		public override ISqlOptimizer GetSqlOptimizer()
		{
			return _sqlOptimizer;
		}

		public override SchemaProvider.ISchemaProvider GetSchemaProvider()
		{
			return new OracleSchemaProvider();
		}

		Action<DataConnection> _setBindByName;

		public override void InitCommand(DataConnection dataConnection)
		{
			dataConnection.DisposeCommand();

			if (_setBindByName == null)
				EnsureConnection();

			_setBindByName(dataConnection);
		}

		Func<DateTimeOffset,string,object> _createOracleTimeStampTZ;

		public override void SetParameter(IDbDataParameter parameter, string name, DataType dataType, object value)
		{
			switch (dataType)
			{
				case DataType.DateTimeOffset:
					if (value is DateTimeOffset)
					{
						var dto  = (DateTimeOffset)value;
						var zone = dto.Offset.ToString("hh\\:mm");
						if (!zone.StartsWith("-") && !zone.StartsWith("+"))
							zone = "+" + zone;
						value = _createOracleTimeStampTZ(dto, zone);
					}
					break;
				case DataType.Boolean:
					dataType = DataType.Byte;
					if (value is bool)
						value = (bool)value ? (byte)1 : (byte)0;
					break;
				case DataType.Guid:
					if (value is Guid) value = ((Guid)value).ToByteArray();
					break;
				case DataType.Time:
					// According to http://docs.oracle.com/cd/E16655_01/win.121/e17732/featOraCommand.htm#ODPNT258
					// Inference of DbType and OracleDbType from Value: TimeSpan - Object - IntervalDS
					//
					if (value is TimeSpan)
						dataType = DataType.Undefined;
					break;
			}

			base.SetParameter(parameter, name, dataType, value);
		}

		public override Type ConvertParameterType(Type type, DataType dataType)
		{
			if (type.IsNullable())
				type = type.ToUnderlying();

			switch (dataType)
			{
				case DataType.DateTimeOffset : if (type == typeof(DateTimeOffset)) return _oracleTimeStampTZ; break;
				case DataType.Boolean        : if (type == typeof(bool))           return typeof(byte);       break;
				case DataType.Guid           : if (type == typeof(Guid))           return typeof(byte[]);     break;
			}

			return base.ConvertParameterType(type, dataType);
		}

		static Action<IDbDataParameter> _setSingle;
		static Action<IDbDataParameter> _setDouble;
		static Action<IDbDataParameter> _setText;
		static Action<IDbDataParameter> _setNText;
		static Action<IDbDataParameter> _setImage;
		static Action<IDbDataParameter> _setBinary;
		static Action<IDbDataParameter> _setVarBinary;
		static Action<IDbDataParameter> _setDate;
		static Action<IDbDataParameter> _setSmallDateTime;
		static Action<IDbDataParameter> _setDateTime2;
		static Action<IDbDataParameter> _setDateTimeOffset;
		static Action<IDbDataParameter> _setGuid;

		protected override void SetParameterType(IDbDataParameter parameter, DataType dataType)
		{
			if (parameter is BulkCopyReader.Parameter)
				return;

			switch (dataType)
			{
				case DataType.Byte           : parameter.DbType = DbType.Int16;            break;
				case DataType.SByte          : parameter.DbType = DbType.Int16;            break;
				case DataType.UInt16         : parameter.DbType = DbType.Int32;            break;
				case DataType.UInt32         : parameter.DbType = DbType.Int64;            break;
				case DataType.UInt64         : parameter.DbType = DbType.Decimal;          break;
				case DataType.VarNumeric     : parameter.DbType = DbType.Decimal;          break;
				case DataType.Single         : _setSingle           (parameter);           break;
				case DataType.Double         : _setDouble           (parameter);           break;
				case DataType.Text           : _setText             (parameter);           break;
				case DataType.NText          : _setNText            (parameter);           break;
				case DataType.Image          : _setImage            (parameter);           break;
				case DataType.Binary         : _setBinary           (parameter);           break;
				case DataType.VarBinary      : _setVarBinary        (parameter);           break;
				case DataType.Date           : _setDate             (parameter);           break;
				case DataType.SmallDateTime  : _setSmallDateTime    (parameter);           break;
				case DataType.DateTime2      : _setDateTime2        (parameter);           break;
				case DataType.DateTimeOffset : _setDateTimeOffset   (parameter);           break;
				case DataType.Guid           : _setGuid             (parameter);           break;
				default                      : base.SetParameterType(parameter, dataType); break;
			}
		}

		#region BulkCopy

		OracleBulkCopy _bulkCopy;

		public override BulkCopyRowsCopied BulkCopy<T>(DataConnection dataConnection, BulkCopyOptions options, IEnumerable<T> source)
		{
			if (_bulkCopy == null)
				_bulkCopy = new OracleBulkCopy(this, GetConnectionType());

			return _bulkCopy.BulkCopy(
				options.BulkCopyType == BulkCopyType.Default ? OracleTools.DefaultBulkCopyType : options.BulkCopyType,
				dataConnection,
				options,
				source);
		}

		#endregion

	}
}
