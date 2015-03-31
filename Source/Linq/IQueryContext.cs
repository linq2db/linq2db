using System;
using System.Data;

namespace LinqToDB.Linq
{
	public interface IQueryContext : IDisposable
	{
		int         ExecuteNonQuery();
		object      ExecuteScalar  ();
		IDataReader ExecuteReader  ();
	}
}
