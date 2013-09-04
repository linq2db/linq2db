using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.Linq;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;

namespace LinqToDB.DataProvider
{
	using Data;
	using Expressions;
	using Mapping;
	using SchemaProvider;
	using SqlProvider;

	public abstract class DataProviderBase : IDataProvider
	{
		#region .ctor

		protected DataProviderBase(string name, MappingSchema mappingSchema)
		{
			Name             = name;
			MappingSchema    = mappingSchema;
			SqlProviderFlags = new SqlProviderFlags
			{
				AcceptsTakeAsParameter       = true,
				IsTakeSupported              = true,
				IsSkipSupported              = true,
				IsSubQueryTakeSupported      = true,
				IsSubQueryColumnSupported    = true,
				IsCountSubQuerySupported     = true,
				IsInsertOrUpdateSupported    = true,
				CanCombineParameters         = true,
				MaxInListValuesCount         = int.MaxValue,
				IsGroupByExpressionSupported = true,
			};

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

		public          string           Name                { get; private set; }
		public abstract string           ConnectionNamespace { get; }
		public abstract Type             DataReaderType      { get; }
		public virtual  MappingSchema    MappingSchema       { get; private set; }
		public          SqlProviderFlags SqlProviderFlags    { get; private set; }

		public abstract IDbConnection    CreateConnection (string connectionString);
		public abstract ISqlBuilder      CreateSqlBuilder();
		public abstract ISqlOptimizer    GetSqlOptimizer ();

		public virtual void InitCommand(DataConnection dataConnection)
		{
			dataConnection.Command.CommandType = CommandType.Text;

			if (dataConnection.Command.Parameters.Count != 0)
				dataConnection.Command.Parameters.Clear();
		}

		public virtual object GetConnectionInfo(DataConnection dataConnection, string parameterName)
		{
			return null;
		}

		#endregion

		#region Helpers

		public readonly ConcurrentDictionary<ReaderInfo,Expression> ReaderExpressions = new ConcurrentDictionary<ReaderInfo,Expression>();

		protected void SetCharField(string dataTypeName, Expression<Func<IDataReader, int, string>> expr)
		{
			ReaderExpressions[new ReaderInfo { FieldType = typeof(string), DataTypeName = dataTypeName }] = expr;
		}

		protected void SetField<TP,T>(Expression<Func<TP,int,T>> expr)
		{
			ReaderExpressions[new ReaderInfo { FieldType = typeof(T) }] = expr;
		}

		protected void SetProviderField<TP,T>(Expression<Func<TP,int,T>> expr)
		{
			ReaderExpressions[new ReaderInfo { ProviderFieldType = typeof(T) }] = expr;
		}

		protected void SetProviderField<TP,T,TS>(Expression<Func<TP,int,T>> expr)
		{
			ReaderExpressions[new ReaderInfo { ToType = typeof(T), ProviderFieldType = typeof(TS) }] = expr;
		}

		#endregion

		#region GetReaderExpression

		static readonly MethodInfo _getValueMethodInfo = MemberHelper.MethodOf<IDataReader>(r => r.GetValue(0));

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

			return Expression.Convert(
				Expression.Call(readerExpression, _getValueMethodInfo, Expression.Constant(idx)),
				fieldType);
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
				case DataType.Blob      :
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

		public virtual ISchemaProvider GetSchemaProvider()
		{
			throw new NotImplementedException();
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
				case DataType.Blob           :
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

		#region Create/Drop Database

		internal static void CreateFileDatabase(
			string databaseName,
			bool   deleteIfExists,
			string extension,
			Action<string> createDatabase)
		{
			databaseName = databaseName.Trim();

			if (!databaseName.ToLower().EndsWith(extension))
				databaseName += extension;

			if (File.Exists(databaseName))
			{
				if (!deleteIfExists)
					return;
				File.Delete(databaseName);
			}

			createDatabase(databaseName);
		}

		internal static void DropFileDatabase(string databaseName, string extension)
		{
			databaseName = databaseName.Trim();

			if (File.Exists(databaseName))
			{
				File.Delete(databaseName);
			}
			else
			{
				if (!databaseName.ToLower().EndsWith(extension))
				{
					databaseName += extension;

					if (File.Exists(databaseName))
						File.Delete(databaseName);
				}
			}
		}

		#endregion

		#region BulkCopy

		public virtual int BulkCopy<T>(DataConnection dataConnection, BulkCopyOptions options, IEnumerable<T> source)
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
