using System;
using System.Data;
using System.Data.SqlTypes;
using System.Linq.Expressions;
using System.Xml;
using System.Xml.Linq;

namespace LinqToDB.DataProvider.SqlCe
{
	using Common;
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
		}

		public    override string ConnectionNamespace { get { return "System.Data.SqlServerCe"; } }
		protected override string ConnectionTypeName  { get { return "{0}.{1}, {0}".Args(ConnectionNamespace, "SqlCeConnection"); } }
		protected override string DataReaderTypeName  { get { return "{0}.{1}, {0}".Args(ConnectionNamespace, "SqlCeDataReader"); } }

		protected override void OnConnectionTypeCreated(Type connectionType)
		{
			_setNText     = GetSetParameter(connectionType, SqlDbType.NText);
			_setNChar     = GetSetParameter(connectionType, SqlDbType.NChar);
			_setNVarChar  = GetSetParameter(connectionType, SqlDbType.NVarChar);
			_setTimestamp = GetSetParameter(connectionType, SqlDbType.Timestamp);
			_setBinary    = GetSetParameter(connectionType, SqlDbType.Binary);
			_setVarBinary = GetSetParameter(connectionType, SqlDbType.VarBinary);
			_setImage     = GetSetParameter(connectionType, SqlDbType.Image);
			_setDateTime  = GetSetParameter(connectionType, SqlDbType.DateTime );
			_setMoney     = GetSetParameter(connectionType, SqlDbType.Money);
			_setBoolean   = GetSetParameter(connectionType, SqlDbType.Bit);
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

		static Action<IDbDataParameter> GetSetParameter(Type connectionType, SqlDbType value)
		{
			var pType  = connectionType.Assembly.GetType(connectionType.Namespace + ".SqlCeParameter", true);

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
				case DataType.SByte      : parameter.DbType    = DbType.Int16;   break;
				case DataType.UInt16     : parameter.DbType    = DbType.Int32;   break;
				case DataType.UInt32     : parameter.DbType    = DbType.Int64;   break;
				case DataType.UInt64     : parameter.DbType    = DbType.Decimal; break;
				case DataType.VarNumeric : parameter.DbType    = DbType.Decimal; break;
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

		public override void CreateDatabase([JetBrains.Annotations.NotNull] string databaseName)
		{
			if (databaseName == null) throw new ArgumentNullException("databaseName");

			if (!databaseName.ToLower().EndsWith(".sdf"))
				databaseName += ".sdf";

			dynamic eng = Activator.CreateInstance(
				GetConnectionType().Assembly.GetType("System.Data.SqlServerCe.SqlCeEngine"),
				string.Format("Data Source={0}", databaseName));

			eng.CreateDatabase();

			var disp = eng as IDisposable;

			if (disp != null)
				disp.Dispose();
		}
	}
}
