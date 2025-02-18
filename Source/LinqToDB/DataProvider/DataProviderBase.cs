using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.Linq;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.Expressions;
using LinqToDB.Infrastructure;
using LinqToDB.Internal.Common;
using LinqToDB.Linq.Translation;
using LinqToDB.Mapping;
using LinqToDB.SchemaProvider;
using LinqToDB.SqlProvider;

namespace LinqToDB.DataProvider
{
	public abstract class DataProviderBase : IDataProvider, IInfrastructure<IServiceProvider>
	{
		#region .ctor

		protected DataProviderBase(string name, MappingSchema mappingSchema)
		{
			Name             = name;
			MappingSchema    = mappingSchema;
			// set default flags values explicitly even for default values
			SqlProviderFlags = new SqlProviderFlags()
			{
				IsParameterOrderDependent            = false,
				AcceptsTakeAsParameter               = true,
				AcceptsTakeAsParameterIfSkip         = false,
				IsTakeSupported                      = true,
				IsSkipSupported                      = true,
				IsSkipSupportedIfTake                = false,
				TakeHintsSupported                   = null,
				IsSubQueryTakeSupported              = true,
				IsDerivedTableTakeSupported          = true,
				IsCorrelatedSubQueryTakeSupported    = true,
				IsSupportsJoinWithoutCondition       = true,
				IsSubQuerySkipSupported              = true,
				IsSubQueryColumnSupported            = true,
				IsSubQueryOrderBySupported           = false,
				IsCountSubQuerySupported             = true,
				IsIdentityParameterRequired          = false,
				IsApplyJoinSupported                 = false,
				IsInsertOrUpdateSupported            = true,
				CanCombineParameters                 = true,
				MaxInListValuesCount                 = int.MaxValue,
				OutputDeleteUseSpecialTable          = false,
				OutputInsertUseSpecialTable          = false,
				OutputUpdateUseSpecialTables         = false,
				IsCrossJoinSupported                 = true,
				IsCommonTableExpressionsSupported    = false,
				IsAllSetOperationsSupported          = false,
				IsDistinctSetOperationsSupported     = true,
				IsCountDistinctSupported             = true,
				IsAggregationDistinctSupported       = true,
				AcceptsOuterExpressionInAggregate    = true,
				IsUpdateFromSupported                = true,
				DefaultMultiQueryIsolationLevel      = IsolationLevel.RepeatableRead,
				RowConstructorSupport                = RowFeature.None,
				IsWindowFunctionsSupported           = true,
				IsDerivedTableOrderBySupported       = true,
			};

			SetField<DbDataReader, bool>    ((r,i) => r.GetBoolean (i));
			SetField<DbDataReader, byte>    ((r,i) => r.GetByte    (i));
			SetField<DbDataReader, char>    ((r,i) => r.GetChar    (i));
			SetField<DbDataReader, short>   ((r,i) => r.GetInt16   (i));
			SetField<DbDataReader, int>     ((r,i) => r.GetInt32   (i));
			SetField<DbDataReader, long>    ((r,i) => r.GetInt64   (i));
			SetField<DbDataReader, float>   ((r,i) => r.GetFloat   (i));
			SetField<DbDataReader, double>  ((r,i) => r.GetDouble  (i));
			SetField<DbDataReader, string>  ((r,i) => r.GetString  (i));
			SetField<DbDataReader, decimal> ((r,i) => r.GetDecimal (i));
			SetField<DbDataReader, DateTime>((r,i) => r.GetDateTime(i));
			SetField<DbDataReader, Guid>    ((r,i) => r.GetGuid    (i));
			SetField<DbDataReader, byte[]>  ((r,i) => (byte[])r.GetValue(i));
		}

		#endregion

		#region Public Members

		public          string           Name                  { get; }
		public abstract string?          ConnectionNamespace   { get; }
		public abstract Type             DataReaderType        { get; }
		public virtual  MappingSchema    MappingSchema         { get; }
		public          SqlProviderFlags SqlProviderFlags      { get; }
		public abstract TableOptions     SupportedTableOptions { get; }
		public virtual  bool             TransactionsSupported => true;

		public static Func<IDataProvider, DbConnection, DbConnection>? OnConnectionCreated { get; set; }

		public virtual void InitContext(IDataContext dataContext)
		{
		}

		private int? _id;
		public  int   ID
		{
			get
			{
				if (_id == null)
				{
					using var idBuilder = new IdentifierBuilder(Name);
					_id = idBuilder.CreateID();
				}

				return _id.Value;
			}
		}

