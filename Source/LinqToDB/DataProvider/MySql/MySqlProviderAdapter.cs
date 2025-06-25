using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Expressions.Types;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

namespace LinqToDB.DataProvider.MySql
{
	public abstract class MySqlProviderAdapter : IDynamicProviderAdapter
	{
		private static readonly Type[] _ordinalParameters = [typeof(int)];

		private static readonly Lock _mysqlDataSyncRoot      = new ();
		private static readonly Lock _mysqlConnectorSyncRoot = new ();

		private static MySqlProviderAdapter? _mysqlDataInstance;
		private static MySqlProviderAdapter? _mysqlConnectorInstance;

		public const string MySqlConnectorAssemblyName = "MySqlConnector";
		public const string MySqlDataAssemblyName      = "MySql.Data";

		public const string MySqlDataClientNamespace = "MySql.Data.MySqlClient";
		public const string MySqlDataTypesNamespace  = "MySql.Data.Types";

		public const string MySqlConnectorNamespace      = "MySqlConnector";
		public const string MySqlConnectorTypesNamespace = "MySqlConnector";

		public const string OldMySqlConnectorNamespace       = "MySql.Data.MySqlClient";
		public const string OldMySqlConnectorTypesNamespace  = "MySql.Data.Types";

		MySqlProviderAdapter()
		{
		}

		#region IDynamicProviderAdapter

		public Type          ConnectionType  { get; protected set; } = null!;
		public Type          DataReaderType  { get; protected set; } = null!;
		public Type          ParameterType   { get; protected set; } = null!;
		public Type          CommandType     { get; protected set; } = null!;
		public Type          TransactionType { get; protected set; } = null!;

		Func<string, DbConnection> _connectionFactory = null!;
		public DbConnection CreateConnection(string connectionString) => _connectionFactory(connectionString);

		#endregion

		public MySqlProvider ProviderType    { get; protected set; }
		public MappingSchema MappingSchema   { get; protected set; } = null!;

		/// <summary>
		/// Not supported by MySqlConnector prior to 2.1.0.
		/// </summary>
		public Type? MySqlDecimalType { get; protected set; }
		public Type MySqlDateTimeType { get; protected set; } = null!;
		public Type MySqlGeometryType { get; protected set; } = null!;

		/// <summary>
		/// Not needed for MySqlConnector as it supports MySqlDecimal parameters.
		/// </summary>
		public Func<object,string>? MySqlDecimalGetter { get; protected set; }

		/// <summary>
		/// Not supported by MySqlConnector prior to 2.1.0.
		/// </summary>
		public string? GetMySqlDecimalMethodName { get; protected set; }

		/// <summary>
		/// MySqlConnector-only.
		/// </summary>
		public string? GetDateTimeOffsetMethodName { get; protected set; }
		public string? GetTimeSpanMethodName       { get; protected set; }
		public string? GetTimeOnlyMethodName       { get; protected set; }
		public string? GetDateOnlyMethodName       { get; protected set; }
		public string? GetSByteMethodName          { get; protected set; }
		public string? GetUInt16MethodName         { get; protected set; }
		public string? GetUInt32MethodName         { get; protected set; }
		public string? GetUInt64MethodName         { get; protected set; }
		public string  GetMySqlDateTimeMethodName  { get; protected set; } = null!;
		public string  ProviderTypesNamespace      { get; protected set; } = null!;

		/// <summary>
		/// Returns object, because both providers use different enums and we anyway don't need typed value.
		/// </summary>
		public Func<DbParameter,object> GetDbType   { get; protected set; } = null!;
		public BulkCopyAdapter? BulkCopy            { get; protected set; }
		public bool             IsDateOnlySupported { get; protected set; }

		public abstract bool    IsPackageProceduresSupported { get; }

		public class BulkCopyAdapter
		{
			internal BulkCopyAdapter(
				Func<DbConnection, DbTransaction?, MySqlConnector.MySqlBulkCopy> bulkCopyCreator,
				Func<int, string, MySqlBulkCopyColumnMapping>                    bulkCopyColumnMappingCreator)
			{
				Create              = bulkCopyCreator;
				CreateColumnMapping = bulkCopyColumnMappingCreator;
			}

			internal Func<DbConnection, DbTransaction?, MySqlConnector.MySqlBulkCopy> Create              { get; }
			internal Func<int, string, MySqlBulkCopyColumnMapping>                    CreateColumnMapping { get; }
		}

