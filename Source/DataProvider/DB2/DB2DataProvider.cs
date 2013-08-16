using System;
using System.Data;
using System.Linq.Expressions;

namespace LinqToDB.DataProvider.DB2
{
	using Data;
	using Mapping;
	using SchemaProvider;
	using SqlProvider;

	public class DB2DataProvider : DynamicDataProviderBase
	{
		public DB2DataProvider()
			: this(ProviderName.DB2, new DB2MappingSchema())
		{
		}

		protected DB2DataProvider(string name, MappingSchema mappingSchema)
			: base(name, mappingSchema)
		{
			SqlProviderFlags.AcceptsTakeAsParameter       = false;
			SqlProviderFlags.AcceptsTakeAsParameterIfSkip = true;

			SetCharField("CHAR", (r,i) => r.GetString(i).TrimEnd());
		}

		Type _db2Int64;
		Type _db2Int32;
		Type _db2Int16;
		Type _db2Decimal;
		Type _db2DecimalFloat;
		Type _db2Real;
		Type _db2Real370;
		Type _db2Double;
		Type _db2String;
		Type _db2Clob;
		Type _db2Binary;
		Type _db2Blob;
		Type _db2Date;
		Type _db2Time;
		Type _db2TimeStamp;
		Type _db2Xml;
		Type _db2RowId;

		protected override void OnConnectionTypeCreated(Type connectionType)
		{
			_db2Int64        = connectionType.Assembly.GetType("IBM.Data.DB2Types.DB2Int64",        true);
			_db2Int32        = connectionType.Assembly.GetType("IBM.Data.DB2Types.DB2Int32",        true);
			_db2Int16        = connectionType.Assembly.GetType("IBM.Data.DB2Types.DB2Int16",        true);
			_db2Decimal      = connectionType.Assembly.GetType("IBM.Data.DB2Types.DB2Decimal",      true);
			_db2DecimalFloat = connectionType.Assembly.GetType("IBM.Data.DB2Types.DB2DecimalFloat", true);
			_db2Real         = connectionType.Assembly.GetType("IBM.Data.DB2Types.DB2Real",         true);
			_db2Real370      = connectionType.Assembly.GetType("IBM.Data.DB2Types.DB2Real370",      true);
			_db2Double       = connectionType.Assembly.GetType("IBM.Data.DB2Types.DB2Double",       true);
			_db2String       = connectionType.Assembly.GetType("IBM.Data.DB2Types.DB2String",       true);
			_db2Clob         = connectionType.Assembly.GetType("IBM.Data.DB2Types.DB2Clob",         true);
			_db2Binary       = connectionType.Assembly.GetType("IBM.Data.DB2Types.DB2Binary",       true);
			_db2Blob         = connectionType.Assembly.GetType("IBM.Data.DB2Types.DB2Blob",         true);
			_db2Date         = connectionType.Assembly.GetType("IBM.Data.DB2Types.DB2Date",         true);
			_db2Time         = connectionType.Assembly.GetType("IBM.Data.DB2Types.DB2Time",         true);
			_db2TimeStamp    = connectionType.Assembly.GetType("IBM.Data.DB2Types.DB2TimeStamp",    true);
			_db2Xml          = connectionType.Assembly.GetType("IBM.Data.DB2Types.DB2Xml",          true);
			_db2RowId        = connectionType.Assembly.GetType("IBM.Data.DB2Types.DB2RowId",        true);

			SetProviderField(_db2Int64,        typeof(Int64),    "GetDB2Int64");
			SetProviderField(_db2Int32,        typeof(Int32),    "GetDB2Int32");
			SetProviderField(_db2Int16,        typeof(Int16),    "GetDB2Int16");
			SetProviderField(_db2Decimal,      typeof(Decimal),  "GetDB2Decimal");
			SetProviderField(_db2DecimalFloat, typeof(Decimal),  "GetDB2DecimalFloat");
			SetProviderField(_db2Real,         typeof(Single),   "GetDB2Real");
			SetProviderField(_db2Real370,      typeof(Single),   "GetDB2Real370");
			SetProviderField(_db2Double,       typeof(Double),   "GetDB2Double");
			SetProviderField(_db2String,       typeof(String),   "GetDB2String");
			SetProviderField(_db2Clob,         typeof(String),   "GetDB2Clob");
			SetProviderField(_db2Binary,       typeof(byte[]),   "GetDB2Binary");
			SetProviderField(_db2Blob,         typeof(byte[]),   "GetDB2Blob");
			SetProviderField(_db2Date,         typeof(DateTime), "GetDB2Date");
			SetProviderField(_db2Time,         typeof(TimeSpan), "GetDB2Time");
			SetProviderField(_db2TimeStamp,    typeof(DateTime), "GetDB2TimeStamp");
			SetProviderField(_db2Xml,          typeof(string),   "GetDB2Xml");
			SetProviderField(_db2RowId,        typeof(byte[]),   "GetDB2RowId");

			MappingSchema.AddScalarType(_db2Int64,        GetNullValue(_db2Int64),        true, DataType.Int64);
			MappingSchema.AddScalarType(_db2Int32,        GetNullValue(_db2Int32),        true, DataType.Int32);
			MappingSchema.AddScalarType(_db2Int16,        GetNullValue(_db2Int16),        true, DataType.Int16);
			MappingSchema.AddScalarType(_db2Decimal,      GetNullValue(_db2Decimal),      true, DataType.Decimal);
			MappingSchema.AddScalarType(_db2DecimalFloat, GetNullValue(_db2DecimalFloat), true, DataType.Decimal);
			MappingSchema.AddScalarType(_db2Real,         GetNullValue(_db2Real),         true, DataType.Single);
			MappingSchema.AddScalarType(_db2Real370,      GetNullValue(_db2Real370),      true, DataType.Single);
			MappingSchema.AddScalarType(_db2Double,       GetNullValue(_db2Double),       true, DataType.Double);
			MappingSchema.AddScalarType(_db2String,       GetNullValue(_db2String),       true, DataType.NVarChar);
			MappingSchema.AddScalarType(_db2Clob,         GetNullValue(_db2Clob),         true, DataType.NText);
			MappingSchema.AddScalarType(_db2Binary,       GetNullValue(_db2Binary),       true, DataType.VarBinary);
			MappingSchema.AddScalarType(_db2Blob,         GetNullValue(_db2Blob),         true, DataType.Blob);
			MappingSchema.AddScalarType(_db2Date,         GetNullValue(_db2Date),         true, DataType.Date);
			MappingSchema.AddScalarType(_db2Time,         GetNullValue(_db2Time),         true, DataType.Time);
			MappingSchema.AddScalarType(_db2TimeStamp,    GetNullValue(_db2TimeStamp),    true, DataType.DateTime2);
			MappingSchema.AddScalarType(_db2Xml,          GetNullValue(_db2Xml),          true, DataType.Xml);
			MappingSchema.AddScalarType(_db2RowId,        GetNullValue(_db2RowId),        true, DataType.VarBinary);

			_setBlob = GetSetParameter(connectionType, "DB2Parameter", "DB2Type", "DB2Type", "Blob");
		}

