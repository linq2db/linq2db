using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;

namespace LinqToDB.DataProvider
{
	using Data;
	using Expressions;
	using Mapping;
	using SqlProvider;

	public abstract class DataProviderBase : IDataProvider
	{
		#region .ctor

		protected DataProviderBase(string name, MappingSchema mappingSchema)
		{
			Name             = name;
			MappingSchema    = mappingSchema;
			SqlProviderFlags = new SqlProviderFlags();

			SetField<IDataReader,bool>    ((r,i) => r.GetBoolean (i));
			SetField<IDataReader,byte>    ((r,i) => r.GetByte    (i));
			SetField<IDataReader,char>    ((r,i) => r.GetChar    (i));
			SetField<IDataReader,short>   ((r,i) => r.GetInt16   (i));
			SetField<IDataReader,int>     ((r,i) => r.GetInt32   (i));
			SetField<IDataReader,long>    ((r,i) => r.GetInt64   (i));
			SetField<IDataReader,float>   ((r,i) => r.GetFloat   (i));
			SetField<IDataReader,double>  ((r,i) => r.GetDouble  (i));
			SetField<IDataReader,string>  ((r,i) => r.GetString  (i));
			SetField<IDataReader,decimal> ((r,i) => r.GetDecimal (i));
			SetField<IDataReader,DateTime>((r,i) => r.GetDateTime(i));
			SetField<IDataReader,Guid>    ((r,i) => r.GetGuid    (i));
			SetField<IDataReader,byte[]>  ((r,i) => (byte[])r.GetValue(i));
		}

		#endregion

		#region Public Members

		public          string           Name             { get; private set; }
		public abstract Type             ConnectionType   { get; }
		public abstract Type             DataReaderType   { get; }
		public virtual  MappingSchema    MappingSchema    { get; private set; }
		public          SqlProviderFlags SqlProviderFlags { get; private set; }

		public abstract IDbConnection    CreateConnection (string connectionString);
		public abstract ISqlProvider     CreateSqlProvider();

		public virtual void InitCommand(DataConnection dataConnection)
		{
			if (dataConnection.Command.Parameters.Count != 0)
				dataConnection.Command.Parameters.Clear();
		}

		public virtual object GetConnectionInfo(DataConnection dataConnection, string parameterName)
		{
			return null;
		}

		#endregion

		#region GetReaderExpression

		public readonly ConcurrentDictionary<ReaderInfo,Expression> ReaderExpressions = new ConcurrentDictionary<ReaderInfo,Expression>();

		protected void SetCharField(string dataTypeName, Expression<Func<IDataReader, int, string>> expr)
		{
			ReaderExpressions[new ReaderInfo { FieldType = typeof(string), DataTypeName = dataTypeName }] = expr;
		}

		protected void SetField<TP,T>(Expression<Func<TP,int,T>> expr)
		{
			ReaderExpressions[new ReaderInfo { FieldType = typeof(T) }] = expr;
		}

		protected void SetField<TP,T>(string dataTypeName, Expression<Func<TP,int,T>> expr)
		{
			ReaderExpressions[new ReaderInfo { FieldType = typeof(T), DataTypeName = dataTypeName }] = expr;
		}

		protected void SetProviderField<TP,T>(Expression<Func<TP,int,T>> expr)
		{
			ReaderExpressions[new ReaderInfo { ProviderFieldType = typeof(T) }] = expr;
		}

		protected void SetProviderField<TP,T,TS>(Expression<Func<TP,int,T>> expr)
		{
			ReaderExpressions[new ReaderInfo { ToType = typeof(T), ProviderFieldType = typeof(TS) }] = expr;
		}

		protected void SetProviderField2<TP,T,TS>(Expression<Func<TP,int,TS>> expr)
		{
			ReaderExpressions[new ReaderInfo { ToType = typeof(T), ProviderFieldType = typeof(TS) }] = expr;
		}

		protected void SetToTypeField<TP,T>(Expression<Func<TP,int,T>> expr)
		{
			ReaderExpressions[new ReaderInfo { ToType = typeof(T) }] = expr;
		}

		public virtual Expression GetReaderExpression(MappingSchema mappingSchema, IDataReader reader, int idx, Expression readerExpression, Type toType)
		{
			var fieldType    = ((DbDataReader)reader).GetFieldType(idx);
			var providerType = ((DbDataReader)reader).GetProviderSpecificFieldType(idx);
			var typeName     = ((DbDataReader)reader).GetDataTypeName(idx);

			Expression expr;

			if (ReaderExpressions.TryGetValue(new ReaderInfo { ToType = toType, ProviderFieldType = providerType, FieldType = fieldType, DataTypeName = typeName }, out expr) ||
			    ReaderExpressions.TryGetValue(new ReaderInfo { ToType = toType, ProviderFieldType = providerType, FieldType = fieldType                          }, out expr) ||
			    ReaderExpressions.TryGetValue(new ReaderInfo { ToType = toType, ProviderFieldType = providerType                                                 }, out expr) ||
			    ReaderExpressions.TryGetValue(new ReaderInfo {                  ProviderFieldType = providerType                                                 }, out expr) ||
			    ReaderExpressions.TryGetValue(new ReaderInfo {                  ProviderFieldType = providerType, FieldType = fieldType, DataTypeName = typeName }, out expr) ||
			    ReaderExpressions.TryGetValue(new ReaderInfo {                  ProviderFieldType = providerType, FieldType = fieldType                          }, out expr) ||
			    ReaderExpressions.TryGetValue(new ReaderInfo { ToType = toType,                                   FieldType = fieldType, DataTypeName = typeName }, out expr) ||
			    ReaderExpressions.TryGetValue(new ReaderInfo { ToType = toType,                                   FieldType = fieldType                          }, out expr) ||
			    ReaderExpressions.TryGetValue(new ReaderInfo {                                                    FieldType = fieldType, DataTypeName = typeName }, out expr) ||
			    ReaderExpressions.TryGetValue(new ReaderInfo { ToType = toType                                                                                   }, out expr) ||
			    ReaderExpressions.TryGetValue(new ReaderInfo {                                                    FieldType = fieldType                          }, out expr))
				return expr;

			return (Expression<Func<IDataReader,int,object>>)((r,i) => r.GetValue(i));
		}

