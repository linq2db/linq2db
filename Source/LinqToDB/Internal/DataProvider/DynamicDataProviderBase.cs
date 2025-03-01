using System;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Data.RetryPolicy;
using LinqToDB.DataProvider;
using LinqToDB.Extensions;
using LinqToDB.Interceptors;
using LinqToDB.Internal.Extensions;
using LinqToDB.Mapping;
using LinqToDB.Tools;

namespace LinqToDB.Internal.DataProvider
{
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

		public override string? ConnectionNamespace   => Adapter.ConnectionType.Namespace;
		public override Type    DataReaderType        => Adapter.DataReaderType;
		public override bool    TransactionsSupported => Adapter.TransactionType != null;

		protected override DbConnection CreateConnectionInternal(string connectionString) => Adapter.CreateConnection(connectionString);

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

			ReaderExpressions[new ReaderInfo { FieldType = fieldType, DataTypeName = dataTypeName, DataReaderType = dataReaderType }] =
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

			ReaderExpressions[new ReaderInfo { ProviderFieldType = fieldType, DataReaderType = dataReaderType }] =
				Expression.Lambda(
					Expression.Call(dataReaderParameter, methodName, null, indexParameter),
					dataReaderParameter,
					indexParameter);
		}

		protected void SetToTypeField(Type toType, string methodName, Type? dataReaderType = null)
		{
			var dataReaderParameter = Expression.Parameter(DataReaderType, "r");
			var indexParameter      = Expression.Parameter(typeof(int), "i");

			ReaderExpressions[new ReaderInfo { ToType = toType, DataReaderType = dataReaderType }] =
				Expression.Lambda(
					Expression.Call(dataReaderParameter, methodName, null, indexParameter),
					dataReaderParameter,
					indexParameter);
		}

		protected bool SetProviderField<TTo, TField>(string methodName, bool throwException = true, Type? dataReaderType = null)
		{
			return SetProviderField(typeof(TTo), typeof(TField), methodName, throwException, dataReaderType);
		}

		protected bool SetProviderField(Type toType, Type fieldType, string methodName, bool throwException = true, Type? dataReaderType = null, string? typeName = null)
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

			ReaderExpressions[new ReaderInfo { ToType = toType, ProviderFieldType = fieldType, DataReaderType = dataReaderType, DataTypeName = typeName }] =
				Expression.Lambda(methodCall, dataReaderParameter, indexParameter);

			return true;
		}

		protected bool SetGetFieldValueReader(Type toType, Type fieldType, Type? dataReaderType = null, string? typeName = null)
		{
			var dataReaderParameter = Expression.Parameter(DataReaderType, "r");
			var indexParameter      = Expression.Parameter(typeof(int), "i");

			var methodCall = Expression.Call(dataReaderParameter, nameof(DbDataReader.GetFieldValue), new[] { toType }, indexParameter);

			ReaderExpressions[new ReaderInfo { ToType = toType, ProviderFieldType = fieldType, DataReaderType = dataReaderType, DataTypeName = typeName }] =
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

			if (dataContext is IInterceptable<IUnwrapDataObjectInterceptor> { Interceptor: { } interceptor })
				using (ActivityService.Start(ActivityID.UnwrapDataObjectInterceptorUnwrapCommand))
					command = interceptor.UnwrapCommand(dataContext, command);

			return Adapter.CommandType.IsSameOrParentOf(command.GetType()) ? command : null;
		}

		public virtual DbConnection? TryGetProviderConnection(IDataContext dataContext, DbConnection connection)
		{
			if (dataContext is IInterceptable<IUnwrapDataObjectInterceptor> { Interceptor: { } interceptor })
				using (ActivityService.Start(ActivityID.UnwrapDataObjectInterceptorUnwrapConnection))
					connection = interceptor.UnwrapConnection(dataContext, connection);

			return Adapter.ConnectionType.IsSameOrParentOf(connection.GetType()) ? connection : null;
		}

		public virtual DbTransaction? TryGetProviderTransaction(IDataContext dataContext, DbTransaction transaction)
		{
			if (Adapter.TransactionType == null)
				return null;

			if (dataContext is IInterceptable<IUnwrapDataObjectInterceptor> { Interceptor: { } interceptor })
				using (ActivityService.Start(ActivityID.UnwrapDataObjectInterceptorUnwrapTransaction))
					transaction = interceptor.UnwrapTransaction(dataContext, transaction);

			return Adapter.TransactionType.IsSameOrParentOf(transaction.GetType()) ? transaction : null;
		}

		#endregion
	}
}
