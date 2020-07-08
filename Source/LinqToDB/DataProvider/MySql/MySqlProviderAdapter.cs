using System;
using System.Data;

namespace LinqToDB.DataProvider.MySql
{
	using System.Collections.Generic;
	using System.Linq.Expressions;
	using System.Threading;
	using System.Threading.Tasks;
	using LinqToDB.Expressions;
	using LinqToDB.Mapping;

	public class MySqlProviderAdapter : IDynamicProviderAdapter
	{
		private static readonly object _mysqlDataSyncRoot      = new object();
		private static readonly object _mysqlConnectorSyncRoot = new object();

		private static MySqlProviderAdapter? _mysqlDataInstance;
		private static MySqlProviderAdapter? _mysqlConnectorInstance;

		public const string MySqlConnectorAssemblyName    = "MySqlConnector";
		public const string MySqlDataAssemblyName         = "MySql.Data";

		// shared by both providers
		public const string ClientNamespace = "MySql.Data.MySqlClient";
		public const string TypesNamespace  = "MySql.Data.Types";

		internal enum MySqlProvider
		{
			MySqlData,
			MySqlConnector
		}

		private MySqlProviderAdapter(
			MySqlProvider provider,

			Type connectionType,
			Type dataReaderType,
			Type parameterType,
			Type commandType,
			Type transactionType,

			Type? mySqlDecimalType,
			Type  mySqlDateTimeType,
			Type  mySqlGeometryType,

			Func<object, decimal>? mySqlDecimalGetter,

			Func<IDbDataParameter, object> dbTypeGetter,

			string? getMySqlDecimalMethodName,
			string? getDateTimeOffsetMethodName,
			string  getMySqlDateTimeMethodName,

			MappingSchema    mappingSchema,
			BulkCopyAdapter? bulkCopy)
		{
			ProviderType = provider;

			ConnectionType  = connectionType;
			DataReaderType  = dataReaderType;
			ParameterType   = parameterType;
			CommandType     = commandType;
			TransactionType = transactionType;

			MySqlDecimalType  = mySqlDecimalType;
			MySqlDateTimeType = mySqlDateTimeType;
			MySqlGeometryType = mySqlGeometryType;

			MySqlDecimalGetter = mySqlDecimalGetter;

			GetDbType = dbTypeGetter;

			GetMySqlDecimalMethodName   = getMySqlDecimalMethodName;
			GetDateTimeOffsetMethodName = getDateTimeOffsetMethodName;
			GetMySqlDateTimeMethodName  = getMySqlDateTimeMethodName;

			MappingSchema = mappingSchema;
			BulkCopy      = bulkCopy;
		}

		internal MySqlProvider ProviderType { get; }

		public Type ConnectionType  { get; }
		public Type DataReaderType  { get; }
		public Type ParameterType   { get; }
		public Type CommandType     { get; }
		public Type TransactionType { get; }

		public MappingSchema MappingSchema { get; }

		/// <summary>
		/// Not supported by MySqlConnector.
		/// </summary>
		public Type? MySqlDecimalType  { get; }
		public Type  MySqlDateTimeType { get; }
		public Type  MySqlGeometryType { get; }

		/// <summary>
		/// Not supported by MySqlConnector.
		/// </summary>
		public Func<object, decimal>? MySqlDecimalGetter { get; }

		/// <summary>
		/// Not supported by MySqlConnector.
		/// </summary>
		public string? GetMySqlDecimalMethodName { get; }

		/// <summary>
		/// MySqlConnector-only.
		/// </summary>
		public string? GetDateTimeOffsetMethodName { get; }

		public string GetMySqlDateTimeMethodName   { get; }

		public string ProviderTypesNamespace => TypesNamespace;

		/// <summary>
		/// Returns object, because both providers use different enums and we anyway don't need typed value.
		/// </summary>
		public Func<IDbDataParameter, object> GetDbType { get; }

		internal BulkCopyAdapter? BulkCopy { get; }

		internal class BulkCopyAdapter
		{
			internal BulkCopyAdapter(
				Func<IDbConnection, IDbTransaction?, MySqlConnector.MySqlBulkCopy> bulkCopyCreator,
				Func<int, string, MySqlBulkCopyColumnMapping>                      bulkCopyColumnMappingCreator)
			{
				Create              = bulkCopyCreator;
				CreateColumnMapping = bulkCopyColumnMappingCreator;
			}

