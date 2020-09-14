﻿using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Linq
{
	public interface IDataReaderAsync : IDisposable
#if !NETFRAMEWORK
		, IAsyncDisposable
#endif
	{
		IDataReader DataReader { get; }
		Task<bool>  ReadAsync(CancellationToken cancellationToken);
	}
}
