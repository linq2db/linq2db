using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.Linq;
using System.IO;
using System.Linq.Expressions;
using System.Xml;
using System.Xml.Linq;

namespace LinqToDB.DataProvider
{
	using Common;
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
				AcceptsTakeAsParameter         = true,
				IsTakeSupported                = true,
				IsSkipSupported                = true,
				IsSubQueryTakeSupported        = true,
				IsSubQueryColumnSupported      = true,
				IsCountSubQuerySupported       = true,
				IsInsertOrUpdateSupported      = true,
				CanCombineParameters           = true,
				MaxInListValuesCount           = int.MaxValue,
				IsGroupByExpressionSupported   = true,
				IsDistinctOrderBySupported     = true,
				IsUpdateSetTableAliasSupported = true,
				TakeHintsSupported             = null,
				IsCrossJoinSupported           = true,
				IsInnerJoinAsCrossSupported    = true
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

		public static Func<IDataProvider,IDbConnection,IDbConnection> OnConnectionCreated { get; set; }

		public IDbConnection CreateConnection(string connectionString)
		{
			var connection = CreateConnectionInternal(connectionString);

			if (OnConnectionCreated != null)
				connection = OnConnectionCreated(this, connection);

			return connection;
		}

		protected abstract IDbConnection CreateConnectionInternal (string connectionString);
		public    abstract ISqlBuilder   CreateSqlBuilder();
		public    abstract ISqlOptimizer GetSqlOptimizer ();

		public virtual void InitCommand(DataConnection dataConnection, CommandType commandType, string commandText, DataParameter[] parameters)
		{
			dataConnection.Command.CommandType = commandType;

			if (dataConnection.Command.Parameters.Count != 0)
				dataConnection.Command.Parameters.Clear();

			dataConnection.Command.CommandText = commandText;
		}

		public virtual void DisposeCommand(DataConnection dataConnection)
		{
			dataConnection.Command.Dispose();
		}

		public virtual object GetConnectionInfo(DataConnection dataConnection, string parameterName)
		{
			return null;
		}

		public virtual CommandBehavior GetCommandBehavior(CommandBehavior commandBehavior)
		{
			return commandBehavior;
		}

		#endregion

		#region Helpers

		public readonly ConcurrentDictionary<ReaderInfo,Expression> ReaderExpressions = new ConcurrentDictionary<ReaderInfo,Expression>();

		protected void SetCharField(string dataTypeName, Expression<Func<IDataReader,int,string>> expr)
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

		protected void SetToType<TP,T,TF>(Expression<Func<TP,int,T>> expr)
		{
			ReaderExpressions[new ReaderInfo { ToType = typeof(T), FieldType = typeof(TF) }] = expr;
		}

		#endregion

		#region GetReaderExpression

		public virtual Expression GetReaderExpression(MappingSchema mappingSchema, IDataReader reader, int idx, Expression readerExpression, Type toType)
		{
			var fieldType    = ((DbDataReader)reader).GetFieldType(idx);
			var providerType = ((DbDataReader)reader).GetProviderSpecificFieldType(idx);
			var typeName     = ((DbDataReader)reader).GetDataTypeName(idx);

			if (fieldType == null)
			{
				throw new LinqToDBException("Can't create '{0}' type or '{1}' specific type for {2}.".Args(
					typeName,
					providerType,
					((DbDataReader)reader).GetName(idx)));
			}

#if DEBUG1
			Debug.WriteLine("ToType                ProviderFieldType     FieldType             DataTypeName          Expression");
			Debug.WriteLine("--------------------- --------------------- --------------------- --------------------- ---------------------");
			Debug.WriteLine("{0,-21} {1,-21} {2,-21} {3,-21}".Args(
				toType       == null ? "(null)" : toType.Name,
				providerType == null ? "(null)" : providerType.Name,
				fieldType.Name,
				typeName ?? "(null)"));
			Debug.WriteLine("--------------------- --------------------- --------------------- --------------------- ---------------------");

			foreach (var ex in ReaderExpressions)
			{
				Debug.WriteLine("{0,-21} {1,-21} {2,-21} {3,-21} {4}"
					.Args(
						ex.Key.ToType            == null ? null : ex.Key.ToType.Name,
						ex.Key.ProviderFieldType == null ? null : ex.Key.ProviderFieldType.Name,
						ex.Key.FieldType         == null ? null : ex.Key.FieldType.Name,
						ex.Key.DataTypeName,
						ex.Value));
			}
#endif

			Expression expr;

			if (FindExpression(new ReaderInfo { ToType = toType, ProviderFieldType = providerType, FieldType = fieldType, DataTypeName = typeName }, out expr) ||
			    FindExpression(new ReaderInfo { ToType = toType, ProviderFieldType = providerType, FieldType = fieldType                          }, out expr) ||
			    FindExpression(new ReaderInfo { ToType = toType, ProviderFieldType = providerType                                                 }, out expr) ||
			    FindExpression(new ReaderInfo {                  ProviderFieldType = providerType                                                 }, out expr) ||
			    FindExpression(new ReaderInfo {                  ProviderFieldType = providerType, FieldType = fieldType, DataTypeName = typeName }, out expr) ||
			    FindExpression(new ReaderInfo {                  ProviderFieldType = providerType, FieldType = fieldType                          }, out expr) ||
			    FindExpression(new ReaderInfo { ToType = toType,                                   FieldType = fieldType, DataTypeName = typeName }, out expr) ||
			    FindExpression(new ReaderInfo { ToType = toType,                                   FieldType = fieldType                          }, out expr) ||
			    FindExpression(new ReaderInfo {                                                    FieldType = fieldType, DataTypeName = typeName }, out expr) ||
			    FindExpression(new ReaderInfo { ToType = toType                                                                                   }, out expr) ||
			    FindExpression(new ReaderInfo {                                                    FieldType = fieldType                          }, out expr))
				return expr;

			var getValueMethodInfo = MemberHelper.MethodOf<IDataReader>(r => r.GetValue(0));
			return Expression.Convert(
				Expression.Call(readerExpression, getValueMethodInfo, Expression.Constant(idx)),
				fieldType);
		}