		public static MySqlProviderAdapter GetInstance(MySqlProvider provider)
		{
			switch (provider)
			{
				case MySqlProvider.MySqlConnector:
				{
					if (_mysqlConnectorInstance == null)
					{
						lock (_mysqlConnectorSyncRoot)
#pragma warning disable CA1508 // Avoid dead conditional code
							_mysqlConnectorInstance ??= new MySqlConnector.MySqlConnectorProviderAdapter();
#pragma warning restore CA1508 // Avoid dead conditional code
					}

					return _mysqlConnectorInstance;
				}
				default:
				{
					if (_mysqlDataInstance == null)
					{
						lock (_mysqlDataSyncRoot)
#pragma warning disable CA1508 // Avoid dead conditional code
							_mysqlDataInstance ??= new MySqlData.MySqlDataProviderAdapter();
#pragma warning restore CA1508 // Avoid dead conditional code
					}

					return _mysqlDataInstance;
				}
			}
		}

		private static void AppendAction(StringBuilder sb, string value) => sb.Append(value);

		sealed class MySqlData
		{
			public sealed class MySqlDataProviderAdapter : MySqlProviderAdapter
			{
				public override bool IsPackageProceduresSupported => false;

				public MySqlDataProviderAdapter()
				{
					var assembly = Common.Tools.TryLoadAssembly(MySqlDataAssemblyName, null);
					if (assembly == null)
						throw new InvalidOperationException($"Cannot load assembly {MySqlDataAssemblyName}");

					var connectionType    = assembly.GetType($"{MySqlDataClientNamespace}.MySqlConnection" , true)!;
					var dataReaderType    = assembly.GetType($"{MySqlDataClientNamespace}.MySqlDataReader" , true)!;
					var parameterType     = assembly.GetType($"{MySqlDataClientNamespace}.MySqlParameter"  , true)!;
					var commandType       = assembly.GetType($"{MySqlDataClientNamespace}.MySqlCommand"    , true)!;
					var transactionType   = assembly.GetType($"{MySqlDataClientNamespace}.MySqlTransaction", true)!;
					var dbType            = assembly.GetType($"{MySqlDataClientNamespace}.MySqlDbType"     , true)!;
					var mySqlDecimalType  = assembly.GetType($"{MySqlDataTypesNamespace}.MySqlDecimal"     , true)!;
					var mySqlDateTimeType = assembly.GetType($"{MySqlDataTypesNamespace}.MySqlDateTime"    , true)!;
					var mySqlGeometryType = assembly.GetType($"{MySqlDataTypesNamespace}.MySqlGeometry"    , true)!;

					var typeMapper = new TypeMapper();
					typeMapper.RegisterTypeWrapper<MySqlConnection>(connectionType);
					typeMapper.RegisterTypeWrapper<MySqlParameter>(parameterType);
					typeMapper.RegisterTypeWrapper<MySqlDbType>(dbType);
					typeMapper.RegisterTypeWrapper<MySqlDateTime>(mySqlDateTimeType);
					typeMapper.RegisterTypeWrapper<MySqlDecimal>(mySqlDecimalType);
					typeMapper.FinalizeMappings();

					var dbTypeGetter       = typeMapper.Type<MySqlParameter>().Member(p => p.MySqlDbType).BuildGetter<DbParameter>();
					var decimalGetter      = typeMapper.BuildFunc<object, string>(typeMapper.MapLambda((object value) => ((MySqlDecimal)value).ToString()));
					var toDecimalConverter = typeMapper.MapLambda((MySqlDecimal d) => d.Value);
					var toDoubleConverter  = typeMapper.MapLambda((MySqlDecimal d) => d.ToDouble());
					var dateTimeConverter  = typeMapper.MapLambda((MySqlDateTime dt) => dt.GetDateTime());

					_connectionFactory = typeMapper.BuildTypedFactory<string, MySqlConnection, DbConnection>(connectionString => new MySqlConnection(connectionString));

					var mappingSchema = new MySqlDataAdapterMappingSchema();

					mappingSchema.SetDataType(mySqlDecimalType, DataType.Decimal);
					mappingSchema.SetConvertExpression(mySqlDecimalType, typeof(decimal), toDecimalConverter);
					mappingSchema.SetConvertExpression(mySqlDecimalType, typeof(double), toDoubleConverter);
					mappingSchema.SetValueToSqlConverter(mySqlDecimalType,
						typeMapper.BuildAction<StringBuilder,SqlDataType,DataOptions,object>(
							typeMapper.MapActionLambda((StringBuilder sb, SqlDataType type, DataOptions options, object value) => AppendAction(sb, ((MySqlDecimal)value).ToString()))));

					mappingSchema.SetDataType(mySqlDateTimeType, DataType.DateTime2);
					mappingSchema.SetConvertExpression(mySqlDateTimeType, typeof(DateTime), dateTimeConverter);

					ProviderType                = MySqlProvider.MySqlData;
					ConnectionType              = connectionType;
					DataReaderType              = dataReaderType;
					ParameterType               = parameterType;
					CommandType                 = commandType;
					TransactionType             = transactionType;
					MySqlDecimalType            = mySqlDecimalType;
					MySqlDateTimeType           = mySqlDateTimeType;
					MySqlGeometryType           = mySqlGeometryType;
					MySqlDecimalGetter          = decimalGetter;
					GetDbType                   = p => dbTypeGetter(p);
					GetMySqlDecimalMethodName   = "GetMySqlDecimal";
					GetDateTimeOffsetMethodName = null;
					GetSByteMethodName          = dataReaderType.GetMethod("GetSByte",  _ordinalParameters)?.Name;
					GetUInt16MethodName         = dataReaderType.GetMethod("GetUInt16", _ordinalParameters)?.Name;
					GetUInt32MethodName         = dataReaderType.GetMethod("GetUInt32", _ordinalParameters)?.Name;
					GetUInt64MethodName         = dataReaderType.GetMethod("GetUInt64", _ordinalParameters)?.Name;
					GetMySqlDateTimeMethodName  = "GetMySqlDateTime";
					ProviderTypesNamespace      = MySqlDataTypesNamespace;
					MappingSchema               = mappingSchema;
					BulkCopy                    = null;
					IsDateOnlySupported         = false;
				}
			}

