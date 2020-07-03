﻿using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Linq
{
	public interface IDataReaderAsync : IDisposable
#if !NET45 && !NET46
		, IAsyncDisposable
#endif
	{
		IDataReader DataReader { get; }
		Task<bool>  ReadAsync(CancellationToken cancellationToken);
	}
}
