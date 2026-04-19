using LinqToDB.Internal.Infrastructure;

namespace LinqToDB.Internal.Linq.Builder
{
	/// <summary>
	/// Default implementation of <see cref="ICteBuilder"/> used during expression translation.
	/// Captures configuration chosen by the user so it can be copied onto the emitted <see cref="SqlQuery.CteClause"/>.
	/// </summary>
	/// <remarks>
	/// Extension methods that live under <c>LinqToDB</c> set provider-specific state via direct access to this type.
	/// </remarks>
	public sealed class CteBuilderImpl : ICteBuilder
	{
		public string?     Name        { get; private set; }
		public Annotatable Annotations { get; } = new();

		ICteBuilder ICteBuilder.HasName(string? name)
		{
			Name = name;
			return this;
		}
	}
}