			sealed class MySqlDataAdapterMappingSchema : LockedMappingSchema
			{
				public MySqlDataAdapterMappingSchema() : base("MySql.Data")
				{
				}
			}

			[Wrapper]
			private sealed class MySqlDateTime
			{
				public DateTime GetDateTime() => throw new NotImplementedException();
			}

			[Wrapper]
			private sealed class MySqlDecimal
			{
				public          decimal Value      => throw new NotImplementedException();
				public          double  ToDouble() => throw new NotImplementedException();
				public override string  ToString() => throw new NotImplementedException();
			}

			[Wrapper]
			private sealed class MySqlParameter
			{
				public MySqlDbType MySqlDbType { get; set; }
			}

			[Wrapper]
			internal enum MySqlDbType
			{
				Binary     = 754,
				Bit        = 16,
				Blob       = 252,
				Byte       = 1,
				Date       = 10,
				Datetime   = 12,
				DateTime   = 12,
				Decimal    = 0,
				Double     = 5,
				Enum       = 247,
				Float      = 4,
				Geometry   = 255,
				Guid       = 854,
				Int16      = 2,
				Int24      = 9,
				Int32      = 3,
				Int64      = 8,
				JSON       = 245,
				LongBlob   = 251,
				LongText   = 751,
				MediumBlob = 250,
				MediumText = 750,
				Newdate    = 14,
				NewDecimal = 246,
				Set        = 248,
				String     = 254,
				Text       = 752,
				Time       = 11,
				Timestamp  = 7,
				TinyBlob   = 249,
				TinyText   = 749,
				UByte      = 501,
				UInt16     = 502,
				UInt24     = 509,
				UInt32     = 503,
				UInt64     = 508,
				VarBinary  = 753,
				VarChar    = 253,
				VarString  = 15,
				Year       = 13
			}
		}

		internal sealed class MySqlConnector
		{
			private static readonly Version MinBulkCopyVersion     = new (0, 67);
			private static readonly Version MinModernVersion       = new (1, 0);
			// actually it was added in 2.1.0, but assembly version wasn't updated
			private static readonly Version MinMySqlDecimalVersion = new (2, 0);
			// added in 2.0.0 with bulk copy fix in 2.1.8
			private static readonly Version MinDateOnlyVersion     = new (2, 0);

			public sealed class MySqlConnectorProviderAdapter : MySqlProviderAdapter
			{
				public override bool IsPackageProceduresSupported => true;