		protected bool FindExpression(ReaderInfo info, out Expression expr)
		{
#if DEBUG1
				Debug.WriteLine("{0,-21} {1,-21} {2,-21} {3,-21}"
					.Args(
						info.ToType            == null ? null : info.ToType.Name,
						info.ProviderFieldType == null ? null : info.ProviderFieldType.Name,
						info.FieldType         == null ? null : info.FieldType.Name,
						info.DataTypeName));
#endif

			if (ReaderExpressions.TryGetValue(info, out expr))
			{
#if DEBUG1
				Debug.WriteLine("ReaderExpression found: {0}".Args(expr));
#endif
				return true;
			}
			
			return false;
		}

		public virtual bool? IsDBNullAllowed(IDataReader reader, int idx)
		{
#if !NETSTANDARD
			var st = ((DbDataReader)reader).GetSchemaTable();
			return st == null || (bool)st.Rows[idx]["AllowDBNull"];
#else
			return true;
#endif
		}

		#endregion

		#region SetParameter

		public virtual void SetParameter(IDbDataParameter parameter, string name, DataType dataType, object value)
		{
			switch (dataType)
			{
				case DataType.Char      :
				case DataType.NChar     :
				case DataType.VarChar   :
				case DataType.NVarChar  :
				case DataType.Text      :
				case DataType.NText     :
					if      (value is DateTimeOffset) value = ((DateTimeOffset)value).ToString("yyyy-MM-ddTHH:mm:ss.ffffff zzz");
					else if (value is DateTime)
					{
						var dt = (DateTime)value;
						value = dt.ToString(
							dt.Millisecond == 0
								? dt.Hour == 0 && dt.Minute == 0 && dt.Second == 0
									? "yyyy-MM-dd"
									: "yyyy-MM-ddTHH:mm:ss"
								: "yyyy-MM-ddTHH:mm:ss.fff");
					}
					else if (value is TimeSpan)
					{
						var ts = (TimeSpan)value;
						value = ts.ToString(
							ts.Days > 0
								? ts.Milliseconds > 0
									? "d\\.hh\\:mm\\:ss\\.fff"
									: "d\\.hh\\:mm\\:ss"
								: ts.Milliseconds > 0
									? "hh\\:mm\\:ss\\.fff"
									: "hh\\:mm\\:ss");
					}
					break;
				case DataType.Image     :
				case DataType.Binary    :
				case DataType.Blob      :
				case DataType.VarBinary :
					if (value is Binary) value = ((Binary)value).ToArray();
					break;
				case DataType.Int64     :
					if (value is TimeSpan) value = ((TimeSpan)value).Ticks;
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

		public virtual Type ConvertParameterType(Type type, DataType dataType)
		{
			switch (dataType)
			{
				case DataType.Char      :
				case DataType.NChar     :
				case DataType.VarChar   :
				case DataType.NVarChar  :
				case DataType.Text      :
				case DataType.NText     :
					if (type == typeof(DateTimeOffset)) return typeof(string);
					break;
				case DataType.Image     :
				case DataType.Binary    :
				case DataType.Blob      :
				case DataType.VarBinary :
					if (type == typeof(Binary)) return typeof(byte[]);
					break;
				case DataType.Int64     :
					if (type == typeof(TimeSpan)) return typeof(long);
					break;
				case DataType.Xml       :
					if (type == typeof(XDocument) ||
						type == typeof(XmlDocument)) return typeof(string);
					break;
			}

			return type;
		}

		public abstract bool            IsCompatibleConnection(IDbConnection connection);
#if !NETSTANDARD
		public abstract ISchemaProvider GetSchemaProvider     ();
#endif

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

		public virtual BulkCopyRowsCopied BulkCopy<T>(DataConnection dataConnection, BulkCopyOptions options, IEnumerable<T> source)
		{
			return new BasicBulkCopy().BulkCopy(options.BulkCopyType, dataConnection, options, source);
		}

#endregion

#region Merge

		[Obsolete("Use new Merge API. TODO: link to migration wiki-page")]
		public virtual int Merge<T>(DataConnection dataConnection, Expression<Func<T,bool>> deletePredicate, bool delete, IEnumerable<T> source,
			string tableName, string databaseName, string schemaName)
			where T : class
		{
			return new BasicMerge().Merge(dataConnection, deletePredicate, delete, source, tableName, databaseName, schemaName);
		}

		public int Merge<TTarget, TSource>(DataConnection dataConnection, IMerge<TTarget, TSource> merge)
			where TTarget : class
			where TSource : class
		{
			if (dataConnection == null)
				throw new ArgumentNullException("dataConnection");

			if (merge == null)
				throw new ArgumentNullException("merge");

			var builder = GetMergeBuilder(merge);

			builder.Validate();

			var cmd = builder.BuildCommand();

			return dataConnection.Execute(cmd, builder.Parameters);
		}

		protected virtual BasicMergeBuilder<TTarget, TSource> GetMergeBuilder<TTarget, TSource>(IMerge<TTarget, TSource> merge)
			where TTarget : class
			where TSource : class
		{
			return new UnsupportedMergeBuilder<TTarget, TSource>(merge, Name);
		}

		#endregion
	}
}
