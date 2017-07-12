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

			public Task<IDataReaderAsync> ExecuteReaderAsync(CancellationToken cancellationToken, TaskCreationOptions options)
			{
				return _queryRunner.ExecuteReaderAsync(cancellationToken, options);
			}

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
		}
	}
}