				public MySqlConnectorProviderAdapter()
				{
					var assembly = Common.Tools.TryLoadAssembly(MySqlConnectorAssemblyName, null);
					if (assembly == null)
						throw new InvalidOperationException($"Cannot load assembly {MySqlConnectorAssemblyName}");

					var hasBulkCopy  = assembly.GetName().Version >= MinBulkCopyVersion;
					var version1plus = assembly.GetName().Version >= MinModernVersion;

					var clientNamespace = version1plus ? MySqlConnectorNamespace      : OldMySqlConnectorNamespace;
					var typesNamespace  = version1plus ? MySqlConnectorTypesNamespace : OldMySqlConnectorTypesNamespace;

					var connectionType    = assembly.GetType($"{clientNamespace}.MySqlConnection" , true)!;
					var dataReaderType    = assembly.GetType($"{clientNamespace}.MySqlDataReader" , true)!;
					var parameterType     = assembly.GetType($"{clientNamespace}.MySqlParameter"  , true)!;
					var commandType       = assembly.GetType($"{clientNamespace}.MySqlCommand"    , true)!;
					var transactionType   = assembly.GetType($"{clientNamespace}.MySqlTransaction", true)!;
					var dbType            = assembly.GetType($"{clientNamespace}.MySqlDbType"     , true)!;
					var mySqlDateTimeType = assembly.GetType($"{typesNamespace}.MySqlDateTime"    , true)!;
					var mySqlGeometryType = assembly.GetType($"{typesNamespace}.MySqlGeometry"    , true)!;
					var mySqlDecimalType  = assembly.GetName().Version >= MinMySqlDecimalVersion ? assembly.GetType($"{typesNamespace}.MySqlDecimal", false) : null;

					var typeMapper = new TypeMapper();

					typeMapper.RegisterTypeWrapper<MySqlParameter>(parameterType);
					typeMapper.RegisterTypeWrapper<MySqlDbType   >(dbType);
					typeMapper.RegisterTypeWrapper<MySqlDateTime >(mySqlDateTimeType);
					if (mySqlDecimalType != null)
						typeMapper.RegisterTypeWrapper<MySqlDecimal>(mySqlDecimalType);

					typeMapper.RegisterTypeWrapper<MySqlConnection >(connectionType);
					typeMapper.RegisterTypeWrapper<MySqlTransaction>(transactionType);

					BulkCopyAdapter? bulkCopy = null;
					if (hasBulkCopy)
					{
						var bulkCopyType                   = assembly.GetType($"{clientNamespace}.MySqlBulkCopy"              , true)!;
						var bulkRowsCopiedEventHandlerType = assembly.GetType($"{clientNamespace}.MySqlRowsCopiedEventHandler", true)!;
						var bulkCopyColumnMappingType      = assembly.GetType($"{clientNamespace}.MySqlBulkCopyColumnMapping" , true)!;
						var rowsCopiedEventArgsType        = assembly.GetType($"{clientNamespace}.MySqlRowsCopiedEventArgs"   , true)!;
						var bulkCopyResultType             = assembly.GetType($"{clientNamespace}.MySqlBulkCopyResult"        , false)!;

						typeMapper.RegisterTypeWrapper<MySqlBulkCopy              >(bulkCopyType!);
						typeMapper.RegisterTypeWrapper<MySqlRowsCopiedEventHandler>(bulkRowsCopiedEventHandlerType);
						typeMapper.RegisterTypeWrapper<MySqlBulkCopyColumnMapping >(bulkCopyColumnMappingType);
						typeMapper.RegisterTypeWrapper<MySqlRowsCopiedEventArgs   >(rowsCopiedEventArgsType);
						if (bulkCopyResultType != null)
							typeMapper.RegisterTypeWrapper<MySqlBulkCopyResult    >(bulkCopyResultType);
						typeMapper.FinalizeMappings();

						bulkCopy = new BulkCopyAdapter(
							typeMapper.BuildWrappedFactory((DbConnection connection, DbTransaction? transaction) => new MySqlBulkCopy((MySqlConnection)(object)connection, (MySqlTransaction?)(object?)transaction)),
							typeMapper.BuildWrappedFactory((int source, string destination) => new MySqlBulkCopyColumnMapping(source, destination, null)));
					}
					else
						typeMapper.FinalizeMappings();

					_connectionFactory = typeMapper.BuildTypedFactory<string, MySqlConnection, DbConnection>(connectionString => new MySqlConnection(connectionString));

					var typeGetter        = typeMapper.Type<MySqlParameter>().Member(p => p.MySqlDbType).BuildGetter<DbParameter>();
					var dateTimeConverter = typeMapper.MapLambda((MySqlDateTime dt) => dt.GetDateTime());

					var mappingSchema = new MySqlConnectorAdapterMappingSchema();

					mappingSchema.SetDataType(mySqlDateTimeType, DataType.DateTime2);
					mappingSchema.SetConvertExpression(mySqlDateTimeType, typeof(DateTime), dateTimeConverter);

					if (mySqlDecimalType != null)
					{
						var toDecimalConverter = typeMapper.MapLambda((MySqlDecimal d) => d.Value);
						var toDoubleConverter  = typeMapper.MapLambda((MySqlDecimal d) => d.ToDouble());

						mappingSchema.SetDataType(mySqlDecimalType, DataType.Decimal);
						mappingSchema.SetConvertExpression(mySqlDecimalType, typeof(decimal), toDecimalConverter);
						mappingSchema.SetValueToSqlConverter(mySqlDecimalType,
							typeMapper.BuildAction<StringBuilder,SqlDataType,DataOptions,object>(
								typeMapper.MapActionLambda((StringBuilder sb, SqlDataType type, DataOptions options, object value) => AppendAction(sb, ((MySqlDecimal)value).ToString()))));

						mappingSchema.SetConvertExpression(mySqlDecimalType, typeof(double) , toDoubleConverter);
					}

					ProviderType                = MySqlProvider.MySqlConnector;
					ConnectionType              = connectionType;
					DataReaderType              = dataReaderType;
					ParameterType               = parameterType;
					CommandType                 = commandType;
					TransactionType             = transactionType;
					MySqlDecimalType            = mySqlDecimalType;
					MySqlDateTimeType           = mySqlDateTimeType;
					MySqlGeometryType           = mySqlGeometryType;
					MySqlDecimalGetter          = null;
					GetDbType                   = p => typeGetter(p);
					GetMySqlDecimalMethodName   = mySqlDecimalType != null ? "GetMySqlDecimal" : null;
					GetDateTimeOffsetMethodName = "GetDateTimeOffset";
					GetMySqlDateTimeMethodName  = "GetMySqlDateTime";
					GetTimeSpanMethodName       = dataReaderType.GetMethod("GetTimeSpan", _ordinalParameters)?.Name;
					GetTimeOnlyMethodName       = dataReaderType.GetMethod("GetTimeOnly", _ordinalParameters)?.Name;
					GetDateOnlyMethodName       = dataReaderType.GetMethod("GetDateOnly", _ordinalParameters)?.Name;
					GetSByteMethodName          = dataReaderType.GetMethod("GetSByte", _ordinalParameters)?.Name;
					GetUInt16MethodName         = dataReaderType.GetMethod("GetUInt16", _ordinalParameters)?.Name;
					GetUInt32MethodName         = dataReaderType.GetMethod("GetUInt32", _ordinalParameters)?.Name;
					GetUInt64MethodName         = dataReaderType.GetMethod("GetUInt64", _ordinalParameters)?.Name;
					ProviderTypesNamespace      = typesNamespace;
					MappingSchema               = mappingSchema;
					BulkCopy                    = bulkCopy;
					IsDateOnlySupported         = assembly.GetName().Version >= MinDateOnlyVersion;
				}
			}

