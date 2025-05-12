using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Common;
using LinqToDB.Common.Internal;
using LinqToDB.Expressions.Types;
using LinqToDB.Extensions;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

using static LinqToDB.DataProvider.OdbcProviderAdapter;

namespace LinqToDB.DataProvider.SapHana
{
	// TODO: add HanaDecimal support
	// TODO: add ReadVector support
	public class SapHanaProviderAdapter : IDynamicProviderAdapter
	{
		private static readonly Lock _unmanagedSyncRoot = new ();
		private static readonly Lock _odbcSyncRoot      = new ();

		private static SapHanaProviderAdapter? _unmanagedProvider;
		private static SapHanaProviderAdapter? _odbcProvider;

#if NETFRAMEWORK
		public  static readonly IEnumerable<string> UnmanagedAssemblyNames = ["Sap.Data.Hana.v4.5"];
#elif NET8_0_OR_GREATER
		public  static readonly IEnumerable<string> UnmanagedAssemblyNames = ["Sap.Data.Hana.Net.v8.0", "Sap.Data.Hana.Net.v6.0", "Sap.Data.Hana.Core.v2.1"];
#else
		public  static readonly IEnumerable<string> UnmanagedAssemblyNames = ["Sap.Data.Hana.Core.v2.1"];
#endif

		public  const string UnmanagedClientNamespace     = "Sap.Data.Hana";
		private const string UnmanagedProviderFactoryName = "Sap.Data.Hana";

		private SapHanaProviderAdapter(
			Type connectionType,
			Type dataReaderType,
			Type parameterType,
			Type commandType,
			Type transactionType,
			Func<string, DbConnection> connectionFactory,

			Action<DbParameter, HanaDbType> dbTypeSetter,
			Func<DbParameter, HanaDbType> dbTypeGetter,

			Func<DbConnection, HanaBulkCopyOptions, DbTransaction?, HanaBulkCopy> bulkCopyCreator,
			Func<int, string, HanaBulkCopyColumnMapping>                          bulkCopyColumnMappingCreator,
			
			string? getDateTimeOffsetMethod,
			string? getHanaDecimalMethod,
			string? getRealVectorMethod,
			string? getTimeSpanMethod,
			
			MappingSchema? mappingSchema,
			Type? hanaDecimalType)
		{
			ConnectionType     = connectionType;
			DataReaderType     = dataReaderType;
			ParameterType      = parameterType;
			CommandType        = commandType;
			TransactionType    = transactionType;
			_connectionFactory = connectionFactory;

			SetDbType = dbTypeSetter;
			GetDbType = dbTypeGetter;

			CreateBulkCopy              = bulkCopyCreator;
			CreateBulkCopyColumnMapping = bulkCopyColumnMappingCreator;

			GetDateTimeOffsetMethod = getDateTimeOffsetMethod;
			GetHanaDecimalMethod    = getHanaDecimalMethod;
			GetRealVectorMethod     = getRealVectorMethod;
			GetTimeSpanMethod       = getTimeSpanMethod;

			MappingSchema   = mappingSchema;
			HanaDecimalType = hanaDecimalType;
		}

		private SapHanaProviderAdapter(OdbcProviderAdapter odbcProviderAdapter)
		{
			ConnectionType     = odbcProviderAdapter.ConnectionType;
			DataReaderType     = odbcProviderAdapter.DataReaderType;
			ParameterType      = odbcProviderAdapter.ParameterType;
			CommandType        = odbcProviderAdapter.CommandType;
			TransactionType    = odbcProviderAdapter.TransactionType;
			_connectionFactory = odbcProviderAdapter.CreateConnection;

			SetOdbcDbType = odbcProviderAdapter.SetDbType;
			GetOdbcDbType = odbcProviderAdapter.GetDbType;
		}

		#region IDynamicProviderAdapter

		public Type ConnectionType  { get; }
		public Type DataReaderType  { get; }
		public Type ParameterType   { get; }
		public Type CommandType     { get; }
		public Type TransactionType { get; }

		readonly Func<string, DbConnection> _connectionFactory;
		public DbConnection CreateConnection(string connectionString) => _connectionFactory(connectionString);

#endregion

