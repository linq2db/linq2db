using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.DataProvider
{
	using Expressions;
	using Extensions;
	using LinqToDB.Data.RetryPolicy;
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

		Func<string, IDbConnection>? _createConnection;

		protected override IDbConnection CreateConnectionInternal(string connectionString)
		{
			if (_createConnection == null)
			{
				var l = CreateConnectionExpression(Adapter.ConnectionType);
				_createConnection = l.Compile();
			}

			return _createConnection(connectionString);
		}

		private static Expression<Func<string, IDbConnection>> CreateConnectionExpression(Type connectionType)
		{
			var p = Expression.Parameter(typeof(string));
			var l = Expression.Lambda<Func<string, IDbConnection>>(
				Expression.Convert(Expression.New(connectionType.GetConstructor(new[] { typeof(string) }), p), typeof(IDbConnection)),
				p);
			return l;
		}

		#region DataReader ReaderExpressions Helpers

		protected bool SetField(Type fieldType, string dataTypeName, string methodName, bool throwException = true, Type? dataReaderType = null)
		{
			var dataReaderParameter = Expression.Parameter(DataReaderType, "r");
			var indexParameter = Expression.Parameter(typeof(int), "i");

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
			var indexParameter = Expression.Parameter(typeof(int), "i");

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

			ReaderExpressions[new ReaderInfo { ToType = toType, ProviderFieldType = fieldType, DataReaderType = dataReaderType }] =
				Expression.Lambda(methodCall, dataReaderParameter, indexParameter);

			return true;
		}

		#endregion

		#region Provider Type Converters

		// That's fine, as TryGetValue and indexer are lock-free operations for ConcurrentDictionary.
		// In general I don't expect more than one wrapper used (e.g. miniprofiler), still it's not a big deal
		// to support multiple wrappers
		//
		// Actually it should be fine to remove support for IDbDataParameter wrappers, as it's probably something
		// nobody will do
		private readonly IDictionary<Type, Func<IDbDataParameter, IDbDataParameter>?> _parameterConverters   = new ConcurrentDictionary<Type, Func<IDbDataParameter, IDbDataParameter>?>();
		private readonly IDictionary<Type, Func<IDbCommand      , IDbCommand      >?> _commandConverters     = new ConcurrentDictionary<Type, Func<IDbCommand      , IDbCommand      >?>();
		private readonly IDictionary<Type, Func<IDbConnection   , IDbConnection   >?> _connectionConverters  = new ConcurrentDictionary<Type, Func<IDbConnection   , IDbConnection   >?>();
		private readonly IDictionary<Type, Func<IDbTransaction  , IDbTransaction  >?> _transactionConverters = new ConcurrentDictionary<Type, Func<IDbTransaction  , IDbTransaction  >?>();

		public virtual IDbDataParameter? TryGetProviderParameter(IDbDataParameter parameter, MappingSchema ms)
		{
			return TryConvertProviderType(_parameterConverters, Adapter.ParameterType, parameter, ms);
		}

		public virtual IDbCommand? TryGetProviderCommand(IDbCommand command, MappingSchema ms)
		{
			// remove retry policy wrapper
			if (command is RetryingDbCommand rcmd)
				command = rcmd.UnderlyingObject;

			return TryConvertProviderType(_commandConverters, Adapter.CommandType, command, ms);
		}

		public virtual IDbConnection? TryGetProviderConnection(IDbConnection connection, MappingSchema ms)
		{
			// remove retry policy wrapper
			if (connection is RetryingDbConnection rcn)
				connection = rcn.UnderlyingObject;

			return TryConvertProviderType(_connectionConverters, Adapter.ConnectionType, connection, ms);
		}

		public virtual IDbTransaction? TryGetProviderTransaction(IDbTransaction transaction, MappingSchema ms)
		{
			return TryConvertProviderType(_transactionConverters, Adapter.TransactionType, transaction, ms);
		}

		private static TResult? TryConvertProviderType<TResult>(
			IDictionary<Type, Func<TResult, TResult>?> converters,
			Type expectedType,
			TResult value,
			MappingSchema ms)
			where TResult : class
		{
			var valueType = value.GetType();

			if (expectedType.IsSameOrParentOf(valueType))
				return value;

			if (!converters.TryGetValue(valueType, out var converter))
			{
				// don't think it makes sense to lock creation of new converter
				var converterExpr = ms.GetConvertExpression(valueType, typeof(TResult), false, false);
				
				if (converterExpr != null)
				{
					var param = Expression.Parameter(typeof(TResult));
					converter = (Func<TResult, TResult>)Expression
						.Lambda(
							converterExpr.GetBody(Expression.Convert(param, valueType)),
							param)
						.Compile();

					converters[valueType] = converter;
				}
			}

			if (converter != null)
				return converter(value);

			return null;
		}

		#endregion
	}
}