		protected virtual MethodInfo GetReaderMethodInfo(IDataRecord reader, int idx, Type toType)
		{
			var type = reader.GetFieldType(idx);

			switch (Type.GetTypeCode(type))
			{
				case TypeCode.Boolean  : return MemberHelper.MethodOf<IDataRecord>(r => r.GetBoolean (0));
				case TypeCode.Byte     : return MemberHelper.MethodOf<IDataRecord>(r => r.GetByte    (0));
				case TypeCode.Char     : return MemberHelper.MethodOf<IDataRecord>(r => r.GetChar    (0));
				case TypeCode.Int16    : return MemberHelper.MethodOf<IDataRecord>(r => r.GetInt16   (0));
				case TypeCode.Int32    : return MemberHelper.MethodOf<IDataRecord>(r => r.GetInt32   (0));
				case TypeCode.Int64    : return MemberHelper.MethodOf<IDataRecord>(r => r.GetInt64   (0));
				case TypeCode.Single   : return MemberHelper.MethodOf<IDataRecord>(r => r.GetFloat   (0));
				case TypeCode.Double   : return MemberHelper.MethodOf<IDataRecord>(r => r.GetDouble  (0));
				case TypeCode.String   : return MemberHelper.MethodOf<IDataRecord>(r => r.GetString  (0));
				case TypeCode.Decimal  : return MemberHelper.MethodOf<IDataRecord>(r => r.GetDecimal (0));
				case TypeCode.DateTime : return MemberHelper.MethodOf<IDataRecord>(r => r.GetDateTime(0));
			}

			if (type == typeof(Guid))   return MemberHelper.MethodOf<IDataRecord>(r => r.GetGuid(0));
			if (type == typeof(byte[])) return MemberHelper.MethodOf<IDataRecord>(r => r.GetValue(0));

			return null;
		}

		public virtual bool? IsDBNullAllowed(IDataReader reader, int idx)
		{
			var st = ((DbDataReader)reader).GetSchemaTable();
			return st == null || (bool)st.Rows[idx]["AllowDBNull"];
		}

		#endregion

		#region SetParameter

		public virtual void SetParameter(IDbDataParameter parameter, string name, DataType dataType, object value)
		{
			switch (dataType)
			{
				case DataType.Image     :
				case DataType.Binary    :
				case DataType.VarBinary :
					if (value is Binary) value = ((Binary)value).ToArray();
					break;
				case DataType.Xml       :
					     if (value is XDocument)   value = value.ToString();
					else if (value is XmlDocument) value = ((XmlDocument)value).InnerXml;
					break;
			}

			parameter.ParameterName = name;
			SetParameterType(parameter, dataType);
			parameter.Value = value ?? DBNull.Value;
		}

		protected virtual void SetParameterType(IDbDataParameter parameter, DataType dataType)
		{
			DbType dbType;

			switch (dataType)
			{
				case DataType.Char           : dbType = DbType.AnsiStringFixedLength; break;
				case DataType.VarChar        : dbType = DbType.AnsiString;            break;
				case DataType.NChar          : dbType = DbType.StringFixedLength;     break;
				case DataType.NVarChar       : dbType = DbType.String;                break;
				case DataType.VarBinary      : dbType = DbType.Binary;                break;
				case DataType.Boolean        : dbType = DbType.Boolean;               break;
				case DataType.SByte          : dbType = DbType.SByte;                 break;
				case DataType.Int16          : dbType = DbType.Int16;                 break;
				case DataType.Int32          : dbType = DbType.Int32;                 break;
				case DataType.Int64          : dbType = DbType.Int64;                 break;
				case DataType.Byte           : dbType = DbType.Byte;                  break;
				case DataType.UInt16         : dbType = DbType.UInt16;                break;
				case DataType.UInt32         : dbType = DbType.UInt32;                break;
				case DataType.UInt64         : dbType = DbType.UInt64;                break;
				case DataType.Single         : dbType = DbType.Single;                break;
				case DataType.Double         : dbType = DbType.Double;                break;
				case DataType.Decimal        : dbType = DbType.Decimal;               break;
				case DataType.Guid           : dbType = DbType.Guid;                  break;
				case DataType.Date           : dbType = DbType.Date;                  break;
				case DataType.Time           : dbType = DbType.Time;                  break;
				case DataType.DateTime       : dbType = DbType.DateTime;              break;
				case DataType.DateTime2      : dbType = DbType.DateTime2;             break;
				case DataType.DateTimeOffset : dbType = DbType.DateTimeOffset;        break;
				case DataType.Variant        : dbType = DbType.Object;                break;
				case DataType.VarNumeric     : dbType = DbType.VarNumeric;            break;
				default                      : return;
			}

			parameter.DbType = dbType;
		}

		#endregion

		#region BulkCopy

		public virtual int BulkCopy<T>(DataConnection dataConnection, int maxBatchSize, IEnumerable<T> source)
		{
			var n = 0;

			foreach (var item in source)
			{
				dataConnection.Insert(item);
				n++;
			}

			return n;
		}

		#endregion
	}
}
