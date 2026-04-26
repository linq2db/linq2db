using System;

namespace LinqToDB.Internal.DataProvider
{
	/// <summary>
	/// Base class for provider-specific <see cref="IDMLService"/> implementations.
	/// Only providers whose DROP TABLE cannot express "if exists" at the SQL level register one —
	/// for every other provider the service is absent and suppression is not attempted.
	/// </summary>
	public abstract class DMLServiceBase : IDMLService
	{
		public bool IsTableNotFoundException(Exception exception)
		{
			ArgumentNullException.ThrowIfNull(exception);

			for (var current = exception; current != null; current = current.InnerException)
			{
				if (IsTableNotFoundExceptionCore(current))
					return true;

				if (current is AggregateException agg)
				{
					foreach (var inner in agg.Flatten().InnerExceptions)
						if (IsTableNotFoundException(inner))
							return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Detects a provider-specific "table not found" exception.
		/// Called for every link of the inner-exception chain.
		/// </summary>
		protected abstract bool IsTableNotFoundExceptionCore(Exception exception);

		/// <summary>
		/// Matches <paramref name="marker"/> against the exception's type name or its message.
		/// The message check is needed for remote (gRPC / HTTP) data contexts where the original
		/// provider exception is wrapped — the type name survives only as text inside the
		/// wrapping exception's <see cref="Exception.Message"/> (populated via <see cref="Exception.ToString"/>).
		/// </summary>
		protected static bool TypeOrMessageContains(Exception exception, string marker)
		{
			var typeName = exception.GetType().FullName;

			return (typeName != null && typeName.Contains(marker, StringComparison.Ordinal))
				|| exception.Message.Contains(marker, StringComparison.Ordinal);
		}

		/// <summary>
		/// True if <paramref name="exception"/>'s <see cref="Exception.HResult"/> matches
		/// <paramref name="hResult"/>, or the remote-transport message wrapper contains the
		/// canonical hex form ("0x1234ABCD").
		/// </summary>
		protected static bool HResultMatches(Exception exception, int hResult)
		{
			if (exception.HResult == hResult)
				return true;

			var hex = "0x" + hResult.ToString("X8", System.Globalization.CultureInfo.InvariantCulture);
			return exception.Message.Contains(hex, StringComparison.OrdinalIgnoreCase);
		}
	}
}