			sealed class MySqlConnectorAdapterMappingSchema : LockedMappingSchema
			{
				public MySqlConnectorAdapterMappingSchema() : base("MySqlConnector")
				{
				}
			}

			#region wrappers
			[Wrapper]
			private sealed class MySqlDecimal
			{
				public          decimal Value      => throw new NotImplementedException();
				public          double  ToDouble() => throw new NotImplementedException();
				public override string  ToString() => throw new NotImplementedException();
			}

			[Wrapper]
			private sealed class MySqlDateTime
			{
				public DateTime GetDateTime() => throw new NotImplementedException();
			}

			[Wrapper]
			private sealed class MySqlParameter
			{
				public MySqlDbType MySqlDbType { get; set; }
			}

			[Wrapper]
			internal enum MySqlDbType
			{
				Binary     = 600,
				Bit        = 16,
				Blob       = 252,
				Bool       = -1,
				Byte       = 1,
				Date       = 10,
				Datetime   = 12,
				DateTime   = 12,
				Decimal    = 0,
				Double     = 5,
				Enum       = 247,
				Float      = 4,
				Geometry   = 255,
				Guid       = 800,
				Int16      = 2,
				Int24      = 9,
				Int32      = 3,
				Int64      = 8,
				JSON       = 245,
				LongBlob   = 251,
				LongText   = 751,
				MediumBlob = 250,
				MediumText = 750,
				Newdate    = 14,
				NewDecimal = 246,
				Null       = 6,
				Set        = 248,
				String     = 254,
				Text       = 752,
				Time       = 11,
				Timestamp  = 7,
				TinyBlob   = 249,
				TinyText   = 749,
				UByte      = 501,
				UInt16     = 502,
				UInt24     = 509,
				UInt32     = 503,
				UInt64     = 508,
				VarBinary  = 601,
				VarChar    = 253,
				VarString  = 15,
				Year       = 13
			}