		public DbConnection CreateConnection(string connectionString)
		{
			var connection = CreateConnectionInternal(connectionString);

			if (OnConnectionCreated != null)
				connection = OnConnectionCreated(this, connection);

			return connection;
		}

		protected abstract DbConnection  CreateConnectionInternal (string connectionString);
		public    abstract ISqlBuilder   CreateSqlBuilder(MappingSchema   mappingSchema, DataOptions dataOptions);
		public    abstract ISqlOptimizer GetSqlOptimizer (DataOptions     dataOptions);

		public virtual DbCommand InitCommand(DataConnection dataConnection, DbCommand command, CommandType commandType, string commandText, DataParameter[]? parameters, bool withParameters)
		{
			command.CommandType = commandType;

			ClearCommandParameters(command);

			command.CommandText = commandText;

			return command;
		}

		public virtual void ClearCommandParameters(DbCommand command)
		{
			if (command.Parameters.Count != 0)
				command.Parameters.Clear();
		}

		public virtual void DisposeCommand(DbCommand command)
		{
			ClearCommandParameters(command);
			command.Dispose();
		}

#if NET6_0_OR_GREATER
		public virtual ValueTask DisposeCommandAsync(DbCommand command)
		{
			ClearCommandParameters(command);
			return command.DisposeAsync();
		}
#endif

		public virtual object? GetConnectionInfo(DataConnection dataConnection, string parameterName)
		{
			return null;
		}

		public virtual CommandBehavior GetCommandBehavior(CommandBehavior commandBehavior)
		{
			return commandBehavior;
		}

		/// <summary>
		/// Creates disposable object, which should be disposed by caller after database query execution completed.
		/// Could be used to execute provider's method with scope-specific settings, e.g. with Invariant culture to
		/// workaround incorrect culture handling in provider.
		/// </summary>
		/// <param name="dataConnection">Current data connection object.</param>
		/// <returns>Scoped execution disposable object or <c>null</c> if provider doesn't need scoped configuration.</returns>
		public virtual IExecutionScope? ExecuteScope(DataConnection dataConnection) => null;

		#endregion

		#region Helpers

		public readonly ConcurrentDictionary<ReaderInfo,Expression> ReaderExpressions = new ();

		protected void SetCharField(string dataTypeName, Expression<Func<DbDataReader, int,string>> expr)
		{
			ReaderExpressions[new ReaderInfo { FieldType = typeof(string), DataTypeName = dataTypeName }] = expr;
		}

		protected void SetCharFieldToType<T>(string dataTypeName, Expression<Func<DbDataReader, int, string>> expr)
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