			public Func<IDbConnection, IDbTransaction?, MySqlConnector.MySqlBulkCopy> Create              { get; }
			public Func<int, string, MySqlBulkCopyColumnMapping>                      CreateColumnMapping { get; }
		}

		public static MySqlProviderAdapter GetInstance(string name)
		{
			if (name == ProviderName.MySqlConnector)
			{
				if (_mysqlConnectorInstance == null)
					lock (_mysqlConnectorSyncRoot)
						if (_mysqlConnectorInstance == null)
							_mysqlConnectorInstance = MySqlConnector.CreateAdapter();

				return _mysqlConnectorInstance;
			}
			else
			{
				if (_mysqlDataInstance == null)
					lock (_mysqlDataSyncRoot)
						if (_mysqlDataInstance == null)
							_mysqlDataInstance = MySqlData.CreateAdapter();

				return _mysqlDataInstance;
			}
		}

		private class MySqlData
		{
			internal static MySqlProviderAdapter CreateAdapter()
			{
				var assembly = Common.Tools.TryLoadAssembly(MySqlDataAssemblyName, null);
				if (assembly == null)
					throw new InvalidOperationException($"Cannot load assembly {MySqlDataAssemblyName}");

				var connectionType    = assembly.GetType($"{ClientNamespace}.MySqlConnection" , true)!;
				var dataReaderType    = assembly.GetType($"{ClientNamespace}.MySqlDataReader" , true)!;
				var parameterType     = assembly.GetType($"{ClientNamespace}.MySqlParameter"  , true)!;
				var commandType       = assembly.GetType($"{ClientNamespace}.MySqlCommand"    , true)!;
				var transactionType   = assembly.GetType($"{ClientNamespace}.MySqlTransaction", true)!;
				var dbType            = assembly.GetType($"{ClientNamespace}.MySqlDbType"     , true)!;
				var mySqlDecimalType  = assembly.GetType($"{TypesNamespace}.MySqlDecimal"     , true)!;
				var mySqlDateTimeType = assembly.GetType($"{TypesNamespace}.MySqlDateTime"    , true)!;
				var mySqlGeometryType = assembly.GetType($"{TypesNamespace}.MySqlGeometry"    , true)!;

				var typeMapper = new TypeMapper();
				typeMapper.RegisterTypeWrapper<MySqlParameter>(parameterType);
				typeMapper.RegisterTypeWrapper<MySqlDbType>(dbType);
				typeMapper.RegisterTypeWrapper<MySqlDateTime>(mySqlDateTimeType);
				typeMapper.RegisterTypeWrapper<MySqlDecimal>(mySqlDecimalType);

				var dbTypeGetter      = typeMapper.Type<MySqlParameter>().Member(p => p.MySqlDbType).BuildGetter<IDbDataParameter>();
				var decimalGetter     = typeMapper.Type<MySqlDecimal>().Member(p => p.Value).BuildGetter<object>();
				var dateTimeConverter = typeMapper.MapLambda((MySqlDateTime dt) => dt.GetDateTime());

				var mappingSchema = new MappingSchema();
				mappingSchema.SetDataType(mySqlDecimalType, DataType.Decimal);
				mappingSchema.SetDataType(mySqlDateTimeType, DataType.DateTime2);
				mappingSchema.SetConvertExpression(mySqlDateTimeType, typeof(DateTime), dateTimeConverter);

				return new MySqlProviderAdapter(
					MySqlProvider.MySqlData,
					connectionType,
					dataReaderType,
					parameterType,
					commandType,
					transactionType,
					mySqlDecimalType,
					mySqlDateTimeType,
					mySqlGeometryType,
					decimalGetter,
					p => dbTypeGetter(p),
					"GetMySqlDecimal",
					null,
					"GetMySqlDateTime",
					mappingSchema,
					null);
			}

			[Wrapper]
			private class MySqlDateTime
			{
				public DateTime GetDateTime() => throw new NotImplementedException();
			}

			[Wrapper]
			private class MySqlDecimal
			{
				public decimal Value { get; }
			}

			[Wrapper]
			private class MySqlParameter
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

		internal class MySqlConnector
		{
			private static readonly Version MinBulkCopyVersion = new Version(0, 67);

