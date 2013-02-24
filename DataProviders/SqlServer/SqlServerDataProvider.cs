using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Linq;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

using Microsoft.SqlServer.Types;

namespace LinqToDB.DataProvider
{
	using Data;
	using Mapping;
	using SchemaProvider;
	using SqlProvider;

	public class SqlServerDataProvider : DataProviderBase
	{
		#region Init

		public SqlServerDataProvider(string name, SqlServerVersion version, MappingSchema mappingSchema)
			: base(name, mappingSchema)
		{
			Version = version;

			SetCharField("char",  (r,i) => r.GetString(i).TrimEnd());
			SetCharField("nchar", (r,i) => r.GetString(i).TrimEnd());

			SetProviderField<SqlDataReader,SqlBinary  ,SqlBinary  >((r,i) => r.GetSqlBinary  (i));
			SetProviderField<SqlDataReader,SqlBoolean ,SqlBoolean >((r,i) => r.GetSqlBoolean (i));
			SetProviderField<SqlDataReader,SqlByte    ,SqlByte    >((r,i) => r.GetSqlByte    (i));
			SetProviderField<SqlDataReader,SqlDateTime,SqlDateTime>((r,i) => r.GetSqlDateTime(i));
			SetProviderField<SqlDataReader,SqlDecimal ,SqlDecimal >((r,i) => r.GetSqlDecimal (i));
			SetProviderField<SqlDataReader,SqlDouble  ,SqlDouble  >((r,i) => r.GetSqlDouble  (i));
			SetProviderField<SqlDataReader,SqlGuid    ,SqlGuid    >((r,i) => r.GetSqlGuid    (i));
			SetProviderField<SqlDataReader,SqlInt16   ,SqlInt16   >((r,i) => r.GetSqlInt16   (i));
			SetProviderField<SqlDataReader,SqlInt32   ,SqlInt32   >((r,i) => r.GetSqlInt32   (i));
			SetProviderField<SqlDataReader,SqlInt64   ,SqlInt64   >((r,i) => r.GetSqlInt64   (i));
			SetProviderField<SqlDataReader,SqlMoney   ,SqlMoney   >((r,i) => r.GetSqlMoney   (i));
			SetProviderField<SqlDataReader,SqlSingle  ,SqlSingle  >((r,i) => r.GetSqlSingle  (i));
			SetProviderField<SqlDataReader,SqlString  ,SqlString  >((r,i) => r.GetSqlString  (i));
			SetProviderField<SqlDataReader,SqlXml     ,SqlXml     >((r,i) => r.GetSqlXml     (i));

			SetProviderField<SqlDataReader,DateTimeOffset>((r,i) => r.GetDateTimeOffset(i));
			SetProviderField<SqlDataReader,TimeSpan>      ((r,i) => r.GetTimeSpan      (i));
		}

		#endregion

		#region Public Properties

		public override Type ConnectionType { get { return typeof(SqlConnection);  } }
		public override Type DataReaderType { get { return typeof(SqlDataReader);  } }

		public SqlServerVersion Version { get; private set; }

		#endregion

		#region Overrides

		public override IDbConnection CreateConnection(string connectionString)
		{
			return new SqlConnection(connectionString);
		}

		public override ISqlProvider CreateSqlProvider()
		{
			return Version == SqlServerVersion.v2005 ?
				new SqlServer2005SqlProvider(SqlProviderFlags) as ISqlProvider:
				new SqlServer2008SqlProvider(SqlProviderFlags);
		}

		public override DatabaseSchema GetSchema(DataConnection dataConnection)
		{
			return new SqlServerSchemaProvider().GetSchema(dataConnection);
		}

		static readonly ConcurrentDictionary<string,bool> _marsFlags = new ConcurrentDictionary<string,bool>();

