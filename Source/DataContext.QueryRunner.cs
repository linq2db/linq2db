using System;
using System.Data;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB
{
	using Data;
	using Linq;

	public partial class DataContext
	{
		IQueryRunner IDataContextEx.GetQueryRunner(Query query, int queryNumber, Expression expression, object[] parameters)
		{
			return new QueryRunner(this, ((IDataContextEx)GetDataConnection()).GetQueryRunner(query, queryNumber, expression, parameters));
		}

		// IT : QueryRunner - DataContext
		//
		class QueryRunner : IQueryRunner
		{
			public QueryRunner(DataContext dataContext, IQueryRunner queryRunner)
			{
				_dataContext = dataContext;
				_queryRunner = (DataConnection.QueryRunner)queryRunner;
			}

			readonly DataContext                _dataContext;
			readonly DataConnection.QueryRunner _queryRunner;

			public void Dispose()
			{
				_dataContext.ReleaseQuery();
				_queryRunner.Dispose();
			}

			public int ExecuteNonQuery()
			{
				return _queryRunner.ExecuteNonQuery();
			}

			public object ExecuteScalar()
			{
				return _queryRunner.ExecuteScalar();
			}

			public IDataReader ExecuteReader()
			{
				return _queryRunner.ExecuteReader();
			}

#if !NOASYNC

			public Task<object> ExecuteScalarAsync(CancellationToken cancellationToken, TaskCreationOptions options)
			{
				return _queryRunner.ExecuteScalarAsync(cancellationToken, options);
			}

			public Task<IDataReaderAsync> ExecuteReaderAsync(CancellationToken cancellationToken, TaskCreationOptions options)
			{
				return _queryRunner.ExecuteReaderAsync(cancellationToken, options);
			}

			public Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken, TaskCreationOptions options)
			{
				return _queryRunner.ExecuteNonQueryAsync(cancellationToken, options);
			}

#endif

			public string GetSqlText()
			{
				return _queryRunner.GetSqlText();
			}

			public    QueryContext   QueryContext { get { return _queryRunner.QueryContext; } set { _queryRunner.QueryContext = value; } }
			public    IDataContextEx DataContext  { get { return _queryRunner.DataContext;  } set { _queryRunner.DataContext  = value; } }
			public    Expression     Expression   { get { return _queryRunner.Expression;   } set { _queryRunner.Expression   = value; } }
			public    object[]       Parameters   { get { return _queryRunner.Parameters;   } set { _queryRunner.Parameters   = value; } }

			public Func<int> SkipAction { get { return _queryRunner.SkipAction; } set { _queryRunner.SkipAction = value; } }
			public Func<int> TakeAction { get { return _queryRunner.TakeAction; } set { _queryRunner.TakeAction = value; } }

			public Expression MapperExpression
			{
				get { return _queryRunner.MapperExpression;  }
				set { _queryRunner.MapperExpression = value; }
			}

			public int RowsCount
			{
				get { return _queryRunner.RowsCount;  }
				set { _queryRunner.RowsCount = value; }
			}

			public int QueryNumber
			{
				get { return _queryRunner.QueryNumber;  }
				set { _queryRunner.QueryNumber = value; }
			}
		}
	}
}