			internal static MySqlProviderAdapter CreateAdapter()
			{
				var assembly = Common.Tools.TryLoadAssembly(MySqlConnectorAssemblyName, null);
				if (assembly == null)
					throw new InvalidOperationException($"Cannot load assembly {MySqlConnectorAssemblyName}");

				var connectionType    = assembly.GetType($"{ClientNamespace}.MySqlConnection" , true)!;
				var dataReaderType    = assembly.GetType($"{ClientNamespace}.MySqlDataReader" , true)!;
				var parameterType     = assembly.GetType($"{ClientNamespace}.MySqlParameter"  , true)!;
				var commandType       = assembly.GetType($"{ClientNamespace}.MySqlCommand"    , true)!;
				var transactionType   = assembly.GetType($"{ClientNamespace}.MySqlTransaction", true)!;
				var dbType            = assembly.GetType($"{ClientNamespace}.MySqlDbType"     , true)!;
				var mySqlDateTimeType = assembly.GetType($"{TypesNamespace}.MySqlDateTime"    , true)!;
				var mySqlGeometryType = assembly.GetType($"{TypesNamespace}.MySqlGeometry"    , true)!;

				var typeMapper = new TypeMapper();
				typeMapper.RegisterTypeWrapper<MySqlParameter>(parameterType);
				typeMapper.RegisterTypeWrapper<MySqlDbType   >(dbType);
				typeMapper.RegisterTypeWrapper<MySqlDateTime >(mySqlDateTimeType);

				typeMapper.RegisterTypeWrapper<MySqlConnection >(connectionType);
				typeMapper.RegisterTypeWrapper<MySqlTransaction>(transactionType);

				BulkCopyAdapter? bulkCopy = null;

				if (assembly.GetName().Version >= MinBulkCopyVersion)
				{
					var bulkCopyType                   = assembly.GetType($"{ClientNamespace}.MySqlBulkCopy", true)!;
					var bulkRowsCopiedEventHandlerType = assembly.GetType($"{ClientNamespace}.MySqlRowsCopiedEventHandler", true)!;
					var bulkCopyColumnMappingType      = assembly.GetType($"{ClientNamespace}.MySqlBulkCopyColumnMapping" , true)!;
					var rowsCopiedEventArgsType        = assembly.GetType($"{ClientNamespace}.MySqlRowsCopiedEventArgs"   , true)!;

					typeMapper.RegisterTypeWrapper<MySqlBulkCopy              >(bulkCopyType!);
					typeMapper.RegisterTypeWrapper<MySqlRowsCopiedEventHandler>(bulkRowsCopiedEventHandlerType);
					typeMapper.RegisterTypeWrapper<MySqlBulkCopyColumnMapping >(bulkCopyColumnMappingType);
					typeMapper.RegisterTypeWrapper<MySqlRowsCopiedEventArgs   >(rowsCopiedEventArgsType);
					typeMapper.FinalizeMappings();

					bulkCopy = new BulkCopyAdapter(
						typeMapper.BuildWrappedFactory((IDbConnection connection, IDbTransaction? transaction) => new MySqlBulkCopy((MySqlConnection)connection, (MySqlTransaction?)transaction)),
						typeMapper.BuildWrappedFactory((int source, string destination) => new MySqlBulkCopyColumnMapping(source, destination, null)));
				}
				else
					typeMapper.FinalizeMappings();


				var typeGetter        = typeMapper.Type<MySqlParameter>().Member(p => p.MySqlDbType).BuildGetter<IDbDataParameter>();
				var dateTimeConverter = typeMapper.MapLambda((MySqlDateTime dt) => dt.GetDateTime());

				var mappingSchema = new MappingSchema();
				mappingSchema.SetDataType(mySqlDateTimeType, DataType.DateTime2);
				mappingSchema.SetConvertExpression(mySqlDateTimeType, typeof(DateTime), dateTimeConverter);

				return new MySqlProviderAdapter(
					MySqlProvider.MySqlConnector,
					connectionType,
					dataReaderType,
					parameterType,
					commandType,
					transactionType,
					null,
					mySqlDateTimeType,
					mySqlGeometryType,
					null,
					p => typeGetter(p),
					null,
					"GetDateTimeOffset",
					"GetMySqlDateTime",
					mappingSchema,
					bulkCopy);
			}

			#region wrappers
			[Wrapper]
			private class MySqlDateTime
			{
				public DateTime GetDateTime() => throw new NotImplementedException();
			}

