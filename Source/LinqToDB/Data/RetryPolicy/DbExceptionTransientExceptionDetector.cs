// IsTransient property added in .NET 5
// Some providers implement it for other runtimes (e.g. MySqlConnector), but we don't handle them here (could be added on request)
#if NET8_0_OR_GREATER
using System;
using System.Data.Common;

namespace LinqToDB.Data.RetryPolicy
{
	/// <summary>
	/// Detects the exceptions caused by transient failures. Provider must implement <see cref="DbException"/> IsTransient property.
	/// </summary>
	public static class DbExceptionTransientExceptionDetector
	{
		public static bool ShouldRetryOn(Exception ex)
		{
			return ex is DbException dbEx && dbEx.IsTransient;
		}
	}
}
#endif
