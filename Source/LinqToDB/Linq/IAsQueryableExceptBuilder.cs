using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq
{
	/// <summary>
	/// Configuration stage exposed after the rendering mode has been chosen via
	/// <see cref="IAsQueryableBuilder{T}.Parameterize"/> or <see cref="IAsQueryableBuilder{T}.Inline"/>.
	/// Allows per-member overrides via <see cref="Except"/>.
	/// </summary>
	/// <typeparam name="T">Element type of the source enumerable.</typeparam>
	public interface IAsQueryableExceptBuilder<T>
	{
		/// <summary>
		/// Flips the chosen mode for the listed members: under <see cref="IAsQueryableBuilder{T}.Parameterize"/>
		/// the listed members are inlined as literals; under <see cref="IAsQueryableBuilder{T}.Inline"/> they
		/// are rendered as parameters. Selectors are member-access chains rooted at the row, e.g.
		/// <c>p =&gt; p.Id</c> or <c>p =&gt; p.Address.Zip</c>. An empty member list is a no-op.
		/// </summary>
		IAsQueryableExceptBuilder<T> Except(params Expression<Func<T, object?>>[] members);
	}
}
