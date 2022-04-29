﻿using System.Collections.Generic;

namespace LinqToDB.Schema
{
	/// <summary>
	/// Describes callable database object, e.g. stored procedure or function.
	/// </summary>
	/// <param name="Kind">Callable object type.</param>
	/// <param name="Name">Callable object name.</param>
	/// <param name="Description">Optional object description.</param>
	/// <param name="Parameters">Ordered list of parameters. Doesn't include return value parameter (when object supports it).</param>
	public abstract record CallableObject(
		CallableKind                   Kind,
		ObjectName                     Name,
		string?                        Description,
		IReadOnlyCollection<Parameter> Parameters)
	{
		public override string ToString() => $"{Name}({string.Join(", ", Parameters)})";
	}
}
