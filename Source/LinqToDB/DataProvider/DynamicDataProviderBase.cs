using System;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.DataProvider
{
	using Common;
	using Data.RetryPolicy;
	using Extensions;
	using Mapping;

	public abstract class DynamicDataProviderBase<TProviderMappings> : DataProviderBase
		where TProviderMappings : IDynamicProviderAdapter
	{
		// DbDataReader method
		protected const string GetProviderSpecificValueReaderMethod = "GetProviderSpecificValue";

		protected DynamicDataProviderBase(string name, MappingSchema mappingSchema, TProviderMappings providerMappings)
			: base(name, mappingSchema)
		{
			Adapter = providerMappings;
		}

		public TProviderMappings Adapter { get; }

		public override string? ConnectionNamespace => Adapter.ConnectionType.Namespace;
		public override Type    DataReaderType      => Adapter.DataReaderType;

		Func<string, DbConnection>? _createConnection;

		protected override DbConnection CreateConnectionInternal(string connectionString)
		{
			if (_createConnection == null)
			{
				var l = CreateConnectionExpression(Adapter.ConnectionType);
				_createConnection = l.CompileExpression();
			}

			return _createConnection(connectionString);
		}

		private static Expression<Func<string, DbConnection>> CreateConnectionExpression(Type connectionType)
		{
			var p = Expression.Parameter(typeof(string));
			var l = Expression.Lambda<Func<string, DbConnection>>(
				Expression.Convert(Expression.New(
					connectionType.GetConstructor(new[] { typeof(string) })
						?? throw new InvalidOperationException($"DbConnection type {connectionType} missing constructor with connection string parameter: {connectionType.Name}(string connectionString)"),
					p), typeof(DbConnection)),
				p);
			return l;
		}

		#region DataReader ReaderExpressions Helpers

		protected bool SetField(Type fieldType, string dataTypeName, string methodName, bool throwException = true, Type? dataReaderType = null)
		{
			var dataReaderParameter = Expression.Parameter(DataReaderType, "r");
			var indexParameter      = Expression.Parameter(typeof(int), "i");

			MethodCallExpression call;

			if (throwException)
			{
				call = Expression.Call(dataReaderParameter, methodName, null, indexParameter);
			}
			else
			{
				var methodInfo = DataReaderType.GetMethods().FirstOrDefault(m => m.Name == methodName);

				if (methodInfo == null)
					return false;

				call = Expression.Call(dataReaderParameter, methodInfo, indexParameter);
			}

			ReaderExpressions[new ReaderInfo(fieldType: fieldType, dataTypeName: dataTypeName, dataReaderType: dataReaderType)] =
				Expression.Lambda(
					call,
					dataReaderParameter,
					indexParameter);

			return true;
		}

		protected void SetProviderField<TField>(string methodName, Type? dataReaderType = null)
		{
			SetProviderField(typeof(TField), methodName, dataReaderType);
		}

		protected void SetProviderField(Type fieldType, string methodName, Type? dataReaderType = null)
		{
			var dataReaderParameter = Expression.Parameter(DataReaderType, "r");
			var indexParameter      = Expression.Parameter(typeof(int), "i");

			ReaderExpressions[new ReaderInfo(providerFieldType: fieldType, dataReaderType: dataReaderType)] =
				Expression.Lambda(
					Expression.Call(dataReaderParameter, methodName, null, indexParameter),
					dataReaderParameter,
					indexParameter);
		}

		protected void SetToTypeField(Type toType, string methodName, Type? dataReaderType = null)
		{
			var dataReaderParameter = Expression.Parameter(DataReaderType, "r");
			var indexParameter      = Expression.Parameter(typeof(int), "i");

			ReaderExpressions[new ReaderInfo(toType: toType, dataReaderType: dataReaderType)] =
				Expression.Lambda(
					Expression.Call(dataReaderParameter, methodName, null, indexParameter),
					dataReaderParameter,
					indexParameter);
		}

		protected bool SetProviderField<TTo, TField>(string methodName, bool throwException = true, Type? dataReaderType = null)
		{
			return SetProviderField(typeof(TTo), typeof(TField), methodName, throwException, dataReaderType);
		}

		protected bool SetProviderField(Type toType, Type fieldType, string methodName, bool throwException = true, Type? dataReaderType = null)
		{
			var dataReaderParameter = Expression.Parameter(DataReaderType, "r");
			var indexParameter      = Expression.Parameter(typeof(int), "i");

			Expression methodCall;

			if (throwException)
			{
				methodCall = Expression.Call(dataReaderParameter, methodName, null, indexParameter);
			}
			else
			{
				var methodInfo = DataReaderType.GetMethods().FirstOrDefault(m => m.Name == methodName);

				if (methodInfo == null)
					return false;

				methodCall = Expression.Call(dataReaderParameter, methodInfo, indexParameter);
			}

			if (methodCall.Type != toType)
				methodCall = Expression.Convert(methodCall, toType);

			ReaderExpressions[new ReaderInfo(toType: toType, providerFieldType: fieldType, dataReaderType: dataReaderType)] =
				Expression.Lambda(methodCall, dataReaderParameter, indexParameter);

			return true;
		}

		#endregion

		#region Provider Type Converters

		public virtual DbParameter? TryGetProviderParameter(IDataContext dataContext, DbParameter parameter)
		{
			return Adapter.ParameterType.IsSameOrParentOf(parameter.GetType()) ? parameter : null;
		}

		public virtual DbCommand? TryGetProviderCommand(IDataContext dataContext, DbCommand command)
		{
			// remove retry policy wrapper
			if (command is RetryingDbCommand rcmd)
				command = rcmd.UnderlyingObject;

			command = dataContext.UnwrapDataObjectInterceptor?.UnwrapCommand(dataContext, command) ?? command;
			return Adapter.CommandType.IsSameOrParentOf(command.GetType()) ? command : null;
		}

		public virtual DbConnection? TryGetProviderConnection(IDataContext dataContext, DbConnection connection)
		{
			connection = dataContext.UnwrapDataObjectInterceptor?.UnwrapConnection(dataContext, connection) ?? connection;
			return Adapter.ConnectionType.IsSameOrParentOf(connection.GetType()) ? connection : null;
		}

		public virtual DbTransaction? TryGetProviderTransaction(IDataContext dataContext, DbTransaction transaction)
		{
			transaction = dataContext.UnwrapDataObjectInterceptor?.UnwrapTransaction(dataContext, transaction) ?? transaction;
			return Adapter.TransactionType.IsSameOrParentOf(transaction.GetType()) ? transaction : null;
		}

		#endregion
	}
}
