using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.Linq;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

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
				AcceptsTakeAsParameter               = true,
				IsTakeSupported                      = true,
				IsSkipSupported                      = true,
				IsSubQueryTakeSupported              = true,
				IsSubQueryColumnSupported            = true,
				IsCountSubQuerySupported             = true,
				IsInsertOrUpdateSupported            = true,
				CanCombineParameters                 = true,
				MaxInListValuesCount                 = int.MaxValue,
				IsDistinctOrderBySupported           = true,
				IsSubQueryOrderBySupported           = false,
				IsUpdateSetTableAliasSupported       = true,
				TakeHintsSupported                   = null,
				IsCrossJoinSupported                 = true,
				IsInnerJoinAsCrossSupported          = true,
				IsOrderByAggregateFunctionsSupported = true,
				IsAllSetOperationsSupported          = false,
				IsDistinctSetOperationsSupported     = true,
				IsUpdateFromSupported                = true,
				AcceptsOuterExpressionInAggregate    = true,
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

		public          string           Name                  { get; }
		public abstract string?          ConnectionNamespace   { get; }
		public abstract Type             DataReaderType        { get; }
		public virtual  MappingSchema    MappingSchema         { get; }
		public          SqlProviderFlags SqlProviderFlags      { get; }
		public abstract TableOptions     SupportedTableOptions { get; }

		public static Func<IDataProvider,IDbConnection,IDbConnection>? OnConnectionCreated { get; set; }

		public IDbConnection CreateConnection(string connectionString)
		{
			var connection = CreateConnectionInternal(connectionString);

			if (OnConnectionCreated != null)
				connection = OnConnectionCreated(this, connection);

			return connection;
		}

		protected abstract IDbConnection CreateConnectionInternal (string connectionString);
		public    abstract ISqlBuilder   CreateSqlBuilder(MappingSchema mappingSchema);
		public    abstract ISqlOptimizer GetSqlOptimizer ();

		public virtual void InitCommand(DataConnection dataConnection, CommandType commandType, string commandText, DataParameter[]? parameters, bool withParameters)
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

		public virtual object? GetConnectionInfo(DataConnection dataConnection, string parameterName)
		{
			return null;
		}

		public virtual CommandBehavior GetCommandBehavior(CommandBehavior commandBehavior)
		{
			return commandBehavior;
		}

		public virtual IDisposable? ExecuteScope(DataConnection dataConnection)
		{
			return null;
		}

		#endregion

		#region Helpers

		public readonly ConcurrentDictionary<ReaderInfo,Expression> ReaderExpressions = new ();

		protected void SetCharField(string dataTypeName, Expression<Func<IDataReader,int,string>> expr)
		{
			ReaderExpressions[new ReaderInfo { FieldType = typeof(string), DataTypeName = dataTypeName }] = expr;
		}

		protected void SetCharFieldToType<T>(string dataTypeName, Expression<Func<IDataReader, int, string>> expr)
		{
			ReaderExpressions[new ReaderInfo { ToType = typeof(T), FieldType = typeof(string), DataTypeName = dataTypeName }] = expr;
		}

		protected void SetField<TP,T>(Expression<Func<TP,int,T>> expr)
		{
			ReaderExpressions[new ReaderInfo { FieldType = typeof(T) }] = expr;
		}

		protected void SetField<TP,T>(string dataTypeName, Expression<Func<TP,int,T>> expr)
		{
			ReaderExpressions[new ReaderInfo { FieldType = typeof(T), DataTypeName = dataTypeName }] = expr;
		}

		protected void SetField<TP, T>(string dataTypeName, Type fieldType, Expression<Func<TP, int, T>> expr)
		{
			ReaderExpressions[new ReaderInfo { FieldType = fieldType, DataTypeName = dataTypeName }] = expr;
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

		protected void SetToType<TP, T, TF>(string dataTypeName, Expression<Func<TP, int, T>> expr)
		{
			ReaderExpressions[new ReaderInfo { ToType = typeof(T), FieldType = typeof(TF), DataTypeName = dataTypeName }] = expr;
		}

		protected virtual string? NormalizeTypeName(string? typeName)
		{
			return typeName;
		}

		#endregion

		#region GetReaderExpression

		public virtual Expression GetReaderExpression(IDataReader reader, int idx, Expression readerExpression, Type toType)
		{
			var fieldType    = ((DbDataReader)reader).GetFieldType(idx);
			var providerType = ((DbDataReader)reader).GetProviderSpecificFieldType(idx);
			string? typeName = ((DbDataReader)reader).GetDataTypeName(idx);

			if (fieldType == null)
			{
				var name = ((DbDataReader)reader).GetName(idx);
				throw new LinqToDBException($"Can't create '{typeName}' type or '{providerType}' specific type for {name}.");
			}

			typeName = NormalizeTypeName(typeName);

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

			var dataReaderType = readerExpression.Type;

			if (FindExpression(new ReaderInfo { DataReaderType = dataReaderType, ToType = toType, ProviderFieldType = providerType, FieldType = fieldType, DataTypeName = typeName }, out var expr) ||
			    FindExpression(new ReaderInfo { DataReaderType = dataReaderType, ToType = toType, ProviderFieldType = providerType, FieldType = fieldType                          }, out expr) ||
			    FindExpression(new ReaderInfo { DataReaderType = dataReaderType, ToType = toType, ProviderFieldType = providerType                                                 }, out expr) ||
			    FindExpression(new ReaderInfo { DataReaderType = dataReaderType,                  ProviderFieldType = providerType                                                 }, out expr) ||
			    FindExpression(new ReaderInfo { DataReaderType = dataReaderType,                  ProviderFieldType = providerType, FieldType = fieldType, DataTypeName = typeName }, out expr) ||
			    FindExpression(new ReaderInfo { DataReaderType = dataReaderType,                  ProviderFieldType = providerType, FieldType = fieldType                          }, out expr) ||
			    FindExpression(new ReaderInfo { DataReaderType = dataReaderType, ToType = toType,                                   FieldType = fieldType, DataTypeName = typeName }, out expr) ||
			    FindExpression(new ReaderInfo { DataReaderType = dataReaderType, ToType = toType,                                   FieldType = fieldType                          }, out expr) ||
			    FindExpression(new ReaderInfo { DataReaderType = dataReaderType,                                                    FieldType = fieldType, DataTypeName = typeName }, out expr) ||
			    FindExpression(new ReaderInfo { DataReaderType = dataReaderType, ToType = toType                                                                                   }, out expr) ||
			    FindExpression(new ReaderInfo { DataReaderType = dataReaderType,                                                    FieldType = fieldType                          }, out expr))
				return expr;

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
				Expression.Call(readerExpression, getValueMethodInfo, ExpressionInstances.Int32Array(idx)),
				fieldType);
		}

		protected bool FindExpression(ReaderInfo info, [NotNullWhen(true)] out Expression? expr)
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
			var st = ((DbDataReader)reader).GetSchemaTable();
			return st == null || st.Rows[idx].IsNull("AllowDBNull") || (bool)st.Rows[idx]["AllowDBNull"];
		}

		#endregion

		#region SetParameter

		public virtual void SetParameter(DataConnection dataConnection, IDbDataParameter parameter, string name, DbDataType dataType, object? value)
		{
			switch (dataType.DataType)
			{
				case DataType.Char      :
				case DataType.NChar     :
				case DataType.VarChar   :
				case DataType.NVarChar  :
				case DataType.Text      :
				case DataType.NText     :
					if      (value is DateTimeOffset dto) value = dto.ToString("yyyy-MM-ddTHH:mm:ss.ffffff zzz");
					else if (value is DateTime dt)
					{
						value = dt.ToString(
							dt.Millisecond == 0
								? dt.Hour == 0 && dt.Minute == 0 && dt.Second == 0
									? "yyyy-MM-dd"
									: "yyyy-MM-ddTHH:mm:ss"
								: "yyyy-MM-ddTHH:mm:ss.fff");
					}
					else if (value is TimeSpan ts)
					{
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
					if (value is Binary binary) value = binary.ToArray();
					break;
				case DataType.Int64     :
					if (value is TimeSpan span) value = span.Ticks;
					break;
				case DataType.Xml       :
					     if (value is XDocument)            value = value.ToString();
					else if (value is XmlDocument document) value = document.InnerXml;
					break;
			}

			parameter.ParameterName = name;
			SetParameterType(dataConnection, parameter, dataType);
			parameter.Value = value ?? DBNull.Value;
		}

		public virtual Type ConvertParameterType(Type type, DbDataType dataType)
		{
			switch (dataType.DataType)
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

		public abstract ISchemaProvider GetSchemaProvider     ();

		protected virtual void SetParameterType(DataConnection dataConnection, IDbDataParameter parameter, DbDataType dataType)
		{
			DbType dbType;

			switch (dataType.DataType)
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

		#region BulkCopy

		public virtual BulkCopyRowsCopied BulkCopy<T>(ITable<T> table, BulkCopyOptions options, IEnumerable<T> source)
			where T : notnull
		{
			return new BasicBulkCopy().BulkCopy(options.BulkCopyType, table, options, source);
		}

		public virtual Task<BulkCopyRowsCopied> BulkCopyAsync<T>(
			ITable<T> table, BulkCopyOptions options, IEnumerable<T> source, CancellationToken cancellationToken)
			where T : notnull
		{
			return new BasicBulkCopy().BulkCopyAsync(options.BulkCopyType, table, options, source, cancellationToken);
		}

#if NATIVE_ASYNC
		public virtual Task<BulkCopyRowsCopied> BulkCopyAsync<T>(
			ITable<T> table, BulkCopyOptions options, IAsyncEnumerable<T> source, CancellationToken cancellationToken)
			where T: notnull
		{
			return new BasicBulkCopy().BulkCopyAsync(options.BulkCopyType, table, options, source, cancellationToken);
		}
#endif

		#endregion
	}
}