			[Wrapper]
			internal sealed class MySqlTransaction
			{
			}

			#region BulkCopy
			[Wrapper]
			internal sealed class MySqlBulkCopy : TypeWrapper
			{
				private static object[] Wrappers { get; }
					= new object[]
				{
					// [0]: WriteToServer (version < 2.0.0)
					new Tuple<LambdaExpression, bool>
					((Expression<Action<MySqlBulkCopy, IDataReader>>               )((this_, dataReader) => this_.WriteToServer1(dataReader)), true),
					// [1]: get NotifyAfter
					(Expression<Func<MySqlBulkCopy, int>>                         )(this_ => this_.NotifyAfter),
					// [2]: get BulkCopyTimeout
					(Expression<Func<MySqlBulkCopy, int>>                         )(this_ => this_.BulkCopyTimeout),
					// [3]: get DestinationTableName
					(Expression<Func<MySqlBulkCopy, string?>>                     )(this_ => this_.DestinationTableName),
					// [4]: this.ColumnMappings.Add(column)
					(Expression<Action<MySqlBulkCopy, MySqlBulkCopyColumnMapping>>)((this_, column) => this_.ColumnMappings.Add(column)),
					// [5]: set NotifyAfter
					PropertySetter((MySqlBulkCopy this_) => this_.NotifyAfter),
					// [6]: set BulkCopyTimeout
					PropertySetter((MySqlBulkCopy this_) => this_.BulkCopyTimeout),
					// [7]: set DestinationTableName
					PropertySetter((MySqlBulkCopy this_) => this_.DestinationTableName),
					// [8]: WriteToServerAsync (version < 2.0.0)
					new Tuple<LambdaExpression, bool>
					((Expression<Func<MySqlBulkCopy, IDataReader, CancellationToken, Task>>)((this_, dataReader, cancellationToken) => this_.WriteToServerAsync1 (dataReader, cancellationToken)), true),
					// [9]: WriteToServer (version >= 2.0.0)
					new Tuple<LambdaExpression, bool>
					((Expression<Func<MySqlBulkCopy, IDataReader, MySqlBulkCopyResult>>)((this_, dataReader) => this_.WriteToServer2(dataReader)), true),
					// [10]: WriteToServerAsync (version >= 2.0.0)
					new Tuple<LambdaExpression, bool>
					((Expression<Func<MySqlBulkCopy, IDataReader, CancellationToken, Task<MySqlBulkCopyResult>>>)((this_, dataReader, cancellationToken) => this_.WriteToServerAsync2 (dataReader, cancellationToken)), true),
#if !NETFRAMEWORK
					// [11]: WriteToServerAsync (version < 2.0.0)
					new Tuple<LambdaExpression, bool>
					((Expression<Func<MySqlBulkCopy, IDataReader, CancellationToken, ValueTask>>)((this_, dataReader, cancellationToken) => this_.WriteToServerAsync3(dataReader, cancellationToken)), true),
					// [12]: WriteToServerAsync (version >= 2.0.0)
					new Tuple<LambdaExpression, bool>
					((Expression<Func<MySqlBulkCopy, IDataReader, CancellationToken, ValueTask<MySqlBulkCopyResult>>>)((this_, dataReader, cancellationToken) => this_.WriteToServerAsync4(dataReader, cancellationToken)), true),
#endif
				};

				private static string[] Events { get; }
					= new[]
				{
					nameof(MySqlRowsCopied)
				};

