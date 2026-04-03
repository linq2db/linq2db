using System;
using System.Data.Common;
using System.Threading;

using LinqToDB.Internal.Expressions.Types;

namespace LinqToDB.Internal.DataProvider.DuckDB
{
	public sealed class DuckDBProviderAdapter : IDynamicProviderAdapter
	{
		public const string AssemblyName    = "DuckDB.NET.Data";
		public const string ClientNamespace = "DuckDB.NET.Data";

		DuckDBProviderAdapter()
		{
			var assembly = Common.Tools.TryLoadAssembly(AssemblyName, null)
				?? throw new InvalidOperationException($"Cannot load assembly {AssemblyName}.");

			ConnectionType  = assembly.GetType($"{ClientNamespace}.DuckDBConnection" , true)!;
			DataReaderType  = assembly.GetType($"{ClientNamespace}.DuckDBDataReader" , true)!;
			ParameterType   = assembly.GetType($"{ClientNamespace}.DuckDBParameter"  , true)!;
			CommandType     = assembly.GetType($"{ClientNamespace}.DuckDBCommand"    , true)!;
			TransactionType = assembly.GetType($"{ClientNamespace}.DuckDBTransaction", true)!;

			var typeMapper = new TypeMapper();

			typeMapper.RegisterTypeWrapper<DuckDBConnection>(ConnectionType);
			typeMapper.FinalizeMappings();

			_connectionFactory = typeMapper.BuildTypedFactory<string, DuckDBConnection, DbConnection>(connectionString => new DuckDBConnection(connectionString));
		}

		static readonly Lazy<DuckDBProviderAdapter> _lazy = new (() => new ());
		internal static DuckDBProviderAdapter Instance => _lazy.Value;

		#region IDynamicProviderAdapter

		public Type ConnectionType  { get; }
		public Type DataReaderType  { get; }
		public Type ParameterType   { get; }
		public Type CommandType     { get; }
		public Type TransactionType { get; }

		readonly Func<string, DbConnection> _connectionFactory;
		public DbConnection CreateConnection(string connectionString) => _connectionFactory(connectionString);

		#endregion

		#region Wrappers

		[Wrapper]
		internal sealed class DuckDBConnection
		{
			public DuckDBConnection(string connectionString) => throw new NotSupportedException();
		}

		#endregion
	}
}
