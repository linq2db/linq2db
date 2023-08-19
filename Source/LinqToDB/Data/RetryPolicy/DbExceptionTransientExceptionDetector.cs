// IsTransient property added in .NET 5
// Some providers implement it for other runtimes (e.g. MySqlConnector), but we don't handle them here (could be added on request)
#if NET6_0_OR_GREATER
using System;
using System.Data.Common;

namespace LinqToDB.Data.RetryPolicy
{
	/// <summary>
	/// Detects the exceptions caused by transient failures. Provider must implement <see cref="DbException"/> IsTransient property.
	/// </summary>
	// TODO: v6: make static
#pragma warning disable CA1052 // Static holder types should be Static or NotInheritable
	public class DbExceptionTransientExceptionDetector
#pragma warning restore CA1052 // Static holder types should be Static or NotInheritable
	{
		public static bool ShouldRetryOn(Exception ex)
		{
			return ex is DbException dbEx && dbEx.IsTransient;
		}
	}
}
#endif