				public MySqlBulkCopy(object instance, Delegate[] wrappers) : base(instance, wrappers)
				{
				}

				public MySqlBulkCopy(MySqlConnection connection, MySqlTransaction? transaction) => throw new NotImplementedException();

#pragma warning disable RS0030 // API mapping must preserve type (IDataReader)
				[TypeWrapperName("WriteToServer")]
				private void WriteToServer1(IDataReader dataReader) => ((Action<MySqlBulkCopy, IDataReader>)CompiledWrappers[0])(this, dataReader);
				[TypeWrapperName("WriteToServer")]
				private MySqlBulkCopyResult WriteToServer2(IDataReader dataReader) => ((Func<MySqlBulkCopy, IDataReader, MySqlBulkCopyResult>)CompiledWrappers[9])(this, dataReader);

				[TypeWrapperName("WriteToServerAsync")]
				private Task WriteToServerAsync1      (IDataReader dataReader, CancellationToken cancellationToken) => ((Func<MySqlBulkCopy, IDataReader, CancellationToken,      Task>)CompiledWrappers[8])(this, dataReader, cancellationToken);
				[TypeWrapperName("WriteToServerAsync")]
				[return: CustomMapper(typeof(GenericTaskToTaskMapper))]
				private Task<MySqlBulkCopyResult> WriteToServerAsync2(IDataReader dataReader, CancellationToken cancellationToken) => throw new InvalidOperationException();

				private bool CanWriteToServer1 => CompiledWrappers[0] != null;
				private bool CanWriteToServer2 => CompiledWrappers[9] != null;
				private bool CanWriteToServerAsync1 => CompiledWrappers[8] != null;
				private bool CanWriteToServerAsync2 => CompiledWrappers[10] != null;
#if !NETFRAMEWORK
				[TypeWrapperName("WriteToServerAsync")]
				private ValueTask WriteToServerAsync3(IDataReader dataReader, CancellationToken cancellationToken) => ((Func<MySqlBulkCopy, IDataReader, CancellationToken, ValueTask>)CompiledWrappers[11])(this, dataReader, cancellationToken);
				[TypeWrapperName("WriteToServerAsync")]
				[return: CustomMapper(typeof(GenericTaskToTaskMapper))]
				private ValueTask<MySqlBulkCopyResult> WriteToServerAsync4(IDataReader dataReader, CancellationToken cancellationToken) => throw new InvalidOperationException();
				private bool CanWriteToServerAsync3 => CompiledWrappers[11] != null;
				private bool CanWriteToServerAsync4 => CompiledWrappers[12] != null;
#else
				[TypeWrapperName("WriteToServerAsync")]
				private Task WriteToServerAsync3(IDataReader dataReader, CancellationToken cancellationToken) => throw new InvalidOperationException();
				[TypeWrapperName("WriteToServerAsync")]
				[return: CustomMapper(typeof(GenericTaskToTaskMapper))]
				private Task<MySqlBulkCopyResult> WriteToServerAsync4(IDataReader dataReader, CancellationToken cancellationToken) => throw new InvalidOperationException();
				private bool CanWriteToServerAsync3 => false;
				private bool CanWriteToServerAsync4 => false;
#endif
#pragma warning restore RS0030 //  API mapping must preserve type (IDataReader)

				public void WriteToServer(DbDataReader dataReader)
				{
					if (CanWriteToServer2)
						WriteToServer2(dataReader);
					else if (CanWriteToServer1)
						WriteToServer1(dataReader);
					else
						throw new InvalidOperationException("BulkCopy.WriteToServer implementation not configured");
				}

				public bool HasWriteToServerAsync => CanWriteToServerAsync1 || CanWriteToServerAsync2 || CanWriteToServerAsync3 || CanWriteToServerAsync4;