		public string? GetDateTimeOffsetMethod { get; }
		public string? GetHanaDecimalMethod    { get; }
		public string? GetRealVectorMethod     { get; }
		public string? GetTimeSpanMethod       { get; }

		public MappingSchema? MappingSchema { get; }

		public Type? HanaDecimalType { get; }

		public Action<DbParameter, HanaDbType>? SetDbType { get; }
		public Func<DbParameter, HanaDbType>?   GetDbType { get; }

		public Action<DbParameter, OdbcType>? SetOdbcDbType   { get; }
		public Func<DbParameter, OdbcType>?   GetOdbcDbType   { get; }

		internal Func<DbConnection, HanaBulkCopyOptions, DbTransaction?, HanaBulkCopy>? CreateBulkCopy              { get; }
		public   Func<int, string, HanaBulkCopyColumnMapping>?                          CreateBulkCopyColumnMapping { get; }

		internal static SapHanaProviderAdapter GetInstance(SapHanaProvider provider)
		{
			if (provider == SapHanaProvider.ODBC)
			{
				if (_odbcProvider == null)
				{
					lock (_odbcSyncRoot)
#pragma warning disable CA1508 // Avoid dead conditional code
						_odbcProvider ??= new SapHanaProviderAdapter(OdbcProviderAdapter.GetInstance());
#pragma warning restore CA1508 // Avoid dead conditional code
				}

				return _odbcProvider;
			}
			else
			{
				if (_unmanagedProvider == null)
				{
					lock (_unmanagedSyncRoot)
#pragma warning disable CA1508 // Avoid dead conditional code
						if (_unmanagedProvider == null)
#pragma warning restore CA1508 // Avoid dead conditional code
						{
							MappingSchema? ms = null;

							Assembly? assembly = null;
							foreach (var assemblyName in UnmanagedAssemblyNames)
							{
								assembly = Common.Tools.TryLoadAssembly(assemblyName, UnmanagedProviderFactoryName);

								if (assembly != null)
									break;
							}

							if (assembly == null)
								throw new InvalidOperationException($"Cannot load assembly by name(s) {string.Join(", ", UnmanagedAssemblyNames)}");

							var connectionType  = assembly.GetType($"{UnmanagedClientNamespace}.HanaConnection" , true)!;
							var dataReaderType  = assembly.GetType($"{UnmanagedClientNamespace}.HanaDataReader" , true)!;
							var parameterType   = assembly.GetType($"{UnmanagedClientNamespace}.HanaParameter"  , true)!;
							var commandType     = assembly.GetType($"{UnmanagedClientNamespace}.HanaCommand"    , true)!;
							var transactionType = assembly.GetType($"{UnmanagedClientNamespace}.HanaTransaction", true)!;
							var dbType          = assembly.GetType($"{UnmanagedClientNamespace}.HanaDbType"     , true)!;

							var bulkCopyType                    = assembly.GetType($"{UnmanagedClientNamespace}.HanaBulkCopy"                       , true)!;
							var bulkCopyOptionsType             = assembly.GetType($"{UnmanagedClientNamespace}.HanaBulkCopyOptions"                , true)!;
							var bulkCopyColumnMappingType       = assembly.GetType($"{UnmanagedClientNamespace}.HanaBulkCopyColumnMapping"          , true)!;
							var rowsCopiedEventHandlerType      = assembly.GetType($"{UnmanagedClientNamespace}.HanaRowsCopiedEventHandler"         , true)!;
							var rowsCopiedEventArgs             = assembly.GetType($"{UnmanagedClientNamespace}.HanaRowsCopiedEventArgs"            , true)!;
							var bulkCopyColumnMappingCollection = assembly.GetType($"{UnmanagedClientNamespace}.HanaBulkCopyColumnMappingCollection", true)!;

							var hanaDecimalType = LoadType("HanaDecimal", type => new DbDataType(type, DataType.Decimal, dbType: null, length: null, precision: 38, scale: 10),
								// this currently doesn't work for client-to-db conversions
								null/*ReflectionExtensions.GetDefaultValue*/,
								optional: true, register: true);

							var typeMapper = new TypeMapper();

							typeMapper.RegisterTypeWrapper<HanaConnection>(connectionType);
							typeMapper.RegisterTypeWrapper<HanaTransaction>(transactionType);
							typeMapper.RegisterTypeWrapper<HanaParameter>(parameterType);
							typeMapper.RegisterTypeWrapper<HanaDbType>(dbType);

							// bulk copy types
							typeMapper.RegisterTypeWrapper<HanaBulkCopy>(bulkCopyType);
							typeMapper.RegisterTypeWrapper<HanaRowsCopiedEventArgs>(rowsCopiedEventArgs);
							typeMapper.RegisterTypeWrapper<HanaRowsCopiedEventHandler>(rowsCopiedEventHandlerType);
							typeMapper.RegisterTypeWrapper<HanaBulkCopyColumnMappingCollection>(bulkCopyColumnMappingCollection);
							typeMapper.RegisterTypeWrapper<HanaBulkCopyOptions>(bulkCopyOptionsType);
							typeMapper.RegisterTypeWrapper<HanaBulkCopyColumnMapping>(bulkCopyColumnMappingType);

							if (hanaDecimalType != null)
								typeMapper.RegisterTypeWrapper<HanaDecimal>(hanaDecimalType);

							typeMapper.FinalizeMappings();

							if (hanaDecimalType != null)
							{
								var decimalConverter = typeMapper.BuildFunc<object, string>(typeMapper.MapLambda((object value) => ((HanaDecimal)value).ToString()));

								ms!.SetValueToSqlConverter(hanaDecimalType, (sb, dt, _, v) =>
								{
									// because sap developers never heard about cultures, we must ensure that decimal.ToString will not result in invalid literal
									using var r = new InvariantCultureRegion(null);
									sb.Append(decimalConverter(v));
								});
							}

							var connectionFactory = typeMapper.BuildTypedFactory<string, HanaConnection, DbConnection>((string connectionString) => new HanaConnection(connectionString));

							var typeProperty = typeMapper.Type<HanaParameter>().Member(p => p.HanaDbType);

							_unmanagedProvider = new SapHanaProviderAdapter(
								connectionType,
								dataReaderType,
								parameterType,
								commandType,
								transactionType,
								connectionFactory,
								typeProperty.BuildSetter<DbParameter>(),
								typeProperty.BuildGetter<DbParameter>(),
								typeMapper.BuildWrappedFactory((DbConnection connection, HanaBulkCopyOptions options, DbTransaction? transaction) => new HanaBulkCopy((HanaConnection)(object)connection, options, (HanaTransaction?)(object?)transaction)),
								typeMapper.BuildWrappedFactory((int source, string destination) => new HanaBulkCopyColumnMapping(source, destination)),

								dataReaderType.GetMethod("GetDateTimeOffset")?.Name,
								dataReaderType.GetMethod("GetHanaDecimal")?.Name,
								dataReaderType.GetMethod("GetRealVector")?.Name,
								dataReaderType.GetMethod("GetTimeSpan")?.Name,
								
								ms,
								hanaDecimalType);

							Type? LoadType(string typeName, Func<Type, DbDataType> getDataType, Func<Type, object?>? getNullValue = null, bool optional = false, bool register = true)
							{
								var type = assembly!.GetType($"{UnmanagedClientNamespace}.{typeName}", !optional);

								if (type == null)
									return null;

								if (register)
								{
									(ms ??= new()).AddScalarType(type, new SqlDataType(getDataType(type)));
									if (getNullValue != null)
										ms.SetDefaultValue(type, getNullValue(type));
								}

								return type;
							}
						}
				}

				return _unmanagedProvider;
			}

		}

