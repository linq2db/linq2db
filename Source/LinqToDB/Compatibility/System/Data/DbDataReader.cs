using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace System.Data
{
	static partial class DbDataExtensions
	{
		public static async Task<bool> ReadAsync(this DbDataReader reader, CancellationToken cancellationToken)
		{
			return await Task.Factory.StartNew(reader.Read, cancellationToken);
		}

		public static async Task<bool> NextResultAsync(this DbDataReader reader, CancellationToken cancellationToken)
		{
			return await Task.Factory.StartNew(reader.Read, cancellationToken);
		}
	}
}

