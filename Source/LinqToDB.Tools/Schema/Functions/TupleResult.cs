using System.Collections.Generic;

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
		public override string ToString() => $"({string.Join(", ", Fields)})";
	}
}
