using System;
using System.Data;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Linq
{
	interface IQueryRunner: IDisposable
	{
		int                   ExecuteNonQuery();
		object                ExecuteScalar  ();
		IDataReader           ExecuteReader  ();

#if !NOASYNC
		Task<IDataReaderAsync> ExecuteReaderAsync(CancellationToken cancellationToken,TaskCreationOptions options);
#endif

		Func<int>      SkipAction       { get; set; }
		Func<int>      TakeAction       { get; set; }
		QueryContext   QueryContext     { get; set; }
		Expression     Expression       { get; set; }
		IDataContextEx DataContext      { get; set; }
		object[]       Parameters       { get; set; }
		Expression     MapperExpression { get; set; }
		int            RowsCount        { get; set; }
	}
}