		public override object GetConnectionInfo(DataConnection dataConnection, string parameterName)
		{
			switch (parameterName)
			{
				case "IsMarsEnabled" :
					if (dataConnection.ConnectionString != null)
					{
						bool flag;

						if (!_marsFlags.TryGetValue(dataConnection.Connection.ConnectionString, out flag))
						{
							flag = dataConnection.Connection.ConnectionString.Split(';')
								.Select(s => s.Split('='))
								.Where (s => s.Length == 2 && s[0].Trim().ToLower() == "multipleactiveresultsets")
								.Select(s => s[1].Trim().ToLower())
								.Any   (s => s == "true" || s == "1" || s == "yes");

							_marsFlags[dataConnection.Connection.ConnectionString] = flag;
						}

						return flag;
					}

					return false;
			}

			return null;
		}

		public override void SetParameter(IDbDataParameter parameter, string name, DataType dataType, object value)
		{
			switch (dataType)
			{
				case DataType.Image      :
				case DataType.Binary     :
				case DataType.VarBinary  :
					if (value is Binary) value = ((Binary)value).ToArray();
					break;
				case DataType.Xml        :
					     if (value is XDocument)   value = value.ToString();
					else if (value is XmlDocument) value = ((XmlDocument)value).InnerXml;
					break;
				case DataType.Udt        :
					{
						string s;
						if (value != null && _udtTypes.TryGetValue(value.GetType(), out s))
							((SqlParameter)parameter).UdtTypeName = s;
					}
					break;
			}

			parameter.ParameterName = name;
			SetParameterType(parameter, dataType);
			parameter.Value = value ?? DBNull.Value;
		}

		protected override void SetParameterType(IDbDataParameter parameter, DataType dataType)
		{
			switch (dataType)
			{
				case DataType.SByte         : parameter.DbType = DbType.Int16;   break;
				case DataType.UInt16        : parameter.DbType = DbType.Int32;   break;
				case DataType.UInt32        : parameter.DbType = DbType.Int64;   break;
				case DataType.UInt64        : parameter.DbType = DbType.Decimal; break;
				case DataType.VarNumeric    : parameter.DbType = DbType.Decimal; break;
				case DataType.DateTime2     :
					parameter.DbType = Version == SqlServerVersion.v2005 ? DbType.DateTime : DbType.DateTime2;
					break;
				case DataType.Text          : ((SqlParameter)parameter).SqlDbType = SqlDbType.Text;          break;
				case DataType.NText         : ((SqlParameter)parameter).SqlDbType = SqlDbType.NText;         break;
				case DataType.Binary        : ((SqlParameter)parameter).SqlDbType = SqlDbType.Binary;        break;
				case DataType.VarBinary     : ((SqlParameter)parameter).SqlDbType = SqlDbType.VarBinary;     break;
				case DataType.Image         : ((SqlParameter)parameter).SqlDbType = SqlDbType.Image;         break;
				case DataType.Money         : ((SqlParameter)parameter).SqlDbType = SqlDbType.Money;         break;
				case DataType.SmallMoney    : ((SqlParameter)parameter).SqlDbType = SqlDbType.SmallMoney;    break;
				case DataType.Date          : ((SqlParameter)parameter).SqlDbType = SqlDbType.Date;          break;
				case DataType.Time          : ((SqlParameter)parameter).SqlDbType = SqlDbType.Time;          break;
				case DataType.SmallDateTime : ((SqlParameter)parameter).SqlDbType = SqlDbType.SmallDateTime; break;
				case DataType.Timestamp     : ((SqlParameter)parameter).SqlDbType = SqlDbType.Timestamp;     break;
				default                     : base.SetParameterType(parameter, dataType);                    break;
			}
		}

		#endregion

		#region Udt support

		static readonly ConcurrentDictionary<Type,string> _udtTypes = new ConcurrentDictionary<Type,string>(new Dictionary<Type,string>
		{
			{ typeof(SqlGeography),   "geography"   },
			{ typeof(SqlGeometry),    "geometry"    },
			{ typeof(SqlHierarchyId), "hierarchyid" },
		});

		public void AddUdtType(Type type, string udtName)
		{
			MappingSchema.SetScalarType(type);

			_udtTypes[type] = udtName;
		}

		public void AddUdtType<T>(string udtName, T defaultValue, DataType dataType = DataType.Undefined)
		{
			MappingSchema.AddScalarType(typeof(T), defaultValue, dataType);

			_udtTypes[typeof(T)] = udtName;
		}

		#endregion
	}
}
