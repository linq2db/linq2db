using System;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using System.Threading;

using LinqToDB.Common;     // ⇒ ActivatorExt, Tools
using LinqToDB.Common.Internal;
using LinqToDB.Mapping;
using LinqToDB.Expressions.Types;
using System.Linq.Expressions;

namespace LinqToDB.DataProvider.Ydb
{
	public sealed class YdbProviderAdapter : IDynamicProviderAdapter
	{
		static readonly object _syncRoot = new();
		static YdbProviderAdapter? _instance;

		public const string AssemblyName    = "Ydb.Sdk";
		public const string ClientNamespace = "Ydb.Sdk.Ado";
		internal Func<DbConnection, string, YdbBinaryImporter>? BeginBinaryImport { get; }
		internal Func<DbConnection, string, CancellationToken, Task<YdbBinaryImporter>>? BeginBinaryImportAsync { get; }

		// YDB Data types
		public Type? YdbDateType { get; }
		public Type? YdbDateTimeType { get; }
		public Type? YdbIntervalType { get; }
		//---------------------------------------------------------------------

		YdbProviderAdapter(
			Type connectionType,
			Type dataReaderType,
			Type parameterType,
			Type commandType,
			Type? transactionType,
			Func<string, DbConnection> connectionFactory,
			MappingSchema mappingSchema,
			Type? ydbDateType,
			Type? ydbDateTimeType,
			Type? ydbIntervalType,
			Func<DbConnection, string, YdbBinaryImporter>? beginBinaryImport,
			Func<DbConnection, string, CancellationToken, Task<YdbBinaryImporter>>? beginBinaryImportAsync
			)
		{
			ConnectionType = connectionType;
			DataReaderType = dataReaderType;
			ParameterType = parameterType;
			CommandType = commandType;
			TransactionType = transactionType;
			_connectionFactory = connectionFactory;
			MappingSchema = mappingSchema;
			YdbDateType = ydbDateType;
			YdbDateTimeType = ydbDateTimeType;
			YdbIntervalType = ydbIntervalType;
			BeginBinaryImport = beginBinaryImport;
			BeginBinaryImportAsync = beginBinaryImportAsync;
		}

		#region IDynamicProviderAdapter

		public Type ConnectionType { get; }
		public Type DataReaderType { get; }
		public Type ParameterType { get; }
		public Type CommandType { get; }
		public Type? TransactionType { get; }

		readonly Func<string, DbConnection> _connectionFactory;
		public DbConnection CreateConnection(string connectionString)
			=> _connectionFactory(connectionString);

		#endregion

		public MappingSchema MappingSchema { get; }

		//---------------------------------------------------------------------

		public static YdbProviderAdapter GetInstance()
		{
			if (_instance != null)
				return _instance;

			lock (_syncRoot)
				return _instance ??= CreateAdapter();
		}

		//---------------------------------------------------------------------

