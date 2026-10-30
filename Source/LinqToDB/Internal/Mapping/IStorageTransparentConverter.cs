using LinqToDB.Mapping;

namespace LinqToDB.Internal.Mapping
{
	/// <summary>
	/// Marks <see cref="IValueConverter"/> that stores model value unchanged and only re-expresses how the value is
	/// wrapped or how its absence is represented (e.g. F# <c>option</c> or single-case union over a scalar).
	/// </summary>
	/// <remarks>
	/// Value stored through such converter is commensurable with value of a column without conversion, so query
	/// builder doesn't refuse to combine them in database.
	/// </remarks>
	public interface IStorageTransparentConverter
	{
	}
}