		[Wrapper]
		public class HanaTransaction
		{
		}

		[Wrapper]
		internal sealed class HanaConnection
		{
			public HanaConnection(string connectionString) => throw new NotImplementedException();
		}

		[Wrapper]
		private sealed class HanaDecimal
		{
			public override string ToString() => throw new NotImplementedException();
		}

		[Wrapper]
		private sealed class HanaParameter
		{
			public HanaDbType HanaDbType { get; set; }
		}

		// note that hana provider devs don't respect numeric values and could change them with
		// new release of client (like they did with RealVector type addition)
		// so we rely on value names mapping actually
		[Wrapper]
		public enum HanaDbType
		{
			AlphaNum     = 1,
			BigInt       = 2,
			Blob         = 3,
			Boolean      = 4,
			Clob         = 5,
			Date         = 6,
			Decimal      = 7,
			Double       = 8,
			Integer      = 9,
			NClob        = 10,
			NVarChar     = 11,
			Real         = 12,
			RealVector   = 13,
			SecondDate   = 14,
			ShortText    = 15,
			SmallDecimal = 16,
			SmallInt     = 17,
			Text         = 18,
			Time         = 19,
			TimeStamp    = 20,
			TinyInt      = 21,
			VarBinary    = 22,
			VarChar      = 23,
			TableType    = 24,
		}

