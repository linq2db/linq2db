using System.Collections.Generic;
using System.Linq;

namespace LinqToDB
{
	/// <summary>
	/// Provides support for queryable LoadWith/ThenLoad chaining operators.
	/// </summary>
	/// <typeparam name="TEntity">The entity type.</typeparam>
	/// <typeparam name="TProperty">The property type.</typeparam>
	// ReSharper disable once UnusedTypeParameter
	public interface ILoadWithQueryable<out TEntity, out TProperty> : IQueryable<TEntity>
	{
	}
}
