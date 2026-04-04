using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
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

			InitAppender(assembly);
		}

		void InitAppender(System.Reflection.Assembly assembly)
		{
			var appenderType    = assembly.GetType($"{ClientNamespace}.DuckDBAppender", false);
			var appenderRowType = assembly.GetType($"{ClientNamespace}.IDuckDBAppenderRow", false);

			if (appenderType == null || appenderRowType == null)
				return;

			// connection.CreateAppender(string? schema, string table) → IDisposable
			var createAppenderMethod = ConnectionType.GetMethod("CreateAppender", [typeof(string), typeof(string)]);
			if (createAppenderMethod == null)
				return;

			var pConn   = Expression.Parameter(typeof(DbConnection));
			var pSchema = Expression.Parameter(typeof(string));
			var pTable  = Expression.Parameter(typeof(string));
			_createAppender = Expression.Lambda<Func<DbConnection, string?, string, IDisposable>>(
				Expression.Convert(
					Expression.Call(Expression.Convert(pConn, ConnectionType), createAppenderMethod, pSchema, pTable),
					typeof(IDisposable)),
				pConn, pSchema, pTable).Compile();

			// appender.CreateRow() → object
			var createRowMethod = appenderType.GetMethod("CreateRow");
			if (createRowMethod != null)
			{
				var p = Expression.Parameter(typeof(IDisposable));
				_createAppenderRow = Expression.Lambda<Func<IDisposable, object>>(
					Expression.Convert(
						Expression.Call(Expression.Convert(p, appenderType), createRowMethod),
						typeof(object)),
					p).Compile();
			}

			// appender.Close()
			var closeMethod = appenderType.GetMethod("Close");
			if (closeMethod != null)
			{
				var p = Expression.Parameter(typeof(IDisposable));
				_closeAppender = Expression.Lambda<Action<IDisposable>>(
					Expression.Call(Expression.Convert(p, appenderType), closeMethod),
					p).Compile();
			}

			// row.EndRow() — returns void
			var endRowMethod = appenderRowType.GetMethod("EndRow");
			if (endRowMethod != null)
			{
				var p = Expression.Parameter(typeof(object));
				_endRow = Expression.Lambda<Action<object>>(
					Expression.Call(Expression.Convert(p, appenderRowType), endRowMethod),
					p).Compile();
			}

			// row.AppendNullValue() — returns IDuckDBAppenderRow (discard)
			var appendNullMethod = appenderRowType.GetMethod("AppendNullValue");
			if (appendNullMethod != null)
			{
				var p = Expression.Parameter(typeof(object));
				_appendNull = Expression.Lambda<Action<object>>(
					Expression.Block(typeof(void),
						Expression.Call(Expression.Convert(p, appenderRowType), appendNullMethod)),
					p).Compile();
			}

			// row.AppendDefault() — returns IDuckDBAppenderRow (discard)
			var appendDefaultMethod = appenderRowType.GetMethod("AppendDefault");
			if (appendDefaultMethod != null)
			{
				var p = Expression.Parameter(typeof(object));
				_appendDefault = Expression.Lambda<Action<object>>(
					Expression.Block(typeof(void),
						Expression.Call(Expression.Convert(p, appenderRowType), appendDefaultMethod)),
					p).Compile();
			}

			_appendValueActions = BuildAppendValueDelegates(appenderRowType);
		}

		static Dictionary<Type, Action<object, object>> BuildAppendValueDelegates(Type rowType)
		{
			var result = new Dictionary<Type, Action<object, object>>();

			var rowParam   = Expression.Parameter(typeof(object));
			var valueParam = Expression.Parameter(typeof(object));

			foreach (var method in rowType.GetMethods())
			{
				if (method.Name != "AppendValue" || method.IsGenericMethod)
					continue;

				var parameters = method.GetParameters();
				if (parameters.Length != 1)
					continue;

				var paramType = parameters[0].ParameterType;

				// Determine the base type to use as dictionary key
				Type baseType;
				var underlyingType = Nullable.GetUnderlyingType(paramType);
				if (underlyingType != null)
					baseType = underlyingType;             // Nullable<T> → T
				else if (!paramType.IsValueType)
					baseType = paramType;                  // reference type (string, byte[])
				else
					continue;                              // non-nullable value type (Span<byte>) — skip

				// Skip DuckDB-specific types (DuckDBDateOnly, DuckDBTimeOnly)
				if (baseType.FullName?.StartsWith("DuckDB", StringComparison.Ordinal) == true)
					continue;

				Expression valueExpr = baseType.IsValueType
					? Expression.Convert(Expression.Convert(valueParam, baseType), paramType)
					: Expression.Convert(valueParam, baseType);

				var call = Expression.Call(
					Expression.Convert(rowParam, rowType),
					method,
					valueExpr);

				result[baseType] = Expression.Lambda<Action<object, object>>(
					Expression.Block(typeof(void), call),
					rowParam, valueParam).Compile();
			}

			return result;
		}

		#region Appender

		Func<DbConnection, string?, string, IDisposable>?  _createAppender;
		Func<IDisposable, object>?                         _createAppenderRow;
		Action<IDisposable>?                               _closeAppender;
		Action<object>?                                    _endRow;
		Action<object>?                                    _appendNull;
		Action<object>?                                    _appendDefault;
		IReadOnlyDictionary<Type, Action<object, object>>? _appendValueActions;

		internal bool SupportsAppender => _createAppender != null;

		internal IDisposable CreateAppender(DbConnection connection, string? schema, string table)
			=> _createAppender!(connection, schema, table);

		internal object CreateAppenderRow(IDisposable appender)
			=> _createAppenderRow!(appender);

		internal void CloseAppender(IDisposable appender)
			=> _closeAppender!(appender);

		internal void EndRow(object row)
			=> _endRow!(row);

		internal void AppendNull(object row)
			=> _appendNull!(row);

		internal void AppendDefault(object row)
			=> _appendDefault!(row);

		internal bool TryGetAppendValue(Type type, [NotNullWhen(true)] out Action<object, object>? action)
		{
			if (_appendValueActions != null && _appendValueActions.TryGetValue(type, out action))
				return true;
			action = null;
			return false;
		}

		#endregion

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
