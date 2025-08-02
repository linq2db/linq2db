using System;
using System.Data.Common;
using System.Threading.Tasks;

using LinqToDB.Internal.Expressions.Types;

namespace LinqToDB.Internal.DataProvider.Ydb
{
	/*
	 * Misc notes:
	 * - supported default isolation levels: Unspecified/Serializable (same behavior) === TxMode.SerializableRw
	 * 
	 * Optional/future features:
	 * - TODO: add provider-specific retry policy to support YdbException.IsTransientWhenIdempotent
	 * - TODO: add support for BeginTransaction(TxMode mode)
	 */
	public sealed class YdbProviderAdapter : IDynamicProviderAdapter
	{
		public const string AssemblyName    = "Ydb.Sdk";
		public const string ClientNamespace = "Ydb.Sdk.Ado";

		// custom reader methods
		internal const string GetBytes        = "GetBytes";
		internal const string GetSByte        = "GetSByte";
		internal const string GetUInt16       = "GetUInt16";
		internal const string GetUInt32       = "GetUInt32";
		internal const string GetUInt64       = "GetUInt64";
		internal const string GetInterval     = "GetInterval";
		internal const string GetJson         = "GetJson";
		internal const string GetJsonDocument = "GetJsonDocument";

		YdbProviderAdapter()
		{
			var assembly = Common.Tools.TryLoadAssembly(AssemblyName, null)
				?? throw new InvalidOperationException($"Cannot load assembly {AssemblyName}.");

			ConnectionType  = assembly.GetType($"{ClientNamespace}.YdbConnection",  true)!;
			CommandType     = assembly.GetType($"{ClientNamespace}.YdbCommand",     true)!;
			ParameterType   = assembly.GetType($"{ClientNamespace}.YdbParameter",   true)!;
			DataReaderType  = assembly.GetType($"{ClientNamespace}.YdbDataReader",  true)!;
			TransactionType = assembly.GetType($"{ClientNamespace}.YdbTransaction", true)!;

			var typeMapper = new TypeMapper();

			typeMapper.RegisterTypeWrapper<YdbConnection>(ConnectionType);

			typeMapper.FinalizeMappings();

			_connectionFactory = typeMapper.BuildTypedFactory<string, YdbConnection, DbConnection>(connectionString => new YdbConnection(connectionString));
			ClearAllPools      = typeMapper.BuildFunc<Task>(typeMapper.MapLambda(() => YdbConnection.ClearAllPools()));
			ClearPool          = typeMapper.BuildFunc<DbConnection, Task>(typeMapper.MapLambda((YdbConnection connection) => YdbConnection.ClearPool(connection)));
		}

		static readonly Lazy<YdbProviderAdapter> _lazy    = new (() => new ());
		internal static YdbProviderAdapter Instance => _lazy.Value;

		#region IDynamicProviderAdapter

		public Type ConnectionType  { get; }
		public Type DataReaderType  { get; }
		public Type ParameterType   { get; }
		public Type CommandType     { get; }
		public Type TransactionType { get; }

		readonly Func<string, DbConnection> _connectionFactory;
		public DbConnection CreateConnection(string connectionString) => _connectionFactory(connectionString);

		#endregion

		public Func<Task>               ClearAllPools { get; }
		public Func<DbConnection, Task> ClearPool     { get; }

		#region wrappers
		[Wrapper]
		internal sealed class YdbConnection
		{
			public YdbConnection(string connectionString) => throw new NotImplementedException();

			public static Task ClearAllPools() => throw new NotImplementedException();

			public static Task ClearPool(YdbConnection connection) => throw new NotImplementedException();
		}
		#endregion
	}
}
