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
			return DataProvider.IsDBNullAllowed(Options, reader, idx);
		}

		string IDataContext.ContextName => DataProvider.Name;

		Func<ISqlBuilder> IDataContext.CreateSqlProvider => () => DataProvider.CreateSqlBuilder(MappingSchema, Options);

		static Func<DataOptions,ISqlOptimizer> GetGetSqlOptimizer(IDataProvider dp)
		{
			return dp.GetSqlOptimizer;
		}

		Func<DataOptions,ISqlOptimizer> IDataContext.GetSqlOptimizer => GetGetSqlOptimizer(DataProvider);

		#endregion
	}
}
