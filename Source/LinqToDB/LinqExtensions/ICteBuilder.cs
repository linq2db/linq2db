using JetBrains.Annotations;

namespace LinqToDB
{
	/// <summary>
	/// Fluent builder for configuring a common table expression (CTE) declared via
	/// <see cref="LinqExtensions.AsCte{TSource}(System.Linq.IQueryable{TSource},System.Action{LinqToDB.ICteBuilder})"/>.
	/// Provider-specific options are exposed as extension methods over this interface.
	/// </summary>
	[PublicAPI]
	public interface ICteBuilder
	{
		/// <summary>
		/// Sets the CTE name. Passing <see langword="null"/> or an empty string leaves the name unset
		/// and linq2db generates a default name.
		/// </summary>
		/// <param name="name">Common table expression name.</param>
		/// <returns>The same builder for chaining.</returns>
		ICteBuilder HasName(string? name);
	}
}
