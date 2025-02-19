using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace LinqToDB.Internal.DataProvider.ClickHouse
{
	/// <summary>
	/// Detects the exceptions caused by transient failures.
	/// Currently handles only:
	/// <list type="bullet">
	/// <item>Octonica client ClickHouseException with codes ClickHouseErrorCodes.InvalidConnectionState, ClickHouseErrorCodes.ConnectionClosed, ClickHouseErrorCodes.NetworkError</item>
	/// </list>
	/// </summary>
	public static class ClickHouseTransientExceptionDetector
	{
		private static readonly ConcurrentDictionary<Type, Func<Exception, IEnumerable<int>>> _exceptionTypes = new ();

		internal static void RegisterExceptionType(Type type, Func<Exception, IEnumerable<int>> errrorNumbersGetter)
		{
			_exceptionTypes.TryAdd(type, errrorNumbersGetter);
		}

		public static bool IsHandled(Exception ex, [NotNullWhen(true)] out IEnumerable<int>? errorNumbers)
		{
			if (_exceptionTypes.TryGetValue(ex.GetType(), out var getter))
			{
				errorNumbers = getter(ex);
				return true;
			}

			errorNumbers = null;
			return false;
		}

		public static bool ShouldRetryOn(Exception ex)
		{
			if (IsHandled(ex, out var errors))
			{
				// no idea which other codes indicate transient errors
				// https://github.com/Octonica/ClickHouseClient/blob/master/src/Octonica.ClickHouseClient/Exceptions/ClickHouseErrorCodes.cs
				foreach (var err in errors)
					switch (err)
					{
						// ClickHouseErrorCodes.InvalidConnectionState
						case 2:
						// ClickHouseErrorCodes.ConnectionClosed
						case 3:
						// ClickHouseErrorCodes.NetworkError
						case 16:
							return true;
						default:
							return false;
					}

				return false;
			}

			return ex is TimeoutException;
		}
	}
}
