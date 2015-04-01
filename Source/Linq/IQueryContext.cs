using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Linq
{
	using Data;

	public interface IQueryContext : IDisposable
	{
		int         ExecuteNonQuery();
		object      ExecuteScalar  ();

		IDataReader           ExecuteReader     (string commandText, DataParameter[] parameters);
		Task<DataReaderAsync> ExecuteReaderAsync(string commandText, DataParameter[] parameters, CancellationToken cancellationToken);
	}
}
