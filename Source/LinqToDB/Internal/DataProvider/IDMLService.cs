using System;

namespace LinqToDB.Internal.DataProvider
{
	/// <summary>
	/// Provider-specific DML mechanics that can't be expressed through SQL generation alone.
	/// Resolved from the data context's service provider by query runners that execute DML.
	/// </summary>
	public interface IDMLService
	{
		/// <summary>
		/// Returns <see langword="true"/> if the given exception indicates the target table does not exist.
		/// Used by <c>DropTable</c> to decide whether a "not exists" suppression request should
		/// swallow a given error.
		/// </summary>
		bool IsTableNotFoundException(Exception exception);
	}
}
