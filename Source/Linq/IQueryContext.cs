using System;
using System.Data;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Linq
{
	using Data;

	public interface IQueryContext : IDisposable
	{
		int                   ExecuteNonQuery   ();
		object                ExecuteScalar     ();
		IDataReader           ExecuteReader     ();
		Task<DataReaderAsync> ExecuteReaderAsync(CancellationToken cancellationToken);
	}
}
