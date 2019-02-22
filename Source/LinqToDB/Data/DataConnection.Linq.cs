using System;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Data
{
	using DataProvider;
	using Linq;
	using Mapping;
	using SqlQuery;
	using SqlProvider;

	public partial class DataConnection : IDataContext
	{
		/// <summary>
		/// Returns queryable source for specified mapping class for current connection, mapped to database table or view.
		/// </summary>
		/// <typeparam name="T">Mapping class type.</typeparam>
		/// <returns>Queryable source.</returns>
		public ITable<T> GetTable<T>()
			where T : class
		{
			return new Table<T>(this);
		}

		/// <summary>
		/// Returns queryable source for specified mapping class for current connection, mapped to table expression or function.
		/// It could be used e.g. for queries to table-valued functions or to decorate queried table with hints.
		/// </summary>
		/// <typeparam name="T">Mapping class type.</typeparam>
		/// <param name="instance">Instance object for <paramref name="methodInfo"/> method or null for static method.</param>
		/// <param name="methodInfo">Method, decorated with expression attribute, based on <see cref="LinqToDB.Sql.TableFunctionAttribute"/>.</param>
		/// <param name="parameters">Parameters for <paramref name="methodInfo"/> method.</param>
		/// <returns>Queryable source.</returns>
		public ITable<T> GetTable<T>(object instance, MethodInfo methodInfo, params object[] parameters)
			where T : class
		{
			return DataExtensions.GetTable<T>(this, instance, methodInfo, parameters);
		}

		protected virtual SqlStatement ProcessQuery(SqlStatement statement)
		{
			return statement;
		}

		#region IDataContext Members

		SqlProviderFlags IDataContext.SqlProviderFlags => DataProvider.SqlProviderFlags;
		Type             IDataContext.DataReaderType   => DataProvider.DataReaderType;

		bool             IDataContext.CloseAfterUse    { get; set; }

		Expression IDataContext.GetReaderExpression(MappingSchema mappingSchema, IDataReader reader, int idx, Expression readerExpression, Type toType)
		{
			return DataProvider.GetReaderExpression(mappingSchema, reader, idx, readerExpression, toType);
		}

		bool? IDataContext.IsDBNullAllowed(IDataReader reader, int idx)
		{
			return DataProvider.IsDBNullAllowed(reader, idx);
		}

		IDataContext IDataContext.Clone(bool forNestedQuery)
		{
			CheckAndThrowOnDisposed();

			if (forNestedQuery && _connection != null && IsMarsEnabled)
				return new DataConnection(DataProvider, _connection)
				{
					MappingSchema    = MappingSchema,
					Transaction      = Transaction,
					IsMarsEnabled    = IsMarsEnabled,
					ConnectionString = ConnectionString,
				};

			return (DataConnection)Clone();
		}

		string IDataContext.ContextID => DataProvider.Name;

		static Func<ISqlBuilder> GetCreateSqlProvider(IDataProvider dp)
		{
			return dp.CreateSqlBuilder;
		}

		Func<ISqlBuilder> IDataContext.CreateSqlProvider => GetCreateSqlProvider(DataProvider);

		static Func<ISqlOptimizer> GetGetSqlOptimizer(IDataProvider dp)
		{
			return dp.GetSqlOptimizer;
		}

		Func<ISqlOptimizer> IDataContext.GetSqlOptimizer => GetGetSqlOptimizer(DataProvider);

		#endregion
	}
}