		static object GetNullValue(Type type)
		{
			var getValue = Expression.Lambda<Func<object>>(Expression.Convert(Expression.Field(null, type, "Null"), typeof(object)));
			return getValue.Compile()();
		}

		public    override string ConnectionNamespace { get { return "IBM.Data.DB2"; } }
		protected override string ConnectionTypeName  { get { return "IBM.Data.DB2.DB2Connection, IBM.Data.DB2"; } }
		protected override string DataReaderTypeName  { get { return "IBM.Data.DB2.DB2DataReader, IBM.Data.DB2"; } }

		public override ISchemaProvider GetSchemaProvider()
		{
			return new DB2SchemaProvider();
		}

		public override ISqlBuilder CreateSqlBuilder()
		{
			return new DB2SqlBuilder(GetSqlOptimizer(), SqlProviderFlags);
		}

		readonly DB2SqlOptimizer _sqlOptimizer = new DB2SqlOptimizer();

		public override ISqlOptimizer GetSqlOptimizer()
		{
			return _sqlOptimizer;
		}

		public override void InitCommand(DataConnection dataConnection)
		{
			dataConnection.DisposeCommand();
			base.InitCommand(dataConnection);
		}

		static Action<IDbDataParameter> _setBlob;

		public override void SetParameter(IDbDataParameter parameter, string name, DataType dataType, object value)
		{
			if (value is sbyte)
			{
				value    = (short)(sbyte)value;
				dataType = DataType.Int16;
			}
			else if (value is byte)
			{
				value    = (short)(byte)value;
				dataType = DataType.Int16;
			}

			switch (dataType)
			{
				case DataType.UInt16     : dataType = DataType.Int32;    break;
				case DataType.UInt32     : dataType = DataType.Int64;    break;
				case DataType.UInt64     : dataType = DataType.Decimal;  break;
				case DataType.VarNumeric : dataType = DataType.Decimal;  break;
				case DataType.DateTime2  : dataType = DataType.DateTime; break;
				case DataType.Char       :
				case DataType.VarChar    :
				case DataType.NChar      :
				case DataType.NVarChar   :
					     if (value is Guid) value = ((Guid)value).ToString();
					else if (value is bool)
						value = Common.ConvertTo<char>.From((bool)value);
					break;
				case DataType.Boolean    :
				case DataType.Int16      :
					if (value is bool)
					{
						value    = (bool)value ? 1 : 0;
						dataType = DataType.Int16;
					}
					break;
				case DataType.Guid       :
					if (value is Guid)
					{
						value    = ((Guid)value).ToByteArray();
						dataType = DataType.VarBinary;
					}
					break;
				case DataType.Binary     :
				case DataType.VarBinary  :
					if (value is Guid) value = ((Guid)value).ToByteArray();
					break;
				case DataType.Blob       :
					base.SetParameter(parameter, "@" + name, dataType, value);
					_setBlob(parameter);
					return;
			}

			base.SetParameter(parameter, "@" + name, dataType, value);
		}
	}
}