			[Wrapper]
			private class MySqlParameter
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
			internal class MySqlConnection
			{
			}

			[Wrapper]
			internal class MySqlTransaction
			{
			}

			#region BulkCopy
			[Wrapper]
			internal class MySqlBulkCopy : TypeWrapper
			{
				private static object[] Wrappers { get; }
					= new object[]
				{
					// [0]: WriteToServer
					(Expression<Action<MySqlBulkCopy, IDataReader>>               )((MySqlBulkCopy this_, IDataReader            dataReader) => this_.WriteToServer(dataReader)),
					// [1]: get NotifyAfter
					(Expression<Func<MySqlBulkCopy, int>>                         )((MySqlBulkCopy this_                                   ) => this_.NotifyAfter),
					// [2]: get BulkCopyTimeout
					(Expression<Func<MySqlBulkCopy, int>>                         )((MySqlBulkCopy this_                                   ) => this_.BulkCopyTimeout),
					// [3]: get DestinationTableName
					(Expression<Func<MySqlBulkCopy, string?>>                     )((MySqlBulkCopy this_                                   ) => this_.DestinationTableName),
					// [4]: this.ColumnMappings.Add(column)
					(Expression<Action<MySqlBulkCopy, MySqlBulkCopyColumnMapping>>)((MySqlBulkCopy this_, MySqlBulkCopyColumnMapping column) => this_.ColumnMappings.Add(column)),
					// [5]: set NotifyAfter
					PropertySetter((MySqlBulkCopy this_) => this_.NotifyAfter),
					// [6]: set BulkCopyTimeout
					PropertySetter((MySqlBulkCopy this_) => this_.BulkCopyTimeout),
					// [7]: set DestinationTableName
					PropertySetter((MySqlBulkCopy this_) => this_.DestinationTableName),
					// [8]: WriteToServerAsync
					new Tuple<LambdaExpression, bool>
					((Expression<Func<MySqlBulkCopy, IDataReader, CancellationToken, Task>>     )((MySqlBulkCopy this_, IDataReader dataReader, CancellationToken cancellationToken) => this_.WriteToServerAsync (dataReader, cancellationToken)), true),
#if !NET45 && !NET46
					// [9]: WriteToServerAsync
					new Tuple<LambdaExpression, bool>
					((Expression<Func<MySqlBulkCopy, IDataReader, CancellationToken, ValueTask>>)((MySqlBulkCopy this_, IDataReader dataReader, CancellationToken cancellationToken) => this_.WriteToServerAsync2(dataReader, cancellationToken)), true),
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

				public void WriteToServer(IDataReader dataReader) => ((Action<MySqlBulkCopy, IDataReader>)CompiledWrappers[0])(this, dataReader);
				public Task WriteToServerAsync      (IDataReader dataReader, CancellationToken cancellationToken) => ((Func<MySqlBulkCopy, IDataReader, CancellationToken,      Task>)CompiledWrappers[8])(this, dataReader, cancellationToken);
				public bool CanWriteToServerAsync => CompiledWrappers[8] != null;
#if !NET45 && !NET46
				public ValueTask WriteToServerAsync2(IDataReader dataReader, CancellationToken cancellationToken) => ((Func<MySqlBulkCopy, IDataReader, CancellationToken, ValueTask>)CompiledWrappers[9])(this, dataReader, cancellationToken);
				public bool CanWriteToServerAsync2 => CompiledWrappers[9] != null;
#endif

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
			public class MySqlRowsCopiedEventArgs : TypeWrapper
			{
				private static LambdaExpression[] Wrappers { get; }
					= new LambdaExpression[]
				{
					// [0]: get RowsCopied
					(Expression<Func<MySqlRowsCopiedEventArgs, long>> )((MySqlRowsCopiedEventArgs this_) => this_.RowsCopied),
					// [1]: get Abort
					(Expression<Func<MySqlRowsCopiedEventArgs, bool>>)((MySqlRowsCopiedEventArgs this_) => this_.Abort),
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
		internal class MySqlBulkCopyColumnMapping : TypeWrapper
		{
			public MySqlBulkCopyColumnMapping(object instance) : base(instance, null)
			{
			}

			public MySqlBulkCopyColumnMapping(int sourceOrdinal, string destinationColumn, string? expression = null) => throw new NotImplementedException();
		}
	}
}
