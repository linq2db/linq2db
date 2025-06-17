using System;
using System.Data.Common;
using System.Linq.Expressions;

using LinqToDB.DataProvider;
using LinqToDB.SqlProvider;
using LinqToDB.SqlQuery;

namespace LinqToDB.Data
{
	public partial class DataConnection
	{
		protected virtual SqlStatement ProcessQuery(SqlStatement statement, EvaluationContext context)
		{
			CheckAndThrowOnDisposed();

			return statement;
		}

		#region IDataContext Members

		SqlProviderFlags IDataContext.SqlProviderFlags      => DataProvider.SqlProviderFlags;
		TableOptions     IDataContext.SupportedTableOptions => DataProvider.SupportedTableOptions;
		Type             IDataContext.DataReaderType        => DataProvider.DataReaderType;

		bool             IDataContext.CloseAfterUse    { get; set; }

		Expression IDataContext.GetReaderExpression(DbDataReader reader, int idx, Expression readerExpression, Type toType)
		{
			CheckAndThrowOnDisposed();

			return DataProvider.GetReaderExpression(reader, idx, readerExpression, toType);
		}

		bool? IDataContext.IsDBNullAllowed(DbDataReader reader, int idx)
		{
			return DataProvider.IsDBNullAllowed(Options, reader, idx);
		}

		string IDataContext.ContextName => DataProvider.Name;

		Func<ISqlBuilder> IDataContext.CreateSqlBuilder => () => DataProvider.CreateSqlBuilder(MappingSchema, Options);

		Func<DataOptions,ISqlOptimizer> IDataContext.GetSqlOptimizer => DataProvider.GetSqlOptimizer;

		#endregion
	}
}
