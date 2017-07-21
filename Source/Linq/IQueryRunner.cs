using System;
using System.Data;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Linq
{
	interface IQueryRunner1: IDisposable
	{
//		int                   ExecuteNonQuery();
//		object                ExecuteScalar  ();
		IDataReader           ExecuteReader  ();

//#if !SL4
//		Task<IDataReaderAsync> ExecuteReaderAsync(CancellationToken cancellationToken,TaskCreationOptions options);
//#endif

//		Func<int>  SkipAction       { get; set; }
//		Func<int>  TakeAction       { get; set; }
		Expression MapperExpression { get; set; }
		int        RowsCount        { get; set; }
	}

	interface IQueryRunner: IDisposable
	{
		int                   ExecuteNonQuery();
		object                ExecuteScalar  ();
		IDataReader           ExecuteReader  ();

#if !SL4
		Task<IDataReaderAsync> ExecuteReaderAsync(CancellationToken cancellationToken,TaskCreationOptions options);
#endif

		Func<int>  SkipAction       { get; set; }
		Func<int>  TakeAction       { get; set; }
		Expression MapperExpression { get; set; }
		int        RowsCount        { get; set; }
	}
}
