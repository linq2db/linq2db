using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using LinqToDB.Interceptors;

namespace LinqToDB.Data
{
	using System.Data.Common;
	using DataProvider;
	using Linq;
	using SqlProvider;
	using SqlQuery;

	public partial class DataConnection
	{
		protected virtual SqlStatement ProcessQuery(SqlStatement statement, EvaluationContext context)
		{
			return statement;
		}

		#region IDataContext Members

		SqlProviderFlags IDataContext.SqlProviderFlags      => DataProvider.SqlProviderFlags;
		TableOptions     IDataContext.SupportedTableOptions => DataProvider.SupportedTableOptions;
		Type             IDataContext.DataReaderType        => DataProvider.DataReaderType;

		bool             IDataContext.CloseAfterUse    { get; set; }

		Expression IDataContext.GetReaderExpression(DbDataReader reader, int idx, Expression readerExpression, Type toType)
		{
			return DataProvider.GetReaderExpression(reader, idx, readerExpression, toType);
		}

		bool? IDataContext.IsDBNullAllowed(DbDataReader reader, int idx)
		{
			return DataProvider.IsDBNullAllowed(reader, idx);
		}

		IDataContext IDataContext.Clone(bool forNestedQuery)
		{
			CheckAndThrowOnDisposed();

			if (forNestedQuery && _connection != null && IsMarsEnabled)
				return new DataConnection(DataProvider, _connection.Connection)
				{
					MappingSchema             = MappingSchema,
					TransactionAsync          = TransactionAsync,
					IsMarsEnabled             = IsMarsEnabled,
					ConnectionString          = ConnectionString,
					RetryPolicy               = RetryPolicy,
					CommandTimeout            = CommandTimeout,
					InlineParameters          = InlineParameters,
					ThrowOnDisposed           = ThrowOnDisposed,
					_queryHints               = _queryHints?.Count > 0 ? _queryHints.ToList() : null,
					OnTraceConnection         = OnTraceConnection,
					_commandInterceptor       = _commandInterceptor      .CloneAggregated(),
					_connectionInterceptor    = _connectionInterceptor   .CloneAggregated(),
					_dataContextInterceptor   = _dataContextInterceptor  .CloneAggregated(),
					_entityServiceInterceptor = _entityServiceInterceptor.CloneAggregated(),
				};

			return (DataConnection)Clone();
		}

		string IDataContext.ContextID => DataProvider.Name;

		Func<ISqlBuilder> IDataContext.CreateSqlProvider => () => DataProvider.CreateSqlBuilder(MappingSchema);

		static Func<ISqlOptimizer> GetGetSqlOptimizer(IDataProvider dp)
		{
			return dp.GetSqlOptimizer;
		}

		Func<ISqlOptimizer> IDataContext.GetSqlOptimizer => GetGetSqlOptimizer(DataProvider);

		#endregion
	}
}
