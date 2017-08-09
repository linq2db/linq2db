﻿using System;
using System.Data;
using System.Linq.Expressions;
using System.Threading;

#if !SL4
using System.Threading.Tasks;
#endif

namespace LinqToDB.Linq
{
	interface IQueryRunner: IDisposable
	{
		int                   ExecuteNonQuery();
		object                ExecuteScalar  ();
		IDataReader           ExecuteReader  ();

#if !NOASYNC
		Task<int>              ExecuteNonQueryAsync(CancellationToken cancellationToken);
		Task<object>           ExecuteScalarAsync  (CancellationToken cancellationToken);
		Task<IDataReaderAsync> ExecuteReaderAsync  (CancellationToken cancellationToken);
#endif

		string                GetSqlText     ();

		Func<int>      SkipAction       { get; set; }
		Func<int>      TakeAction       { get; set; }
		Expression     Expression       { get; set; }
		IDataContextEx DataContext      { get; set; }
		object[]       Parameters       { get; set; }
		Expression     MapperExpression { get; set; }
		int            RowsCount        { get; set; }
		int            QueryNumber      { get; set; }
	}
}