		protected void SetProviderField<TP, T>(Type providerFieldType, Expression<Func<TP, int, T>> expr)
		{
			ReaderExpressions[new ReaderInfo { ToType = typeof(T), ProviderFieldType = providerFieldType }] = expr;
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

		public virtual Expression GetReaderExpression(DbDataReader reader, int idx, Expression readerExpression, Type? toType)
		{
			var fieldType    = reader.GetFieldType(idx);
			var providerType = reader.GetProviderSpecificFieldType(idx);
			var typeName     = reader.GetDataTypeName(idx);

			if (fieldType == null)
			{
				var name = reader.GetName(idx);
				throw new LinqToDBException($"Can't create '{typeName}' type or '{providerType}' specific type for {name}.");
			}

			typeName = NormalizeTypeName(typeName);

#if DEBUG1
			Debug.WriteLine("ToType                ProviderFieldType     FieldType             DataTypeName          Expression");
			Debug.WriteLine("--------------------- --------------------- --------------------- --------------------- ---------------------");
			Debug.WriteLine("{0,-21} {1,-21} {2,-21} {3,-21}",
				toType       == null ? "(null)" : toType.Name,
				providerType == null ? "(null)" : providerType.Name,
				fieldType.Name,
				typeName ?? "(null)");
			Debug.WriteLine("--------------------- --------------------- --------------------- --------------------- ---------------------");

			foreach (var ex in ReaderExpressions)
			{
				Debug.WriteLine("{0,-21} {1,-21} {2,-21} {3,-21} {4}",
					ex.Key.ToType?.Name,
					ex.Key.ProviderFieldType?.Name,
					ex.Key.FieldType?.Name,
					ex.Key.DataTypeName,
					ex.Value);
			}
#endif

			var dataReaderType = readerExpression.Type;

			if (FindExpression(new ReaderInfo { DataReaderType = dataReaderType, ToType = toType, ProviderFieldType = providerType, FieldType = fieldType, DataTypeName = typeName }, out var expr) ||
			    FindExpression(new ReaderInfo { DataReaderType = dataReaderType, ToType = toType, ProviderFieldType = providerType, FieldType = fieldType                          }, out expr) ||
			    FindExpression(new ReaderInfo { DataReaderType = dataReaderType, ToType = toType, ProviderFieldType = providerType,                        DataTypeName = typeName }, out expr) ||
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
			    FindExpression(new ReaderInfo { ToType = toType, ProviderFieldType = providerType,                        DataTypeName = typeName }, out expr) ||
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

			var getValueMethodInfo = MemberHelper.MethodOf<DbDataReader>(r => r.GetValue(0));
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

		public virtual bool? IsDBNullAllowed(DataOptions options, DbDataReader reader, int idx)
		{
			var st = reader.GetSchemaTable();
			return st == null || st.Rows[idx].IsNull("AllowDBNull") || (bool)st.Rows[idx]["AllowDBNull"];
		}

		#endregion

		#region SetParameter

		public virtual void SetParameter(DataConnection dataConnection, DbParameter parameter, string name, DbDataType dataType, object? value)
		{
			switch (dataType.DataType)
			{
				case DataType.Char      :
				case DataType.NChar     :
				case DataType.VarChar   :
				case DataType.NVarChar  :
				case DataType.Text      :
				case DataType.NText     :
					if      (value is DateTimeOffset dto) value = dto.ToString("yyyy-MM-ddTHH:mm:ss.ffffff zzz", DateTimeFormatInfo.InvariantInfo);
					else if (value is DateTime dt)
					{
						value = dt.ToString(
							dt.Millisecond == 0
								? dt.Hour == 0 && dt.Minute == 0 && dt.Second == 0
									? "yyyy-MM-dd"
									: "yyyy-MM-ddTHH:mm:ss"
								: "yyyy-MM-ddTHH:mm:ss.fff",
							DateTimeFormatInfo.InvariantInfo);
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
									: "hh\\:mm\\:ss",
							DateTimeFormatInfo.InvariantInfo);
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
					     if (value is XDocument xdoc)       value = xdoc.ToString();
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

		protected virtual void SetParameterType(DataConnection dataConnection, DbParameter parameter, DbDataType dataType)
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

		public virtual BulkCopyRowsCopied BulkCopy<T>(DataOptions options, ITable<T> table, IEnumerable<T> source)
			where T : notnull
		{
			return new BasicBulkCopy().BulkCopy(options.BulkCopyOptions.BulkCopyType, table, options, source);
		}

		public virtual Task<BulkCopyRowsCopied> BulkCopyAsync<T>(DataOptions options, ITable<T> table,
			IEnumerable<T> source, CancellationToken cancellationToken)
			where T : notnull
		{
			return new BasicBulkCopy().BulkCopyAsync(options.BulkCopyOptions.BulkCopyType, table, options, source, cancellationToken);
		}

		public virtual Task<BulkCopyRowsCopied> BulkCopyAsync<T>(DataOptions options, ITable<T> table,
			IAsyncEnumerable<T> source, CancellationToken cancellationToken)
			where T: notnull
		{
			return new BasicBulkCopy().BulkCopyAsync(options.BulkCopyOptions.BulkCopyType, table, options, source, cancellationToken);
		}

		#endregion

		public virtual IQueryParametersNormalizer GetQueryParameterNormalizer() => new UniqueParametersNormalizer();

		protected abstract IMemberTranslator  CreateMemberTranslator();
		protected virtual  IIdentifierService CreateIdentifierService() => new IdentifierServiceSimple(128);

		protected virtual void InitServiceProvider(SimpleServiceProvider serviceProvider)
		{
			serviceProvider.AddService(CreateMemberTranslator());
			serviceProvider.AddService(CreateIdentifierService());
		}

		SimpleServiceProvider? _serviceProvider;
		readonly Lock          _guard = new();

		IServiceProvider IInfrastructure<IServiceProvider>.Instance
		{
			get
			{
				if (_serviceProvider == null)
				{
					lock (_guard)
					{
						if (_serviceProvider == null)
						{
							var serviceProvider = new SimpleServiceProvider();
							InitServiceProvider(serviceProvider);
							_serviceProvider = serviceProvider;
						}
					}
				}

				return _serviceProvider;
			}
		}

	}
}
