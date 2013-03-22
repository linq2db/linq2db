using System;
using System.Data;
using System.Data.SqlTypes;
using System.Linq.Expressions;
using System.Xml;
using System.Xml.Linq;

namespace LinqToDB.DataProvider.SqlCe
{
	using Mapping;
	using SqlProvider;

	public class SqlCeDataProvider : DynamicDataProviderBase
	{
		public SqlCeDataProvider()
			: this(ProviderName.SqlCe, new SqlCeMappingSchema())
		{
		}

		protected SqlCeDataProvider(string name, MappingSchema mappingSchema)
			: base(name, mappingSchema)
		{
			SqlProviderFlags.IsSubQueryColumnSupported = false;

			SetCharField("NChar", (r,i) => r.GetString(i).TrimEnd());

			SetTypes("System.Data.SqlServerCe", "SqlCeConnection", "SqlCeDataReader");
		}

		protected override void OnConnectionTypeCreated()
		{
			_setNText     = GetSetParameter(SqlDbType.NText);
			_setNChar     = GetSetParameter(SqlDbType.NChar);
			_setNVarChar  = GetSetParameter(SqlDbType.NVarChar);
			_setTimestamp = GetSetParameter(SqlDbType.Timestamp);
			_setBinary    = GetSetParameter(SqlDbType.Binary);
			_setVarBinary = GetSetParameter(SqlDbType.VarBinary);
			_setImage     = GetSetParameter(SqlDbType.Image);
			_setDateTime  = GetSetParameter(SqlDbType.DateTime );
			_setMoney     = GetSetParameter(SqlDbType.Money);
			_setBoolean   = GetSetParameter(SqlDbType.Bit);
		}

		static Action<IDbDataParameter> _setNText;
		static Action<IDbDataParameter> _setNChar;
		static Action<IDbDataParameter> _setNVarChar;
		static Action<IDbDataParameter> _setTimestamp;
		static Action<IDbDataParameter> _setBinary;
		static Action<IDbDataParameter> _setVarBinary;
		static Action<IDbDataParameter> _setImage;
		static Action<IDbDataParameter> _setDateTime;
		static Action<IDbDataParameter> _setMoney;
		static Action<IDbDataParameter> _setBoolean;

		Action<IDbDataParameter> GetSetParameter(SqlDbType value)
		{
			var pType  = ConnectionType.Assembly.GetType(ConnectionType.Namespace + ".SqlCeParameter", true);

			var p = Expression.Parameter(typeof(IDbDataParameter));
			var l = Expression.Lambda<Action<IDbDataParameter>>(
				Expression.Assign(
					Expression.PropertyOrField(
						Expression.Convert(p, pType),
						"SqlDbType"),
					Expression.Constant(value)),
				p);

			return l.Compile();
		}

		#region Overrides

		public override ISqlProvider CreateSqlProvider()
		{
			return new SqlCeSqlProvider(SqlProviderFlags);
		}

		public override SchemaProvider.ISchemaProvider GetSchemaProvider()
		{
			return new SqlCeSchemaProvider();
		}

		public override void SetParameter(IDbDataParameter parameter, string name, DataType dataType, object value)
		{
			switch (dataType)
			{
				case DataType.Xml :
					dataType = DataType.NVarChar;

					if (value is SqlXml)
					{
						var xml = (SqlXml)value;
						value = xml.IsNull ? null : xml.Value;
					}
					else if (value is XDocument)   value = value.ToString();
					else if (value is XmlDocument) value = ((XmlDocument)value).InnerXml;

					break;
			}

			base.SetParameter(parameter, name, dataType, value);
		}

		protected override void SetParameterType(IDbDataParameter parameter, DataType dataType)
		{
			switch (dataType)
			{
				case DataType.SByte      : parameter.DbType    = DbType.Int16;        break;
				case DataType.UInt16     : parameter.DbType    = DbType.Int32;        break;
				case DataType.UInt32     : parameter.DbType    = DbType.Int64;        break;
				case DataType.UInt64     : parameter.DbType    = DbType.Decimal;      break;
				case DataType.VarNumeric : parameter.DbType    = DbType.Decimal;      break;
				case DataType.Text       :
				case DataType.NText      : _setNText    (parameter); break;
				case DataType.Char       :
				case DataType.NChar      : _setNChar    (parameter); break;
				case DataType.VarChar    :
				case DataType.NVarChar   : _setNVarChar (parameter); break;
				case DataType.Timestamp  : _setTimestamp(parameter); break;
				case DataType.Binary     : _setBinary   (parameter); break;
				case DataType.VarBinary  : _setVarBinary(parameter); break;
				case DataType.Image      : _setImage    (parameter); break;
				case DataType.DateTime   :
				case DataType.DateTime2  : _setDateTime (parameter); break;
				case DataType.Money      : _setMoney    (parameter); break;
				case DataType.Boolean    : _setBoolean  (parameter); break;
				default                  :
					base.SetParameterType(parameter, dataType);
					break;
			}
		}

		#endregion
	}
}
