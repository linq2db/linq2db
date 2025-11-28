using System.Linq.Expressions;

namespace LinqToDB.Internal.DataProvider.Translation
{
	/// <summary>
	/// Defines a mechanism for converting an <see cref="Expression"/> into another <see cref="Expression"/>  while
	/// indicating whether the conversion was handled.
	/// </summary>
	/// <remarks>This interface is typically used to implement custom logic for transforming expressions in
	/// scenarios  such as query translation, expression tree rewriting, or dynamic evaluation. Implementations should 
	/// ensure that the conversion process is deterministic and thread-safe if used in multi-threaded contexts.</remarks>
	public interface IMemberConverter
	{
		Expression Convert(Expression expression, out bool handled);
	}
}
