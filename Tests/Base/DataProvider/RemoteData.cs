using LinqToDB;
using LinqToDB.Linq;
using LinqToDB.Mapping;
using LinqToDB.SqlProvider;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Issues
{
	/* Requirement: implement a data context that handles the implementation of IQueryRunner, so can do things like
	 * - Can delegate to a webservice call like REST
	 * - Implement connection/command handling ourselves so we can cache data
	 * etc.
	 */

	// Implement IDataContext so we can provide our own GetQueryRunner implemetation
	public class RemoteDataContext : IDataContext, IEntityServices
	{
		// Implementation of most of this copied from RemoteDataContextBase

		private LinqToDB.DataProvider.SQLite.SQLiteDataProvider prov = new LinqToDB.DataProvider.SQLite.SQLiteDataProvider();
		private List<string> queryHints;
		private List<string> nextQueryHints;

		public RemoteDataContext()
		{

		}

		public RemoteDataContext(string name)
		{

		}

		string IDataContext.ContextID => prov.Name;

		Func<ISqlBuilder> IDataContext.CreateSqlProvider => () => prov.CreateSqlBuilder(prov.MappingSchema);

		Func<ISqlOptimizer> IDataContext.GetSqlOptimizer => prov.GetSqlOptimizer;

		SqlProviderFlags IDataContext.SqlProviderFlags => prov.SqlProviderFlags;

		Type IDataContext.DataReaderType => typeof(DbDataReader); // !!!

		MappingSchema IDataContext.MappingSchema => prov.MappingSchema;

		bool IDataContext.InlineParameters { get; set; }

		List<string> IDataContext.QueryHints => queryHints ?? (queryHints = new List<string>());

		List<string> IDataContext.NextQueryHints => nextQueryHints ?? (nextQueryHints = new List<string>());

		bool IDataContext.CloseAfterUse { get; set; }

		Action<EntityCreatedEventArgs> IEntityServices.OnEntityCreated { get; set; }

		public event EventHandler OnClosing;

		IDataContext IDataContext.Clone(bool forNestedQuery) => new RemoteDataContext();

		void IDataContext.Close()
		{
			// Nothing to do
		}

		void IDisposable.Dispose()
		{
			//throw new NotImplementedException();
		}

		Expression IDataContext.GetReaderExpression(MappingSchema mappingSchema, IDataReader reader, int idx, Expression readerExpression, Type toType) =>
			prov.GetReaderExpression(mappingSchema, reader, idx, readerExpression, toType);

		bool? IDataContext.IsDBNullAllowed(IDataReader reader, int idx) => prov.IsDBNullAllowed(reader, idx);

		IQueryRunner IDataContext.GetQueryRunner(Query query, int queryNumber, Expression expression, object[] parameters)
		{
			// Return out implementation of IQueryRunner
			return new RemoteQueryRunner(query)
			{
				DataContext = this,
				Expression = expression,
				Parameters = parameters,
				QueryNumber = queryNumber
			};
		}

	}

	// Implement remote query running, unfortuately
	// - 1. The Query passed is opaque, I need to use reflection to get access to the details
	// - 2. Parameters are coming through as null
	// - 3. Copying the actual code to generate the Optimum SQL uses internal APIs

	public class RemoteQueryRunner : IQueryRunner
	{
		// Implementation of this tried to copy QueryRunnerBase

		private readonly Query Query;

		public RemoteQueryRunner(Query query) { this.Query = query; }

		// Part of IQueryRunner, implict so can be set
		public Expression Expression { get; set; }
		public IDataContext DataContext { get; set; }
		public object[] Parameters { get; set; }
		public Expression MapperExpression { get; set; }
		public int RowsCount { get; set; }
		public int QueryNumber { get; set; }

		void IDisposable.Dispose() { }

		int IQueryRunner.ExecuteNonQuery()
		{
			throw new NotImplementedException();
		}

		Task<int> IQueryRunner.ExecuteNonQueryAsync(CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		IDataReader IQueryRunner.ExecuteReader()
		{
			// this is what the remote version does fails to compile because of internal access
			var q = DataContext.GetSqlOptimizer().OptimizeStatement(queryContextByReflection().Statement, DataContext.MappingSchema);
			return null;
		}

		Task<IDataReaderAsync> IQueryRunner.ExecuteReaderAsync(CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		object IQueryRunner.ExecuteScalar()
		{
			throw new NotImplementedException();
		}

		Task<object> IQueryRunner.ExecuteScalarAsync(CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		// Work around because The Query.Queries which is a List<QueryInfo> is internal
		private IQueryContext queryContextByReflection()
		{
			var fields = Query.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic);
			var l = fields.Single(a => a.Name == "Queries").GetValue(Query);
			var casted = l as System.Collections.IList;
			return casted[QueryNumber] as IQueryContext;
		}

		string IQueryRunner.GetSqlText() => GetSql(true);

		protected string GetSql(bool EmbedParams)
		{
			// Example code is this, but it requires internal access
			var query = Query.Queries[QueryNumber];
			/* Workaround using reflection:
			var query = queryContext();
			*/
			var sqlBuilder = DataContext.CreateSqlProvider();
			var sb = new System.Text.StringBuilder();

			if (EmbedParams && query.Statement.Parameters != null && query.Statement.Parameters.Count > 0)
			{
				foreach (var p in query.Statement.Parameters)
				{
					var value = p.Value;
					if (value is string || value is char)
						value = "'" + value.ToString().Replace("'", "''") + "'";
					sb.AppendLine($"-- DECLARE {p.Name} {(value == null ? p.SystemType.ToString() : value.GetType().Name)} = {value} ");
				}
			}

			var cc = sqlBuilder.CommandCount(query.Statement);
			for (var i = 0; i < cc; i++)
			{
				sqlBuilder.BuildSql(i, query.Statement, sb);
				if (i == 0 && query.QueryHints != null && query.QueryHints.Count > 0)
				{
					var sql = sb.ToString();
					sql = sqlBuilder.ApplyQueryHints(sql, query.QueryHints);
					sb = new StringBuilder(sql);
				}
			}
			return sb.ToString();
		}
	}
}