		static YdbProviderAdapter CreateAdapter()
		{
            var assembly = Common.Tools.TryLoadAssembly(AssemblyName, null)
                ?? throw new InvalidOperationException($"Cannot load assembly {AssemblyName}.");

			var connectionType = assembly.GetType($"{ClientNamespace}.YdbConnection", true)!;
			var commandType    = assembly.GetType($"{ClientNamespace}.YdbCommand",    true)!;
			var parameterType  = assembly.GetType($"{ClientNamespace}.YdbParameter",  true)!;
			var dataReaderType = assembly.GetType($"{ClientNamespace}.YdbDataReader", true)!;
			Type? transactionType = assembly.GetType($"{ClientNamespace}.YdbTransaction", false);

			var csBuilderType  = assembly.GetType($"{ClientNamespace}.YdbConnectionStringBuilder", true)!;

			//------------------------------------------------------------------
			// Fabric DbConnection
			//------------------------------------------------------------------
			Func<string, DbConnection> connectionFactory;

			// 1) YdbConnection(string)
			if (connectionType.GetConstructor(new[] { typeof(string) }) != null)
			{
				connectionFactory = connStr =>
					(DbConnection)ActivatorExt.CreateInstance(connectionType, connStr);
			}
			// 2) YdbConnection(YdbConnectionStringBuilder)
			else if (connectionType.GetConstructor(new[] { csBuilderType }) != null)
			{
				// a) YdbConnectionStringBuilder(string)
				if (csBuilderType.GetConstructor(new[] { typeof(string) }) != null)
				{
					connectionFactory = connStr =>
					{
						var builder = ActivatorExt.CreateInstance(csBuilderType, connStr);
						return (DbConnection)ActivatorExt.CreateInstance(connectionType, builder);
					};
				}
				// b) Builder without constructor => set ConnectionString manually
				else
				{
					connectionFactory = connStr =>
					{
						dynamic builder = ActivatorExt.CreateInstance(csBuilderType);
						if (builder is DbConnectionStringBuilder b)
							b.ConnectionString = connStr;
						else
							csBuilderType.GetProperty("ConnectionString")?
										 .SetValue(builder, connStr);

						return (DbConnection)ActivatorExt.CreateInstance(connectionType, builder);
					};
				}
			}
			else
				throw new InvalidOperationException(
					"No suitable constructor found YdbConnection.");

			var ydbDateType = assembly.GetType($"{ClientNamespace}.YdbDate", false);
			var ydbDateTimeType = assembly.GetType($"{ClientNamespace}.YdbDateTime", false);
			var ydbIntervalType = assembly.GetType($"{ClientNamespace}.YdbInterval", false);

			//------------------------------------------------------------------
			// Base MappingSchema
			//------------------------------------------------------------------
			var ms = new MappingSchema();

			// Scalar methods
			ms.AddScalarType(typeof(string), DataType.NVarChar);
			ms.AddScalarType(typeof(bool), DataType.Boolean);
			ms.AddScalarType(typeof(DateTime), DataType.DateTime2);
			ms.AddScalarType(typeof(TimeSpan), DataType.Time);

			// YDB types
			if (ydbDateType != null)
				ms.AddScalarType(ydbDateType, DataType.Date);
			if (ydbDateTimeType != null)
				ms.AddScalarType(ydbDateTimeType, DataType.DateTime2);
			if (ydbIntervalType != null)
				ms.AddScalarType(ydbIntervalType, DataType.Interval);

			// BulkCopy
			Func<DbConnection, string, YdbBinaryImporter>? beginBinaryImport = null;
			Func<DbConnection, string, CancellationToken, Task<YdbBinaryImporter>>? beginBinaryImportAsync = null;

			var bulkCopyType = assembly.GetType($"{ClientNamespace}.YdbBulkCopy", false);
			if (bulkCopyType != null)
			{
				var typeMapper = new TypeMapper();
				typeMapper.RegisterTypeWrapper<YdbBinaryImporter>(bulkCopyType);

				var pConnection = Expression.Parameter(typeof(DbConnection));
				var pCommand = Expression.Parameter(typeof(string));

				beginBinaryImport = Expression.Lambda<Func<DbConnection, string, YdbBinaryImporter>>(
					typeMapper.MapExpression((DbConnection conn, string cmd) =>
						typeMapper.Wrap<YdbBinaryImporter>(((YdbConnection)(object)conn).BeginBulkCopy(cmd)),
					pConnection, pCommand),
					pConnection, pCommand).CompileExpression();
			}

			return new YdbProviderAdapter(
				connectionType,
				dataReaderType,
				parameterType,
				commandType,
				transactionType,
				connectionFactory,
				ms,
				ydbDateType,
				ydbDateTimeType,
				ydbIntervalType,
				beginBinaryImport,
				beginBinaryImportAsync);
		}

		[Wrapper]
		public class YdbBinaryImporter : TypeWrapper, IDisposable
		{
			public YdbBinaryImporter(object instance, Delegate[] wrappers) : base(instance, wrappers) { }

			public void Dispose() => ((Action<YdbBinaryImporter>)CompiledWrappers[0])(this);
			public void WriteRow(params object[] values) => ((Action<YdbBinaryImporter, object[]>)CompiledWrappers[1])(this, values);
		}

		// Connection wrapper
		[Wrapper]
		public class YdbConnection : TypeWrapper
		{
			public YdbConnection(object instance, Delegate[] wrappers) : base(instance, wrappers) { }
			public YdbBinaryImporter BeginBulkCopy(string command) => throw new NotImplementedException();
		}
	}
}
