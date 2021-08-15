using System.Collections.Generic;

namespace LinqToDB.CodeGen.Schema
{
	/// <summary>
	/// Describes callable database object, e.g. stored procedure or function.
	/// </summary>
	/// <param name="Kind">Callable object type.</param>
	/// <param name="Name">Callable object name.</param>
	/// <param name="Description">Optional object description.</param>
	/// <param name="IsSystem">Flag indicating that object is predefined system object or user-defined one.</param>
	/// <param name="Parameters">Ordered list of parameters. Doesn't include return value parameter (when object supports it).</param>
	public abstract record CallableObject(
		CallableKind Kind,
		ObjectName Name,
		string? Description,
		bool IsSystem,
		IReadOnlyCollection<Parameter> Parameters)
	{
		public override string ToString() => $"{Name}({string.Join(", ", Parameters)})";
	}
}
