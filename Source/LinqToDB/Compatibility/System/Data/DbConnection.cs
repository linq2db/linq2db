using System;
using System.Threading;
using System.Threading.Tasks;

namespace System.Data
{
	static partial class DbDataExtensions
	{
		public static async Task OpenAsync(this IDbConnection connection, CancellationToken cancellationToken)
		{
			await Task.Factory.StartNew(connection.Open, cancellationToken);
		}
	}
}

