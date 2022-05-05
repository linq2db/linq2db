namespace LinqToDB.Schema
{
	/// <summary>
	/// Database type descriptor.
	/// </summary>
	/// <param name="Name">Type name.</param>
	/// <param name="Length">Optional type length.</param>
	/// <param name="Precision">Optional type precision.</param>
	/// <param name="Scale">Optional type scale.</param>
	/// <remarks>
	/// <para>
	/// While for some types some databases provide <see cref="Length"/>, <see cref="Precision"/> or <see cref="Scale"/> values, this descriptor include
	/// values for those properties only if they are part of type definition in SQL and not equal to default value for this type (in cases when such values could be ommited from type definition).
	/// </para>
	/// <para>
	/// Type nullability also not included here, as it is a property of typed object.
	/// </para>
	/// </remarks>
	public sealed record DatabaseType(string? Name, int? Length, int? Precision, int? Scale)
	{
		public override string ToString() => $"{Name}{(Length != null || Precision != null || Scale != null ? $"({Length ?? Precision}{(Scale != null ? ", " + Scale : null)})" : null)}";
	}
}
