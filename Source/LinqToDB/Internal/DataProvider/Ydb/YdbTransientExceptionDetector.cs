using System;
using System.Globalization;
using System.Reflection;

namespace LinqToDB.Internal.DataProvider.Ydb
{
	internal static class YdbTransientExceptionDetector
	{
		public static bool ShouldRetryOn(Exception ex, bool idempotent)
		{
			if (ex is AggregateException ae && ae.InnerException != null)
				ex = ae.InnerException;

			// Ydb.Sdk.Ado.YdbException ?
			if (ex.GetType().FullName == "Ydb.Sdk.Ado.YdbException")
			{
				// 1) Prefer fag from SDK:
				//    IsTransientWhenIdempotent / IsTransient
				var propName = idempotent ? "IsTransientWhenIdempotent" : "IsTransient";
				var prop     = ex.GetType().GetProperty(propName, BindingFlags.Instance | BindingFlags.Public);
				if (prop?.PropertyType == typeof(bool))
				{
					var val = (bool)(prop.GetValue(ex) ?? false);
					if (val) return true;
				}

				// 2) Most popular gRPC-code from InnerException
				//    (Unavailable, DeadlineExceeded, ResourceExhausted, Aborted)
				var inner = ex.InnerException;
				if (inner != null && inner.GetType().FullName == "Grpc.Core.RpcException")
				{
					// RpcException.Status.StatusCode (enum)
					var status  = inner.GetType().GetProperty("Status")?.GetValue(inner);
					var codeObj = status?.GetType().GetProperty("StatusCode")?.GetValue(status);
					var code    = Convert.ToString(codeObj, CultureInfo.InvariantCulture);

					if (code is "Unavailable" or "DeadlineExceeded" or "ResourceExhausted" or "Aborted")
						return true;
				}

				// 3) Special case from YDB: disabling processing lock
				if (ex.Message?.IndexOf("Transaction Lock Invalidated", StringComparison.OrdinalIgnoreCase) >= 0)
					return true;
			}

			return false;
		}
	}
}
