using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.DataProvider.Oracle
{
	using Common;
	using Data;
	using Expressions;
	using Extensions;
	using Mapping;
	using SqlProvider;

	public class OracleDataProvider : DynamicDataProviderBase
	{
		private static readonly int NanosecondsPerTick = Convert.ToInt32(1000000000 / TimeSpan.TicksPerSecond);

		public OracleDataProvider()
			: this(OracleTools.DetectedProviderName)
		{
		}

		public OracleDataProvider(string name)
			: base(name, null)
		{
			//SqlProviderFlags.IsCountSubQuerySupported    = false;
			SqlProviderFlags.IsIdentityParameterRequired = true;

			SqlProviderFlags.MaxInListValuesCount = 1000;

			SetCharField("Char",  (r,i) => r.GetString(i).TrimEnd());
			SetCharField("NChar", (r,i) => r.GetString(i).TrimEnd());

//			ReaderExpressions[new ReaderInfo { FieldType = typeof(decimal), ToType = typeof(TimeSpan) }] =
//				(Expression<Func<IDataReader,int,TimeSpan>>)((rd,n) => new TimeSpan((long)rd.GetDecimal(n)));

			_sqlOptimizer = new OracleSqlOptimizer(SqlProviderFlags);
		
//			SetField<IDataReader,decimal>((r,i) => OracleTools.DataReaderGetDecimal(r, i));
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
			var typesNamespace  = AssemblyName + ".Types.";

			_oracleBFile        = connectionType.AssemblyEx().GetType(typesNamespace + "OracleBFile",        true);
			_oracleBinary       = connectionType.AssemblyEx().GetType(typesNamespace + "OracleBinary",       true);
			_oracleBlob         = connectionType.AssemblyEx().GetType(typesNamespace + "OracleBlob",         true);
			_oracleClob         = connectionType.AssemblyEx().GetType(typesNamespace + "OracleClob",         true);
			_oracleDate         = connectionType.AssemblyEx().GetType(typesNamespace + "OracleDate",         true);
			_oracleDecimal      = connectionType.AssemblyEx().GetType(typesNamespace + "OracleDecimal",      true);
			_oracleIntervalDS   = connectionType.AssemblyEx().GetType(typesNamespace + "OracleIntervalDS",   true);
			_oracleIntervalYM   = connectionType.AssemblyEx().GetType(typesNamespace + "OracleIntervalYM",   true);
			_oracleRefCursor    = connectionType.AssemblyEx().GetType(typesNamespace + "OracleRefCursor",    true);
			_oracleString       = connectionType.AssemblyEx().GetType(typesNamespace + "OracleString",       true);
			_oracleTimeStamp    = connectionType.AssemblyEx().GetType(typesNamespace + "OracleTimeStamp",    true);
			_oracleTimeStampLTZ = connectionType.AssemblyEx().GetType(typesNamespace + "OracleTimeStampLTZ", true);
			_oracleTimeStampTZ  = connectionType.AssemblyEx().GetType(typesNamespace + "OracleTimeStampTZ",  true);
			_oracleRef          = connectionType.AssemblyEx().GetType(typesNamespace + "OracleRef",          false);
			_oracleXmlType      = connectionType.AssemblyEx().GetType(typesNamespace + "OracleXmlType",      false);
			_oracleXmlStream    = connectionType.AssemblyEx().GetType(typesNamespace + "OracleXmlStream",    false);

			SetProviderField(_oracleBFile,        _oracleBFile,        "GetOracleBFile");
			SetProviderField(_oracleBinary,       _oracleBinary,       "GetOracleBinary");
			SetProviderField(_oracleBlob,         _oracleBlob,         "GetOracleBlob");
			SetProviderField(_oracleClob,         _oracleClob,         "GetOracleClob");
			SetProviderField(_oracleDate,         _oracleDate,         "GetOracleDate");
			SetProviderField(_oracleDecimal,      _oracleDecimal,      "GetOracleDecimal");
			SetProviderField(_oracleIntervalDS,   _oracleIntervalDS,   "GetOracleIntervalDS");
			SetProviderField(_oracleIntervalYM,   _oracleIntervalYM,   "GetOracleIntervalYM");
			SetProviderField(_oracleString,       _oracleString,       "GetOracleString");
			SetProviderField(_oracleTimeStamp,    _oracleTimeStamp,    "GetOracleTimeStamp");
			SetProviderField(_oracleTimeStampLTZ, _oracleTimeStampLTZ, "GetOracleTimeStampLTZ");
			SetProviderField(_oracleTimeStampTZ,  _oracleTimeStampTZ,  "GetOracleTimeStampTZ");

			try
			{
				if (_oracleRef != null)
					SetProviderField(_oracleRef, _oracleRef, "GetOracleRef");
			}
			catch
			{
			}

			try
			{
				if (_oracleXmlType != null)
					SetProviderField(_oracleXmlType, _oracleXmlType, "GetOracleXmlType");
			}
			catch
			{
			}

			var dataReaderParameter = Expression.Parameter(DataReaderType, "r");
			var indexParameter      = Expression.Parameter(typeof(int),    "i");

			{
				// static DateTimeOffset GetOracleTimeStampTZ(OracleDataReader rd, int idx)
				// {
				//     var tstz = rd.GetOracleTimeStampTZ(idx);
				//     return new DateTimeOffset(
				//         tstz.Year, tstz.Month,  tstz.Day,
				//         tstz.Hour, tstz.Minute, tstz.Second,
				//         TimeSpan.Parse(tstz.TimeZone.TrimStart('+'))).AddTicks(tstz.Nanosecond / NanosecondsPerTick);
				// }

				var tstz = Expression.Parameter(_oracleTimeStampTZ, "tstz");

				ReaderExpressions[new ReaderInfo { ToType = typeof(DateTimeOffset), ProviderFieldType = _oracleTimeStampTZ }] =
					Expression.Lambda(
						Expression.Block(
							new[] { tstz },
							new Expression[]
							{
								Expression.Assign(tstz, Expression.Call(dataReaderParameter, "GetOracleTimeStampTZ", null, indexParameter)),
								Expression.Call(
									MemberHelper.MethodOf(() => ToDateTimeOffset(null)),
									Expression.Convert(tstz, typeof(object))
								)
							}),
						dataReaderParameter,
						indexParameter);
			}

			{
				// static decimal GetOracleDecimal(OracleDataReader rd, int idx)
				// {
				//     var tstz = rd.GetOracleDecimal(idx);
				//     decimal decimalVar;
				//     var precision = 29;
				//     while (true)
				//     {
				//        try
				//        {  
				//           tstz = OracleDecimal.SetPrecision(tstz, precision);
				//           decimalVar = (decimal)tstz;
				//           break;
				//        }
				//        catch(OverflowException exceptionVar)
				//        {
				//           if (--precision <= 26)
				//              throw exceptionVar;
				//        }
				//     }
				//
				//     return decimalVar;
				// }

				var tstz               = Expression.Parameter(_oracleDecimal, "tstz");
				var decimalVar         = Expression.Variable(typeof(decimal), "decimalVar");
				var precision          = Expression.Variable(typeof(int),     "precision");
				var label              = Expression.Label(typeof(decimal));
				var setPrecisionMethod = _oracleDecimal.GetMethod("SetPrecision", BindingFlags.Static | BindingFlags.Public);

				var getDecimalAdv = Expression.Lambda(
					Expression.Block(
						new[] {tstz, decimalVar, precision},
						Expression.Assign(tstz, Expression.Call(dataReaderParameter, "GetOracleDecimal", null, indexParameter)),
						Expression.Assign(precision, Expression.Constant(29)),
						Expression.Loop(
							Expression.TryCatch(
								Expression.Block(
									Expression.Assign(tstz, Expression.Call(setPrecisionMethod, tstz, precision)),
									Expression.Assign(decimalVar, Expression.Convert(tstz, typeof(decimal))),
									Expression.Break(label, decimalVar),
									Expression.Constant(0)
								),
								Expression.Catch(typeof(OverflowException),
									Expression.Block(
										Expression.IfThen(
											Expression.LessThanOrEqual(Expression.SubtractAssign(precision, Expression.Constant(1)),
												Expression.Constant(26)),
											Expression.Rethrow()
										),
										Expression.Constant(0)
									)

								)
							),
							label),
						decimalVar
					),
					dataReaderParameter,
					indexParameter);


				// static T GetDecimalValue<T>(OracleDataReader rd, int idx)
				// {
				//    return (T) OracleDecimal.SetPrecision(rd.GetOracleDecimal(idx), 27);
				// }

				Func<Type, LambdaExpression> getDecimal = t =>
					Expression.Lambda(
						Expression.ConvertChecked(
							Expression.Call(setPrecisionMethod,
								Expression.Call(dataReaderParameter, "GetOracleDecimal", null, indexParameter), Expression.Constant(27)),
							t),
						dataReaderParameter,
						indexParameter);

				ReaderExpressions[new ReaderInfo { ToType = typeof(decimal), ProviderFieldType = _oracleDecimal }] = getDecimalAdv;
				ReaderExpressions[new ReaderInfo { ToType = typeof(decimal), FieldType = typeof(decimal)}        ] = getDecimalAdv;
				ReaderExpressions[new ReaderInfo { ToType = typeof(int),     FieldType = typeof(decimal)}        ] = getDecimal(typeof(int));
				ReaderExpressions[new ReaderInfo { ToType = typeof(long),    FieldType = typeof(decimal)}        ] = getDecimal(typeof(long));
				ReaderExpressions[new ReaderInfo {                           FieldType = typeof(decimal)}        ] = getDecimal(typeof(decimal));
			}

			{
				// static DateTimeOffset GetOracleTimeStampLTZ(OracleDataReader rd, int idx)
				// {
				//     var tstz = rd.GetOracleTimeStampLTZ(idx).ToOracleTimeStampTZ();
				//     return new DateTimeOffset(
				//         tstz.Year, tstz.Month,  tstz.Day,
				//         tstz.Hour, tstz.Minute, tstz.Second,
				//         TimeSpan.Parse(tstz.TimeZone.TrimStart('+'))).AddTicks(tstz.Nanosecond / NanosecondsPerTick);
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
								Expression.Call(
									MemberHelper.MethodOf(() => ToDateTimeOffset(null)),
									Expression.Convert(tstz, typeof(object))
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
									connectionType.AssemblyEx().GetType(AssemblyName + ".Client.OracleCommand", true)),
								"BindByName"),
							Expression.Constant(true)),
							p
					).Compile();
			}

			{
				// value = new OracleTimeStampTZ(dto.Year, dto.Month, dto.Day, dto.Hour, dto.Minute, dto.Second, GetDateTimeOffsetNanoseconds(dto), zone);

				var dto  = Expression.Parameter(typeof(DateTimeOffset), "dto");
				var zone = Expression.Parameter(typeof(string),         "zone");

				_createOracleTimeStampTZ =
					Expression.Lambda<Func<DateTimeOffset,string,object>>(
						Expression.Convert(
							Expression.New(
								_oracleTimeStampTZ.GetConstructorEx(new []
								{
									typeof(int), typeof(int), typeof(int), typeof(int), typeof(int), typeof(int), typeof(int), typeof(string)
								}),
								Expression.PropertyOrField(dto, "Year"),
								Expression.PropertyOrField(dto, "Month"),
								Expression.PropertyOrField(dto, "Day"),
								Expression.PropertyOrField(dto, "Hour"),
								Expression.PropertyOrField(dto, "Minute"),
								Expression.PropertyOrField(dto, "Second"),
								Expression.Call(null, MemberHelper.MethodOf(() => GetDateTimeOffsetNanoseconds(default(DateTimeOffset))), dto),
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
			_setCursor         = GetSetParameter(connectionType, "OracleParameter", "OracleDbType", "OracleDbType", "RefCursor");

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

		static int GetDateTimeOffsetNanoseconds(DateTimeOffset value)
		{
			var tmp = new DateTimeOffset(value.Year, value.Month, value.Day, value.Hour, value.Minute, value.Second, value.Offset);

			return Convert.ToInt32((value.Ticks - tmp.Ticks) * NanosecondsPerTick);
		}

		static DateTimeOffset ToDateTimeOffset(object value)
		{
			dynamic tstz = value;

			return new DateTimeOffset(
				tstz.Year, tstz.Month,  tstz.Day, 
				tstz.Hour, tstz.Minute, tstz.Second,
				tstz.GetTimeZoneOffset()).AddTicks(tstz.Nanosecond / NanosecondsPerTick);
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

		public string AssemblyName
		{
			get { return Name == ProviderName.OracleNative ? "Oracle.DataAccess" : "Oracle.ManagedDataAccess"; }
		}

		public    override string ConnectionNamespace { get { return AssemblyName + ".Client"; } }
		protected override string ConnectionTypeName  { get { return "{0}.{1}, {0}".Args(AssemblyName, "Client.OracleConnection"); } }
		protected override string DataReaderTypeName  { get { return "{0}.{1}, {0}".Args(AssemblyName, "Client.OracleDataReader"); } }

		public bool IsXmlTypeSupported
		{
			get { return _oracleXmlType != null; }
		}

		public override ISqlBuilder CreateSqlBuilder()
		{
			return new OracleSqlBuilder(GetSqlOptimizer(), SqlProviderFlags, MappingSchema.ValueToSqlConverter);
		}


		static class MappingSchemaInstance
		{
			public static readonly OracleMappingSchema.NativeMappingSchema  NativeMappingSchema  = new OracleMappingSchema.NativeMappingSchema();
			public static readonly OracleMappingSchema.ManagedMappingSchema ManagedMappingSchema = new OracleMappingSchema.ManagedMappingSchema();
		}

		public override MappingSchema MappingSchema
		{
			get
			{
				return Name == ProviderName.OracleNative
					? MappingSchemaInstance.NativeMappingSchema as MappingSchema
					: MappingSchemaInstance.ManagedMappingSchema;
			}
		}

		readonly ISqlOptimizer _sqlOptimizer;

		public override ISqlOptimizer GetSqlOptimizer()
		{
			return _sqlOptimizer;
		}

#if !NETSTANDARD
		public override SchemaProvider.ISchemaProvider GetSchemaProvider()
		{
			return new OracleSchemaProvider(Name);
		}
#endif 

		Action<DataConnection> _setBindByName;

		public override void InitCommand(DataConnection dataConnection, CommandType commandType, string commandText, DataParameter[] parameters)
		{
			dataConnection.DisposeCommand();

			if (_setBindByName == null)
				EnsureConnection();

			_setBindByName(dataConnection);

			base.InitCommand(dataConnection, commandType, commandText, parameters);

			if (parameters != null)
				foreach (var parameter in parameters)
				{
					if (parameter.IsArray && parameter.Value is object[])
					{
						var value = (object[])parameter.Value;

						if (value.Length != 0)
						{
							dynamic command = dataConnection.Command;
						
							command.ArrayBindCount = value.Length;

							break;
						}
					}
				}
		}

		public override void DisposeCommand(DataConnection dataConnection)
		{
			foreach (DbParameter param in dataConnection.Command.Parameters)
			{
//				if (param != null && param.Value != null && param.Value is IDisposable)
//					((IDisposable)param.Value).Dispose();

				if (param is IDisposable)
					((IDisposable)param).Dispose();
			}

			base.DisposeCommand(dataConnection);
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

			if (dataType == DataType.Undefined && value is string && ((string)value).Length >= 4000)
			{
				dataType = DataType.NText;
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

		Action<IDbDataParameter> _setSingle;
		Action<IDbDataParameter> _setDouble;
		Action<IDbDataParameter> _setText;
		Action<IDbDataParameter> _setNText;
		Action<IDbDataParameter> _setImage;
		Action<IDbDataParameter> _setBinary;
		Action<IDbDataParameter> _setVarBinary;
		Action<IDbDataParameter> _setDate;
		Action<IDbDataParameter> _setSmallDateTime;
		Action<IDbDataParameter> _setDateTime2;
		Action<IDbDataParameter> _setDateTimeOffset;
		Action<IDbDataParameter> _setGuid;
		Action<IDbDataParameter> _setCursor;

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
				case DataType.Cursor         : _setCursor           (parameter);           break;
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

#region Merge

		public override int Merge<T>(DataConnection dataConnection, Expression<Func<T,bool>> deletePredicate, bool delete, IEnumerable<T> source,
			string tableName, string databaseName, string schemaName)
		{
			if (delete)
				throw new LinqToDBException("Oracle MERGE statement does not support DELETE by source.");

			return new OracleMerge().Merge(dataConnection, deletePredicate, delete, source, tableName, databaseName, schemaName);
		}

#endregion
	}
}
