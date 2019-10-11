using LinqToDB;
using LinqToDB.Extensions;
using LinqToDB.Linq;
using LinqToDB.Mapping;
using LinqToDB.SqlProvider;
using System;
using System.Collections;
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
	 * - Delegate to a webservice call like REST
	 * - Implement connection/command handling ourselves so we can cache data
	 * etc.
	 */

	// Implement IDataContext so we can provide our own GetQueryRunner implemetation
	public class ApiTestDataContext : IDataContext, IEntityServices
	{
		// Implementation of most of this copied from RemoteDataContextBase
		private LinqToDB.DataProvider.SQLite.SQLiteDataProvider prov = new LinqToDB.DataProvider.SQLite.SQLiteDataProvider();
		private List<string> queryHints;
		private List<string> nextQueryHints;

		public ApiTestDataContext() { }

		public ApiTestDataContext(string name) {  }

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

		IDataContext IDataContext.Clone(bool forNestedQuery) => new ApiTestDataContext();

		void IDataContext.Close() { }

		void IDisposable.Dispose() { }

		Expression IDataContext.GetReaderExpression(MappingSchema mappingSchema, IDataReader reader, int idx, Expression readerExpression, Type toType) =>
			prov.GetReaderExpression(mappingSchema, reader, idx, readerExpression, toType);

		bool? IDataContext.IsDBNullAllowed(IDataReader reader, int idx) => prov.IsDBNullAllowed(reader, idx);

		IQueryRunner IDataContext.GetQueryRunner(Query query, int queryNumber, Expression expression, object[] parameters)
		{
			// Return out implementation of IQueryRunner
			return new ApiTestQueryRunner(query)
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

	public class ApiTestQueryRunner : IQueryRunner
	{
		// Implementation of this tried to copy QueryRunnerBase

		private readonly Query Query;
		protected List<string> QueryHints = new List<string>();

		public ApiTestQueryRunner(Query query) { this.Query = query; }

		// Part of IQueryRunner, implict so can be set
		public Expression Expression { get; set; }
		public IDataContext DataContext { get; set; }
		public object[] Parameters { get; set; }
		public Expression MapperExpression { get; set; }
		public int RowsCount { get; set; }
		public int QueryNumber { get; set; }

		void IDisposable.Dispose() { }

		// Copied from QueryRunnerBase
		protected virtual void SetCommand(bool clearQueryHints)
		{
			// TODO: can we refactory query to be thread-safe to remove this lock?
			lock (Query)
			{
				if (QueryNumber == 0 && (DataContext.QueryHints.Count > 0 || DataContext.NextQueryHints.Count > 0))
				{
					var queryContext = Query.Queries[QueryNumber];

					queryContext.QueryHints = new List<string>(DataContext.QueryHints);
					queryContext.QueryHints.AddRange(DataContext.NextQueryHints);

					QueryHints.AddRange(DataContext.QueryHints);
					QueryHints.AddRange(DataContext.NextQueryHints);

					if (clearQueryHints)
						DataContext.NextQueryHints.Clear();
				}

				QueryRunner.SetParameters(Query, DataContext, Expression, Parameters, QueryNumber);
				SetQuery();
			}
		}

		// coped from QueryRunner
		protected void SetQuery() { }

		// Copied from QueryRunner
		internal static void SetParameters(Query query, IDataContext dataContext, Expression expression, object[] parameters, int queryNumber)
		{
			var queryContext = query.Queries[queryNumber];

			foreach (var p in queryContext.Parameters)
			{
				var value = p.Accessor(expression, parameters);

				if (value is IEnumerable vs)
				{
					var type = vs.GetType();
					var etype = type.GetItemType();

					if (etype == null || etype == typeof(object) || etype.IsEnumEx() ||
						type.IsGenericTypeEx() && type.GetGenericTypeDefinition() == typeof(Nullable<>) &&
						etype.GetGenericArgumentsEx()[0].IsEnumEx())
					{
						var values = new List<object>();

						foreach (var v in vs)
						{
							value = v;

							if (v != null)
							{
								var valueType = v.GetType();

								if (valueType.ToNullableUnderlying().IsEnumEx())
									value = query.GetConvertedEnum(valueType, value);
							}

							values.Add(value);
						}

						value = values;
					}
				}

				p.SqlParameter.Value = value;

				//				if (value != null && dataContext.InlineParameters && p.SqlParameter.IsQueryParameter)
				//				{
				//					var type = value.GetType();
				//
				//					if (type != typeof(byte[]) && dataContext.MappingSchema.IsScalarType(type))
				//						p.SqlParameter.IsQueryParameter = false;
				//				}

				var dataType = p.DataTypeAccessor(expression, parameters);

				if (dataType != DataType.Undefined)
					p.SqlParameter.DataType = dataType;

				var dbType = p.DbTypeAccessor(expression, parameters);

				if (!string.IsNullOrEmpty(dbType))
					p.SqlParameter.DbType = dbType;

				var size = p.SizeAccessor(expression, parameters);

				if (size != null)
					p.SqlParameter.DbSize = size;

			}
		}

		// Copied from QueryRunner
		int IQueryRunner.ExecuteNonQuery()
		{
			SetCommand(true);
			var queryContext = Query.Queries[QueryNumber];
			var q = DataContext.GetSqlOptimizer().OptimizeStatement(queryContext.Statement, DataContext.MappingSchema);
			// Get SQL text
			var sql = "";
			// Get parameters
			var parameters = q.IsParameterDependent ? q.Parameters.ToArray() : queryContext.GetParameters();

			// Run our command and return result - for the moment just log and return 0 records changed
			Console.WriteLine($"Running SQL: {sql}");
			foreach (var p in parameters)
				Console.WriteLine($"{p.Name} = {p.Value}");
			return 0;
		}

		// Work around because The Query.Queries which is a List<QueryInfo> is internal, so use refelection
		private IQueryContext queryContextByReflection()
		{
			var fields = Query.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic);
			var l = fields.Single(a => a.Name == "Queries").GetValue(Query);
			var casted = l as System.Collections.IList;
			return casted[QueryNumber] as IQueryContext;
		}

		string IQueryRunner.GetSqlText() => GetSql(true);

		// Copied from QueryRunner
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

		#region Unimplemented
		Task<int> IQueryRunner.ExecuteNonQueryAsync(CancellationToken cancellationToken) => throw new NotImplementedException();

		IDataReader IQueryRunner.ExecuteReader() => throw new NotImplementedException();

		Task<IDataReaderAsync> IQueryRunner.ExecuteReaderAsync(CancellationToken cancellationToken) => throw new NotImplementedException();

		object IQueryRunner.ExecuteScalar() => throw new NotImplementedException();

		Task<object> IQueryRunner.ExecuteScalarAsync(CancellationToken cancellationToken) => throw new NotImplementedException();

		#endregion
	}
}