		#region BulkCopy
		[Wrapper]
		internal class HanaBulkCopy : TypeWrapper, IDisposable
		{
			private static object[] Wrappers { get; }
				= new object[]
			{
				// [0]: Dispose
				(Expression<Action<HanaBulkCopy>>                                   )((HanaBulkCopy this_                    ) => ((IDisposable)this_).Dispose()),
				// [1]: WriteToServer
				(Expression<Action<HanaBulkCopy, IDataReader>>                      )((HanaBulkCopy this_, IDataReader reader) => this_.WriteToServer(reader)),
				// [2]: get NotifyAfter
				(Expression<Func<HanaBulkCopy, int>>                                )((HanaBulkCopy this_                    ) => this_.NotifyAfter),
				// [3]: get BatchSize
				(Expression<Func<HanaBulkCopy, int>>                                )((HanaBulkCopy this_                    ) => this_.BatchSize),
				// [4]: get BulkCopyTimeout
				(Expression<Func<HanaBulkCopy, int>>                                )((HanaBulkCopy this_                    ) => this_.BulkCopyTimeout),
				// [5]: get DestinationTableName
				(Expression<Func<HanaBulkCopy, string?>>                            )((HanaBulkCopy this_                    ) => this_.DestinationTableName),
				// [6]: get ColumnMappings
				(Expression<Func<HanaBulkCopy, HanaBulkCopyColumnMappingCollection>>)((HanaBulkCopy this_                    ) => this_.ColumnMappings),
				// [7]: set NotifyAfter
				PropertySetter((HanaBulkCopy this_) => this_.NotifyAfter),
				// [8]: set BatchSize
				PropertySetter((HanaBulkCopy this_) => this_.BatchSize),
				// [9]: set BulkCopyTimeout
				PropertySetter((HanaBulkCopy this_) => this_.BulkCopyTimeout),
				// [10]: set DestinationTableName
				PropertySetter((HanaBulkCopy this_) => this_.DestinationTableName),
				// [11]: WriteToServerAsync
				new Tuple<LambdaExpression, bool>
				((Expression<Func<HanaBulkCopy, IDataReader, CancellationToken, Task>>)((HanaBulkCopy this_, IDataReader reader, CancellationToken cancellationToken) => this_.WriteToServerAsync(reader, cancellationToken)), true),
			};

			private static string[] Events { get; }
				= new[]
			{
				nameof(HanaRowsCopied)
			};

			public HanaBulkCopy(object instance, Delegate[] wrappers) : base(instance, wrappers)
			{
			}

			public HanaBulkCopy(HanaConnection connection, HanaBulkCopyOptions options, HanaTransaction? transaction) => throw new NotImplementedException();

			public void Dispose      ()                       => ((Action<HanaBulkCopy>)CompiledWrappers[0])(this);
#pragma warning disable RS0030 // API mapping must preserve type
			public void WriteToServer(IDataReader dataReader) => ((Action<HanaBulkCopy, IDataReader>)CompiledWrappers[1])(this, dataReader);
#pragma warning restore RS0030 //  API mapping must preserve type

			public bool CanWriteToServerAsync => CompiledWrappers[11] != null;
#pragma warning disable RS0030 // API mapping must preserve type
			public Task WriteToServerAsync(IDataReader dataReader, CancellationToken cancellationToken)
				=> ((Func<HanaBulkCopy, IDataReader, CancellationToken, Task>)CompiledWrappers[11])(this, dataReader, cancellationToken);
#pragma warning restore RS0030 //  API mapping must preserve type

			public int NotifyAfter
			{
				get => ((Func  <HanaBulkCopy, int>)CompiledWrappers[2])(this);
				set => ((Action<HanaBulkCopy, int>)CompiledWrappers[7])(this, value);
			}

