using System;

namespace LinqToDB.Internal.DataProvider
{
	/// <summary>
	/// Default <see cref="IDMLService"/> implementation. Conservative defaults:
	/// <see cref="IsTableNotFoundException"/> returns <see langword="false"/> so that providers
	/// without specific knowledge never swallow exceptions.
	/// </summary>
	public class DMLServiceBase : IDMLService
	{
		public virtual bool IsTableNotFoundException(Exception exception)
		{
			ArgumentNullException.ThrowIfNull(exception);

			for (var current = exception; current != null; current = current.InnerException)
			{
				if (IsTableNotFoundExceptionCore(current))
					return true;
			}

			return false;
		}

		/// <summary>
		/// Override to detect a provider-specific "table not found" exception.
		/// Called for every link of the inner-exception chain.
		/// </summary>
		protected virtual bool IsTableNotFoundExceptionCore(Exception exception) => false;

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
	}
}
