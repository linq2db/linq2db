using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.Linq;
using System.Data.SqlClient;
using System.Linq.Expressions;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;

using Microsoft.SqlServer.Types;

namespace LinqToDB.DataProvider
{
	using Expressions;
	using Mapping;

	abstract class SqlServerDataProvider : DataProviderBase
	{
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

		#region Init

		protected SqlServerDataProvider(SqlServerVersion version, MappingSchema mappingSchema)
			: base(mappingSchema)
		{
			Version = version;
		}

		#endregion

		#region Public Properties

		public override string Name           { get { return ProviderName.SqlServer; } }
		public override Type   ConnectionType { get { return typeof(SqlConnection);  } }

		public SqlServerVersion Version { get; private set; }

		#endregion

		#region Overrides

		public override IDbConnection CreateConnection(string connectionString)
		{
			return new SqlConnection(connectionString);
		}

		public override void Configure(string name, string value)
		{
			if (name == "version") switch (value)
			{
				case "2005" : Version = SqlServerVersion.v2005; break;
				case "2008" : Version = SqlServerVersion.v2008; break;
				case "2012" : Version = SqlServerVersion.v2012; break;
			}
		}

		public override Expression ConvertDataReader(Expression reader)
		{
			return Expression.Convert(reader, typeof(SqlDataReader));
		}

		public override Expression GetReaderExpression(MappingSchema mappingSchema, IDataReader reader, int idx, Expression readerExpression, Type toType)
		{
			var expr = base.GetReaderExpression(mappingSchema, reader, idx, readerExpression, toType);

			if (expr.Type == typeof(object))
			{
				var type = ((DbDataReader)reader).GetProviderSpecificFieldType(idx);

				if (type == typeof(SqlHierarchyId))
					expr = Expression.Convert(expr, type);
			}

			var name = ((SqlDataReader)reader).GetDataTypeName(idx);

			if (expr.Type == typeof(string) && (name == "char" || name == "nchar"))
				expr = Expression.Call(expr, MemberHelper.MethodOf<string>(s => s.Trim()));

			return expr;
		}

		protected override MethodInfo GetReaderMethodInfo(IDataRecord reader, int idx, Type toType)
		{
			var type = ((DbDataReader)reader).GetProviderSpecificFieldType(idx);

			//if (toType == type)
			//{
			//	if (type == typeof(SqlBinary))   return MemberHelper.MethodOf<SqlDataReader>(r => r.GetSqlBinary  (0));
			//	if (type == typeof(SqlBoolean))  return MemberHelper.MethodOf<SqlDataReader>(r => r.GetSqlBoolean (0));
			//	if (type == typeof(SqlByte))     return MemberHelper.MethodOf<SqlDataReader>(r => r.GetSqlByte    (0));
			//	if (type == typeof(SqlDateTime)) return MemberHelper.MethodOf<SqlDataReader>(r => r.GetSqlDateTime(0));
			//	if (type == typeof(SqlDecimal))  return MemberHelper.MethodOf<SqlDataReader>(r => r.GetSqlDecimal (0));
			//	if (type == typeof(SqlDouble))   return MemberHelper.MethodOf<SqlDataReader>(r => r.GetSqlDouble  (0));
			//	if (type == typeof(SqlGuid))     return MemberHelper.MethodOf<SqlDataReader>(r => r.GetSqlGuid    (0));
			//	if (type == typeof(SqlInt16))    return MemberHelper.MethodOf<SqlDataReader>(r => r.GetSqlInt16   (0));
			//	if (type == typeof(SqlInt32))    return MemberHelper.MethodOf<SqlDataReader>(r => r.GetSqlInt32   (0));
			//	if (type == typeof(SqlInt64))    return MemberHelper.MethodOf<SqlDataReader>(r => r.GetSqlInt64   (0));
			//	if (type == typeof(SqlMoney))    return MemberHelper.MethodOf<SqlDataReader>(r => r.GetSqlMoney   (0));
			//	if (type == typeof(SqlSingle))   return MemberHelper.MethodOf<SqlDataReader>(r => r.GetSqlSingle  (0));
			//	if (type == typeof(SqlString))   return MemberHelper.MethodOf<SqlDataReader>(r => r.GetSqlString  (0));
			//	if (type == typeof(SqlXml))      return MemberHelper.MethodOf<SqlDataReader>(r => r.GetSqlXml     (0));
			//}

			var mi = base.GetReaderMethodInfo(reader, idx, toType);

			if (mi != null)
				return mi;

			if (type == typeof(DateTimeOffset)) return MemberHelper.MethodOf<SqlDataReader>(r => r.GetDateTimeOffset(0));
			if (type == typeof(TimeSpan))       return MemberHelper.MethodOf<SqlDataReader>(r => r.GetTimeSpan      (0));

			return null;
		}

		public override bool? IsDBNullAllowed(IDataReader reader, int idx)
		{
			var st = ((SqlDataReader)reader).GetSchemaTable();
			return st == null || (bool)st.Rows[idx]["allowDBNull"];
		}

		public override void SetParameter(IDbDataParameter parameter, string name, DataType dataType, object value)
		{
			switch (dataType)
			{
				case DataType.SByte      : dataType = DataType.Int16;   break;
				case DataType.UInt16     : dataType = DataType.Int32;   break;
				case DataType.UInt32     : dataType = DataType.Int64;   break;
				case DataType.UInt64     : dataType = DataType.Decimal; break;
				case DataType.VarNumeric : dataType = DataType.Decimal; break;
				case DataType.Binary     :
				case DataType.VarBinary  :
					if (value is Binary) value = ((Binary)value).ToArray();
					break;
				case DataType.Xml        :
					     if (value is XDocument)   value = value.ToString();
					else if (value is XmlDocument) value = ((XmlDocument)value).InnerXml;
					break;
				case DataType.Udt        :
					if (value != null)
					{
						string s;
						if (_udtTypes.TryGetValue(value.GetType(), out s))
							((SqlParameter)parameter).UdtTypeName = s;
					}
					break;
				case DataType.Undefined  :
					     if (value is sbyte)  dataType = DataType.Int16;
					else if (value is ushort) dataType = DataType.Int32;
					else if (value is uint)   dataType = DataType.Int64;
					else if (value is ulong)  dataType = DataType.Decimal;
					else if (value is Binary)
					{
						dataType = DataType.VarBinary;
						value = ((Binary)value).ToArray();
					}
					else if (value is XDocument)
					{
						dataType = DataType.Xml;
						value = value.ToString();
					}
					else if (value is XmlDocument)
					{
						dataType = DataType.Xml;
						value = ((XmlDocument)value).InnerXml;
					}
					else
					{
						string s;
						if (_udtTypes.TryGetValue(value.GetType(), out s))
							((SqlParameter)parameter).UdtTypeName = s;
					}

					break;
			}

			base.SetParameter(parameter, name, dataType, value);
		}

		public override void SetParameterType(IDbDataParameter parameter, DataType dataType)
		{
			switch (dataType)
			{
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
	}
}
