using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace LinqToDB.Schema
{
	/// <summary>
	/// Tuple-like return type descriptor.
	/// </summary>
	/// <param name="Fields">Ordered tuple fields.</param>
	/// <param name="Nullable">Return tuple could be <c>NULL</c>.</param>
	public sealed record TupleResult(IReadOnlyCollection<ScalarResult> Fields, bool Nullable)
		: Result(ResultKind.Tuple)
	{
		public override string ToString() => $"({string.Join(", ", Fields.Select(f => string.Format(CultureInfo.InvariantCulture, "{0}", f)))})";
	}
}
