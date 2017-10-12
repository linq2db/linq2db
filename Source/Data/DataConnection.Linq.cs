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

		protected virtual SelectQuery ProcessQuery(SelectQuery selectQuery)
		{
			return selectQuery;
		}

		#region IDataContext Members

		SqlProviderFlags IDataContext.SqlProviderFlags { get { return DataProvider.SqlProviderFlags; } }
		Type             IDataContext.DataReaderType   { get { return DataProvider.DataReaderType;   } }

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
			ThrowOnDisposed();

			if (forNestedQuery && _connection != null && IsMarsEnabled)
				return new DataConnection(DataProvider, _connection) { _mappingSchema = _mappingSchema, Transaction = Transaction };

			return (DataConnection)Clone();
		}

		string IDataContext.ContextID
		{
			get { return DataProvider.Name; }
		}

		static Func<ISqlBuilder> GetCreateSqlProvider(IDataProvider dp)
		{
			return dp.CreateSqlBuilder;
		}

		Func<ISqlBuilder> IDataContext.CreateSqlProvider
		{
			get { return GetCreateSqlProvider(DataProvider); }
		}

		static Func<ISqlOptimizer> GetGetSqlOptimizer(IDataProvider dp)
		{
			return dp.GetSqlOptimizer;
		}

		Func<ISqlOptimizer> IDataContext.GetSqlOptimizer
		{
			get { return GetGetSqlOptimizer(DataProvider); }
		}

		#endregion
	}
}
