using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;

namespace LinqToDB.Internal.DataProvider.Ydb
{
	/// <summary>
	/// Detector for identifying whether an exception represents a transient YDB error,
	/// implemented without a hard dependency on <c>Ydb.Sdk</c>.
	/// <para>
	/// Works by inspecting two properties of <c>YdbException</c>:
	/// <list type="bullet">
	/// <item><description><c>bool IsTransient</c> — indicates if the error is transient (temporary).</description></item>
	/// <item><description><c>enum Code</c> — contains the YDB status code for the error.</description></item>
	/// </list>
	/// </para>
	/// </summary>
	public static class YdbTransientExceptionDetector
	{
		private const string YdbExceptionFullName = "Ydb.Sdk.Ado.YdbException";

		/// <summary>
		/// Checks whether there is a YDB exception either at the top level
		/// or nested inside the provided exception hierarchy.
		/// </summary>
		/// <param name="ex">The exception to search.</param>
		/// <param name="ydbEx">
		/// When this method returns <see langword="true"/>, contains the first discovered YDB exception.
		/// Otherwise, <see langword="null"/>.
		/// </param>
		/// <returns>
		/// <see langword="true"/> if a <c>YdbException</c> was found, otherwise <see langword="false"/>.
		/// </returns>
		public static bool TryGetYdbException(Exception ex, [NotNullWhen(true)] out Exception? ydbEx)
		{
			// YdbException is typically at the top level in the ADO client,
			// but we traverse the inner exceptions just in case.
			for (var e = ex; e != null; e = e.InnerException!)
			{
				if (string.Equals(e.GetType().FullName, YdbExceptionFullName, StringComparison.Ordinal))
				{
					ydbEx = e;
					return true;
				}
			}

			ydbEx = null;
			return false;
		}

		/// <summary>
		/// Reads the YDB <c>Code</c> property (status enum) as a string and also retrieves the <c>IsTransient</c> flag.
		/// </summary>
		/// <param name="ydbEx">The YDB exception instance to inspect.</param>
		/// <param name="codeName">Outputs the name of the status code as a string.</param>
		/// <param name="isTransient">Outputs whether the error is transient.</param>
		/// <returns>
		/// <see langword="true"/> if the <c>Code</c> property was successfully read, otherwise <see langword="false"/>.
		/// </returns>
		public static bool TryGetCodeAndTransient(Exception ydbEx, out string? codeName, out bool isTransient)
		{
			var t = ydbEx.GetType();

			// bool IsTransient { get; }
			var isTransientProp = t.GetProperty("IsTransient", BindingFlags.Public | BindingFlags.Instance);
			isTransient = isTransientProp is not null && isTransientProp.GetValue(ydbEx) is bool b && b;

			// StatusCode Code { get; }
			var codeProp = t.GetProperty("Code", BindingFlags.Public | BindingFlags.Instance);
			var codeVal  = codeProp?.GetValue(ydbEx);
			codeName = Convert.ToString(codeVal, CultureInfo.InvariantCulture);

			return codeProp != null;
		}

		/// <summary>
		/// Determines whether the given exception should trigger a retry attempt
		/// according to minimal YDB retry strategy rules.
		/// <para>
		/// The logic closely follows the official YDB SDK:
		/// it considers transient statuses and certain service-level timeouts.
		/// </para>
		/// </summary>
		/// <param name="ex">The exception to evaluate.</param>
		/// <param name="enableRetryIdempotence">
		/// If <see langword="true"/>, adds additional YDB codes that should be retried based on SDK retry schemes.
		/// If <see langword="false"/>, only the <c>IsTransient</c> flag is considered.
		/// </param>
		/// <returns>
		/// <see langword="true"/> if the operation should be retried; otherwise, <see langword="false"/>.
		/// </returns>
		public static bool ShouldRetryOn(Exception ex, bool enableRetryIdempotence)
		{
			if (TryGetYdbException(ex, out var ydbEx))
			{
				_ = TryGetCodeAndTransient(ydbEx, out var code, out var isTransient);

				// When idempotence is disabled, rely only on IsTransient flag
				if (!enableRetryIdempotence)
					return isTransient;

				// When idempotence=true, include specific codes that the SDK retries
				// using its own backoff strategy.
				// (We use string names of enum members to avoid direct dependency on YDB SDK enums.)
				return isTransient || code is
					"BadSession" or "SessionBusy" or
					"Aborted" or "Undetermined" or
					"Unavailable" or "ClientTransportUnknown" or "ClientTransportUnavailable" or
					"Overloaded" or "ClientTransportResourceExhausted";
			}

			// Also retry for common network or timeout-related exceptions.
			return ex is TimeoutException;
		}
	}
}