			public int BatchSize
			{
				get => ((Func  <HanaBulkCopy, int>)CompiledWrappers[3])(this);
				set => ((Action<HanaBulkCopy, int>)CompiledWrappers[8])(this, value);
			}

			public int BulkCopyTimeout
			{
				get => ((Func  <HanaBulkCopy, int>)CompiledWrappers[4])(this);
				set => ((Action<HanaBulkCopy, int>)CompiledWrappers[9])(this, value);
			}

			public string? DestinationTableName
			{
				get => ((Func  <HanaBulkCopy, string?>)CompiledWrappers[5] )(this);
				set => ((Action<HanaBulkCopy, string?>)CompiledWrappers[10])(this, value);
			}

			public HanaBulkCopyColumnMappingCollection ColumnMappings => ((Func<HanaBulkCopy, HanaBulkCopyColumnMappingCollection>)CompiledWrappers[6])(this);

			private      HanaRowsCopiedEventHandler? _HanaRowsCopied;
			public event HanaRowsCopiedEventHandler?  HanaRowsCopied
			{
				add    => _HanaRowsCopied = (HanaRowsCopiedEventHandler?)Delegate.Combine(_HanaRowsCopied, value);
				remove => _HanaRowsCopied = (HanaRowsCopiedEventHandler?)Delegate.Remove (_HanaRowsCopied, value);
			}
		}

		[Wrapper]
		public class HanaRowsCopiedEventArgs : TypeWrapper
		{
			private static LambdaExpression[] Wrappers { get; }
				= new LambdaExpression[]
			{
				// [0]: get RowsCopied
				(Expression<Func<HanaRowsCopiedEventArgs, long>>)((HanaRowsCopiedEventArgs this_) => this_.RowsCopied),
				// [1]: get Abort
				(Expression<Func<HanaRowsCopiedEventArgs, bool>>)((HanaRowsCopiedEventArgs this_) => this_.Abort),
				// [2]: set Abort
				PropertySetter((HanaRowsCopiedEventArgs this_) => this_.Abort),
			};

			public HanaRowsCopiedEventArgs(object instance, Delegate[] wrappers) : base(instance, wrappers)
			{
			}

			public long RowsCopied => ((Func<HanaRowsCopiedEventArgs, long>)CompiledWrappers[0])(this);

			public bool Abort
			{
				get => ((Func  <HanaRowsCopiedEventArgs, bool>)CompiledWrappers[1])(this);
				set => ((Action<HanaRowsCopiedEventArgs, bool>)CompiledWrappers[2])(this, value);
			}
		}

		[Wrapper]
		public delegate void HanaRowsCopiedEventHandler(object sender, HanaRowsCopiedEventArgs e);

		[Wrapper]
		public class HanaBulkCopyColumnMappingCollection : TypeWrapper
		{
			private static LambdaExpression[] Wrappers { get; }
				= new LambdaExpression[]
			{
				// [0]: Add
				(Expression<Func<HanaBulkCopyColumnMappingCollection, HanaBulkCopyColumnMapping, HanaBulkCopyColumnMapping>>)((HanaBulkCopyColumnMappingCollection this_, HanaBulkCopyColumnMapping column) => this_.Add(column)),
			};

			public HanaBulkCopyColumnMappingCollection(object instance, Delegate[] wrappers) : base(instance, wrappers)
			{
			}

			public HanaBulkCopyColumnMapping Add(HanaBulkCopyColumnMapping bulkCopyColumnMapping) => ((Func<HanaBulkCopyColumnMappingCollection, HanaBulkCopyColumnMapping, HanaBulkCopyColumnMapping>)CompiledWrappers[0])(this, bulkCopyColumnMapping);
		}

		[Wrapper, Flags]
		public enum HanaBulkCopyOptions
		{
			Default                = 0,
			KeepIdentity           = 1,
			TableLock              = 2,
			UseInternalTransaction = 4
		}

		[Wrapper]
		public class HanaBulkCopyColumnMapping : TypeWrapper
		{
			public HanaBulkCopyColumnMapping(object instance) : base(instance, null)
			{
			}

			public HanaBulkCopyColumnMapping(int source, string destination) => throw new NotImplementedException();
		}

		#endregion
	}
}