				public async Task WriteToServerAsync(DbDataReader dataReader, CancellationToken cancellationToken)
				{
					if (CanWriteToServerAsync4)
					{
						var action = (Func<MySqlBulkCopy, IDataReader, CancellationToken, Task>)CompiledWrappers[12];
#pragma warning disable RS0030 // API mapping must preserve type (IDataReader)
						await action(this, dataReader, cancellationToken).ConfigureAwait(false);
#pragma warning restore RS0030 //  API mapping must preserve type (IDataReader)
					}
					else if (CanWriteToServerAsync3)
						await WriteToServerAsync3(dataReader, cancellationToken).ConfigureAwait(false);
					else if (CanWriteToServerAsync2)
					{
						var action = (Func<MySqlBulkCopy, IDataReader, CancellationToken, Task>)CompiledWrappers[10];
#pragma warning disable RS0030 // API mapping must preserve type (IDataReader)
						await action(this, dataReader, cancellationToken).ConfigureAwait(false);
#pragma warning restore RS0030 //  API mapping must preserve type (IDataReader)
					}
					else if (CanWriteToServerAsync1)
						await WriteToServerAsync1(dataReader, cancellationToken).ConfigureAwait(false);
					else
						throw new InvalidOperationException("BulkCopy.WriteToServerAsync implementation not configured");
				}

				public int NotifyAfter
				{
					get => ((Func<MySqlBulkCopy, int>)CompiledWrappers[1])(this);
					set => ((Action<MySqlBulkCopy, int>)CompiledWrappers[5])(this, value);
				}

				public int BulkCopyTimeout
				{
					get => ((Func<MySqlBulkCopy  , int>)CompiledWrappers[2])(this);
					set => ((Action<MySqlBulkCopy, int>)CompiledWrappers[6])(this, value);
				}

				public string? DestinationTableName
				{
					get => ((Func<MySqlBulkCopy  , string?>)CompiledWrappers[3])(this);
					set => ((Action<MySqlBulkCopy, string?>)CompiledWrappers[7])(this, value);
				}

				private MySqlRowsCopiedEventHandler?     _MySqlRowsCopied;
				public event MySqlRowsCopiedEventHandler? MySqlRowsCopied
				{
					add    => _MySqlRowsCopied = (MySqlRowsCopiedEventHandler?)Delegate.Combine(_MySqlRowsCopied, value);
					remove => _MySqlRowsCopied = (MySqlRowsCopiedEventHandler?)Delegate.Remove (_MySqlRowsCopied, value);
				}

				private List<MySqlBulkCopyColumnMapping> ColumnMappings => throw new NotImplementedException("Use AddColumnMapping method instead");

				// because underlying object use List<T> for column mappings, easiest approch will be to add
				// non-existing Add method
				public void AddColumnMapping(MySqlBulkCopyColumnMapping column) => ((Action<MySqlBulkCopy, MySqlBulkCopyColumnMapping>) CompiledWrappers[4])(this, column);
			}

			[Wrapper]
			public sealed class MySqlRowsCopiedEventArgs : TypeWrapper
			{
				private static LambdaExpression[] Wrappers { get; }
					= new LambdaExpression[]
				{
					// [0]: get RowsCopied
					(Expression<Func<MySqlRowsCopiedEventArgs, long>> )(this_ => this_.RowsCopied),
					// [1]: get Abort
					(Expression<Func<MySqlRowsCopiedEventArgs, bool>>)(this_ => this_.Abort),
					// [3]: set Abort
					PropertySetter((MySqlRowsCopiedEventArgs this_) => this_.Abort),
				};

				public MySqlRowsCopiedEventArgs(object instance, Delegate[] wrappers) : base(instance, wrappers)
				{
				}

				public long RowsCopied
				{
					get => ((Func<MySqlRowsCopiedEventArgs, long>)CompiledWrappers[0])(this);
				}

				public bool Abort
				{
					get => ((Func<MySqlRowsCopiedEventArgs,   bool>)CompiledWrappers[1])(this);
					set => ((Action<MySqlRowsCopiedEventArgs, bool>)CompiledWrappers[2])(this, value);
				}
			}

			[Wrapper]
			public delegate void MySqlRowsCopiedEventHandler(object sender, MySqlRowsCopiedEventArgs e);

#endregion
#endregion
		}

		[Wrapper]
		internal sealed class MySqlBulkCopyColumnMapping : TypeWrapper
		{
			public MySqlBulkCopyColumnMapping(object instance) : base(instance, null)
			{
			}

			public MySqlBulkCopyColumnMapping(int sourceOrdinal, string destinationColumn, string? expression = null) => throw new NotImplementedException();
		}

		[Wrapper]
		internal sealed class MySqlBulkCopyResult : TypeWrapper
		{
			public MySqlBulkCopyResult(object instance) : base(instance, null)
			{
			}
		}

		[Wrapper]
		internal sealed class MySqlConnection
		{
			public MySqlConnection(string connectionString) => throw new NotImplementedException();
		}
	}
}
